using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Engine.Core.Diagnostics;
using Engine.Editor.Projects;
using Engine.Tooling.Debugging;

namespace Engine.Editor.UI.Commands;

public sealed class RebuildEngineCommand :
    IDebugCommand,
    IDisposable
{
    private static readonly string[] ProjectsToPublish =
    {
        @"Engine.Hosting.Desktop\Engine.Hosting.Desktop.csproj",
        @"Engine.Serialization\Engine.Serialization.csproj",
        @"Engine.Input.SilkNet\Engine.Input.SilkNet.csproj"
    };

    private static readonly string[] RequiredAssemblies =
    {
        "Engine.Hosting.Desktop.dll",
        "Engine.Runtime.dll",
        "Engine.Graphics.OpenGL.dll",
        "Engine.Audio.OpenAL.dll",
        "Engine.Serialization.dll",
        "Engine.Graphics2D.dll",
        "Engine.Input.SilkNet.dll"
    };
    private readonly Func<bool>? _areGameSessionsActive;
    private readonly Func<GameProject?> _getActiveProject;
    private readonly Queue<RebuildEngineEvent> _events = new();
    private readonly object _sync = new();
    private readonly CancellationTokenSource _shutdown = new();

    private int _running;
    private int _disposed;

    public RebuildEngineCommand(
        Func<GameProject?> getActiveProject,
        Func<bool>? areGameSessionsActive = null)
    {
        ArgumentNullException.ThrowIfNull(getActiveProject);

        _getActiveProject = getActiveProject;
        _areGameSessionsActive = areGameSessionsActive;
    }
    public bool IsRunning =>
    Volatile.Read(ref _running) != 0;
    public string Name => "rebuild-engine";

    public string Description =>
        "Rebuilds the engine and updates the active game project.";

    public DebugCommandResult Execute(
        IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (Volatile.Read(ref _disposed) != 0)
        {
            return DebugCommandResult.Fail(
                "The rebuild command has been disposed.");
        }

        if (_areGameSessionsActive?.Invoke() == true)
        {
            return DebugCommandResult.Fail(
                "Stop all additional game sessions before rebuilding engine DLLs.");
        }

        if (arguments.Count > 2)
        {
            return DebugCommandResult.Fail(
                "Usage: rebuild-engine [\"source directory\"] [Debug|Release]");
        }

        var configuration = "Debug";
        string? requestedSource = null;

        if (arguments.Count == 1)
        {
            if (IsConfiguration(arguments[0]))
            {
                configuration = arguments[0];
            }
            else
            {
                requestedSource = arguments[0];
            }
        }
        else if (arguments.Count == 2)
        {
            requestedSource = arguments[0];
            configuration = arguments[1];

            if (!IsConfiguration(configuration))
            {
                return DebugCommandResult.Fail(
                    "Configuration must be Debug or Release.");
            }
        }

        if (!OperatingSystem.IsWindows())
        {
            return DebugCommandResult.Fail(
                "Engine runtime rebuilding currently supports Windows only.");
        }

        var project = _getActiveProject();

        if (project is null)
        {
            return DebugCommandResult.Fail(
                "Open a game project first.");
        }

        var engineDirectory = Path.Combine(
            project.RootDirectory,
            "Engine");

        if (!Directory.Exists(engineDirectory))
        {
            return DebugCommandResult.Fail(
                $"Project engine directory was not found: {engineDirectory}");
        }

        if (!File.Exists(project.ProjectFilePath))
        {
            return DebugCommandResult.Fail(
                $"Game project file was not found: {project.ProjectFilePath}");
        }

        string sourceRoot;

        try
        {
            sourceRoot = ResolveSourceRoot(requestedSource);
        }
        catch (Exception exception) when (
            exception is IOException
                or ArgumentException
                or UnauthorizedAccessException
                or NotSupportedException)
        {
            return DebugCommandResult.Fail(exception.Message);
        }

        if (Interlocked.CompareExchange(
                ref _running,
                1,
                0) != 0)
        {
            return DebugCommandResult.Fail(
                "An engine rebuild is already in progress.");
        }

        _ = Task.Run(
            () => BuildAsync(
                sourceRoot,
                project,
                configuration,
                _shutdown.Token));

        return DebugCommandResult.Ok(
            $"Rebuilding VibeEngine2 for '{project.Name}' ({configuration}). " +
            "Build output will appear in the editor console.");
    }

    internal bool TryDequeueEvent(
        out RebuildEngineEvent? item)
    {
        lock (_sync)
        {
            if (_events.Count > 0)
            {
                item = _events.Dequeue();
                return true;
            }
        }

        item = null;
        return false;
    }

    internal void Complete()
    {
        Interlocked.Exchange(ref _running, 0);
    }

    private async Task BuildAsync(
        string sourceRoot,
        GameProject project,
        string configuration,
        CancellationToken cancellationToken)
    {
        PreparedEngineUpdate? prepared = null;

        try
        {
            prepared = await PrepareUpdateAsync(
                sourceRoot,
                project,
                configuration,
                cancellationToken).ConfigureAwait(false);

            Enqueue(
                new RebuildEngineEvent(
                    DiagnosticLevel.Info,
                    "Runtime build succeeded. Applying the update...",
                    prepared));

            prepared = null;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            Enqueue(
                new RebuildEngineEvent(
                    DiagnosticLevel.Warning,
                    "Engine rebuild cancelled.",
                    IsTerminal: true));
        }
        catch (Exception exception)
        {
            Enqueue(
                new RebuildEngineEvent(
                    DiagnosticLevel.Error,
                    $"Engine rebuild failed: {exception.Message}",
                    IsTerminal: true));
        }
        finally
        {
            prepared?.Dispose();

            if (Volatile.Read(ref _disposed) != 0)
            {
                Complete();
            }
        }
    }

    private async Task<PreparedEngineUpdate> PrepareUpdateAsync(
        string sourceRoot,
        GameProject project,
        string configuration,
        CancellationToken cancellationToken)
    {
        var buildDirectory = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngine2-Build-{Guid.NewGuid():N}");

        Directory.CreateDirectory(buildDirectory);

        try
        {
            var runtimeDirectory = Path.Combine(
                buildDirectory,
                "runtime");

            Directory.CreateDirectory(runtimeDirectory);

            foreach (var relativeProjectPath in ProjectsToPublish)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var projectPath = Path.Combine(
                    sourceRoot,
                    relativeProjectPath);

                if (!File.Exists(projectPath))
                {
                    throw new FileNotFoundException(
                        "An engine project required for rebuilding was not found.",
                        projectPath);
                }

                var outputDirectory = Path.Combine(
                    buildDirectory,
                    Path.GetFileNameWithoutExtension(
                        relativeProjectPath));

                Directory.CreateDirectory(outputDirectory);

                Enqueue(
                    new RebuildEngineEvent(
                        DiagnosticLevel.Info,
                        $"Publishing {Path.GetFileNameWithoutExtension(relativeProjectPath)}..."));

                await RunDotnetAsync(
                    sourceRoot,
                    new[]
                    {
                        "publish",
                        projectPath,
                        "--configuration",
                        configuration,
                        "--runtime",
                        "win-x64",
                        "--self-contained",
                        "false",
                        "-p:CopyLocalLockFileAssemblies=true",
                        "--output",
                        outputDirectory
                    },
                    cancellationToken).ConfigureAwait(false);

                foreach (var file in Directory.EnumerateFiles(
                             outputDirectory,
                             "*.dll",
                             SearchOption.TopDirectoryOnly))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    File.Copy(
                        file,
                        Path.Combine(
                            runtimeDirectory,
                            Path.GetFileName(file)),
                        overwrite: true);
                }
            }

            var missing = RequiredAssemblies
                .Where(name => !File.Exists(
                    Path.Combine(runtimeDirectory, name)))
                .ToArray();

            if (missing.Length != 0)
            {
                throw new InvalidOperationException(
                    "Runtime output is incomplete. Missing assemblies: " +
                    string.Join(", ", missing));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var workspace = Path.Combine(
                project.RootDirectory,
                $".vibeengine-rebuild-{Guid.NewGuid():N}");

            Directory.CreateDirectory(workspace);

            try
            {
                var stagedEngineDirectory = Path.Combine(
                    workspace,
                    "Engine");

                CopyDirectory(
                    Path.Combine(project.RootDirectory, "Engine"),
                    stagedEngineDirectory);

                var incomingNames = Directory
                    .EnumerateFiles(
                        runtimeDirectory,
                        "*.dll",
                        SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                // Убираем устаревшие DLL самого движка,
                // но сохраняем сторонние библиотеки проекта.
                foreach (var existingDll in Directory.EnumerateFiles(
                             stagedEngineDirectory,
                             "Engine.*.dll",
                             SearchOption.TopDirectoryOnly))
                {
                    if (!incomingNames.Contains(
                            Path.GetFileName(existingDll)))
                    {
                        File.Delete(existingDll);
                    }
                }

                foreach (var file in Directory.EnumerateFiles(
                             runtimeDirectory,
                             "*.dll",
                             SearchOption.TopDirectoryOnly))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    File.Copy(
                        file,
                        Path.Combine(
                            stagedEngineDirectory,
                            Path.GetFileName(file)),
                        overwrite: true);
                }

                var stagedProjectFile = Path.Combine(
                    workspace,
                    "project.new");

                SynchronizeProjectReferences(
                    project.ProjectFilePath,
                    stagedEngineDirectory,
                    stagedProjectFile);

                cancellationToken.ThrowIfCancellationRequested();

                return new PreparedEngineUpdate(
                    project.RootDirectory,
                    project.ProjectFilePath,
                    workspace,
                    stagedEngineDirectory,
                    stagedProjectFile);
            }
            catch
            {
                TryDeleteDirectory(workspace);
                throw;
            }
        }
        finally
        {
            TryDeleteDirectory(buildDirectory);
        }
    }

    private async Task RunDotnetAsync(
        string workingDirectory,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true
        };

        startInfo.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en";

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process
        {
            StartInfo = startInfo
        };

        if (!process.Start())
        {
            throw new InvalidOperationException(
                "Failed to start dotnet.");
        }

        using var cancellationRegistration =
            cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Process may already have exited.
                }
            });

        var stdoutTask = PumpOutputAsync(
            process.StandardOutput,
            isStandardError: false);

        var stderrTask = PumpOutputAsync(
            process.StandardError,
            isStandardError: true);

        try
        {
            await process.WaitForExitAsync(
                cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Process may already have exited.
            }

            await process.WaitForExitAsync().ConfigureAwait(false);
            await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
            throw;
        }

        await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"dotnet publish exited with code {process.ExitCode}.");
        }

        async Task PumpOutputAsync(
            StreamReader reader,
            bool isStandardError)
        {
            while (await reader.ReadLineAsync().ConfigureAwait(false)
                   is { } line)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var level = line.Contains(
                    ": error ",
                    StringComparison.OrdinalIgnoreCase)
                    ? DiagnosticLevel.Error
                    : line.Contains(
                        ": warning ",
                        StringComparison.OrdinalIgnoreCase)
                        ? DiagnosticLevel.Warning
                        : isStandardError
                            ? DiagnosticLevel.Warning
                            : DiagnosticLevel.Debug;

                Enqueue(
                    new RebuildEngineEvent(level, line));
            }
        }
    }

    private static void SynchronizeProjectReferences(
        string projectFilePath,
        string stagedEngineDirectory,
        string outputProjectPath)
    {
        var document = XDocument.Load(
            projectFilePath,
            LoadOptions.PreserveWhitespace);

        var root = document.Root
            ?? throw new InvalidDataException(
                "The .csproj file has no root element.");

        var ns = root.Name.Namespace;

        // Удаляем ссылки на DLL из папки Engine, которых больше нет.
        foreach (var reference in root.Descendants(ns + "Reference").ToArray())
        {
            var hintPath = (string?)reference.Element(ns + "HintPath");

            if (TryGetEngineDllName(hintPath, out var fileName) &&
                !File.Exists(Path.Combine(stagedEngineDirectory, fileName)))
            {
                reference.Remove();
            }
        }

        foreach (var nativeEntry in root.Descendants(ns + "None").ToArray())
        {
            var updatePath = (string?)nativeEntry.Attribute("Update");

            if (TryGetEngineDllName(updatePath, out var fileName) &&
                !File.Exists(Path.Combine(stagedEngineDirectory, fileName)))
            {
                nativeEntry.Remove();
            }
        }

        var referenceGroup = root.Elements(ns + "ItemGroup")
            .FirstOrDefault(group =>
                group.Elements(ns + "Reference").Any());

        if (referenceGroup is null)
        {
            referenceGroup = new XElement(ns + "ItemGroup");
            root.Add(referenceGroup);
        }

        var nativeGroup = root.Elements(ns + "ItemGroup")
            .FirstOrDefault(group =>
                group.Elements(ns + "None").Any());

        if (nativeGroup is null)
        {
            nativeGroup = new XElement(ns + "ItemGroup");
            root.Add(nativeGroup);
        }

        foreach (var dllPath in Directory.EnumerateFiles(
                     stagedEngineDirectory,
                     "*.dll",
                     SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileName(dllPath);
            var relativePath = $@"Engine\{fileName}";

            if (TryGetAssemblyName(dllPath, out var assemblyName))
            {
                var simpleName = assemblyName.Name
                    ?? Path.GetFileNameWithoutExtension(fileName);

                var reference = root.Descendants(ns + "Reference")
                    .FirstOrDefault(item =>
                        string.Equals(
                            GetPathFileName(
                                (string?)item.Element(ns + "HintPath")),
                            fileName,
                            StringComparison.OrdinalIgnoreCase));

                reference ??= root.Descendants(ns + "Reference")
                    .FirstOrDefault(item =>
                        string.Equals(
                            ((string?)item.Attribute("Include"))
                                ?.Split(',')[0].Trim(),
                            simpleName,
                            StringComparison.OrdinalIgnoreCase));

                if (reference is null)
                {
                    reference = new XElement(
                        ns + "Reference",
                        new XAttribute("Include", simpleName));

                    referenceGroup.Add(reference);
                }

                reference.SetAttributeValue("Include", simpleName);

                SetChildValue(
                    reference,
                    ns + "HintPath",
                    relativePath);

                SetChildValue(
                    reference,
                    ns + "Private",
                    "true");
            }
            else
            {
                var nativeEntry = root.Descendants(ns + "None")
                    .FirstOrDefault(item =>
                        string.Equals(
                            NormalizePath(
                                (string?)item.Attribute("Update")),
                            relativePath,
                            StringComparison.OrdinalIgnoreCase));

                if (nativeEntry is null)
                {
                    nativeEntry = new XElement(ns + "None");
                    nativeGroup.Add(nativeEntry);
                }

                nativeEntry.SetAttributeValue("Update", relativePath);

                SetChildValue(
                    nativeEntry,
                    ns + "CopyToOutputDirectory",
                    "PreserveNewest");

                SetChildValue(
                    nativeEntry,
                    ns + "CopyToPublishDirectory",
                    "PreserveNewest");

                SetChildValue(
                    nativeEntry,
                    ns + "TargetPath",
                    fileName);
            }
        }

        document.Save(outputProjectPath);
    }

    private static bool TryGetAssemblyName(
        string path,
        out AssemblyName assemblyName)
    {
        try
        {
            assemblyName = AssemblyName.GetAssemblyName(path);
            return true;
        }
        catch (BadImageFormatException)
        {
            assemblyName = new AssemblyName();
            return false;
        }
        catch (FileLoadException)
        {
            assemblyName = new AssemblyName();
            return false;
        }
    }

    private static bool TryGetEngineDllName(
        string? path,
        out string fileName)
    {
        fileName = string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var normalized = NormalizePath(path);

        if (!normalized.StartsWith(
                @"Engine\",
                StringComparison.OrdinalIgnoreCase) ||
            !normalized.EndsWith(
                ".dll",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        fileName = GetPathFileName(normalized);
        return fileName.Length > 0;
    }

    private static string NormalizePath(string? path) =>
        (path ?? string.Empty).Replace('/', '\\');

    private static string GetPathFileName(string? path)
    {
        var normalized = NormalizePath(path);
        var separator = normalized.LastIndexOf('\\');

        return separator >= 0
            ? normalized[(separator + 1)..]
            : normalized;
    }

    private static void SetChildValue(
        XElement parent,
        XName name,
        string value)
    {
        var child = parent.Element(name);

        if (child is null)
        {
            parent.Add(new XElement(name, value));
        }
        else
        {
            child.Value = value;
        }
    }

    private static void CopyDirectory(
        string source,
        string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.EnumerateFiles(source))
        {
            File.Copy(
                file,
                Path.Combine(destination, Path.GetFileName(file)),
                overwrite: true);
        }

        foreach (var directory in Directory.EnumerateDirectories(source))
        {
            CopyDirectory(
                directory,
                Path.Combine(destination, Path.GetFileName(directory)));
        }
    }

    private static string ResolveSourceRoot(string? requestedPath)
    {
        if (!string.IsNullOrWhiteSpace(requestedPath))
        {
            var requestedRoot = Path.GetFullPath(requestedPath);

            if (!IsEngineRepositoryRoot(requestedRoot))
            {
                throw new DirectoryNotFoundException(
                    $"'{requestedRoot}' is not a VibeEngine2 source root.");
            }

            return requestedRoot;
        }

        var environmentPath = Environment.GetEnvironmentVariable(
            "VIBEENGINE_SOURCE_DIR");

        if (!string.IsNullOrWhiteSpace(environmentPath))
        {
            return ResolveSourceRoot(environmentPath);
        }

        foreach (var startPath in new[]
                 {
                     Environment.CurrentDirectory,
                     AppContext.BaseDirectory
                 })
        {
            var current = new DirectoryInfo(
                Path.GetFullPath(startPath));

            while (current is not null)
            {
                if (IsEngineRepositoryRoot(current.FullName))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not find VibeEngine2.sln. " +
            "Pass the source path to rebuild-engine or set " +
            "VIBEENGINE_SOURCE_DIR.");
    }

    private static bool IsEngineRepositoryRoot(string path)
    {
        return File.Exists(Path.Combine(path, "VibeEngine2.sln")) &&
               ProjectsToPublish.All(
                   project => File.Exists(Path.Combine(path, project)));
    }

    private static bool IsConfiguration(string value) =>
        string.Equals(
            value,
            "Debug",
            StringComparison.OrdinalIgnoreCase) ||
        string.Equals(
            value,
            "Release",
            StringComparison.OrdinalIgnoreCase);

    private void Enqueue(RebuildEngineEvent item)
    {
        var reject = false;

        lock (_sync)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                reject = true;
            }
            else
            {
                _events.Enqueue(item);
            }
        }

        if (reject)
        {
            item.PreparedUpdate?.Dispose();
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Preserve the original build or update result.
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _shutdown.Cancel();
        Complete();

        lock (_sync)
        {
            while (_events.Count > 0)
            {
                _events.Dequeue().PreparedUpdate?.Dispose();
            }
        }
    }
}

internal sealed record RebuildEngineEvent(
    DiagnosticLevel Level,
    string Message,
    PreparedEngineUpdate? PreparedUpdate = null,
    bool IsTerminal = false);

internal sealed class PreparedEngineUpdate : IDisposable
{
    private readonly string _workspace;
    private readonly string _engineDirectory;
    private readonly string _projectFilePath;
    private readonly string _stagedEngineDirectory;
    private readonly string _stagedProjectFilePath;

    private int _disposed;
    private bool _preserveWorkspace;

    public PreparedEngineUpdate(
        string projectRoot,
        string projectFilePath,
        string workspace,
        string stagedEngineDirectory,
        string stagedProjectFilePath)
    {
        ProjectRoot = projectRoot;
        _projectFilePath = projectFilePath;
        _workspace = workspace;
        _stagedEngineDirectory = stagedEngineDirectory;
        _stagedProjectFilePath = stagedProjectFilePath;

        _engineDirectory = Path.Combine(
            projectRoot,
            "Engine");
    }

    public string ProjectRoot { get; }

    public bool CanRestartGame { get; private set; } = true;

    public void Apply()
    {
        var backupEngine = Path.Combine(
            _workspace,
            "Engine.previous");

        var backupProject = Path.Combine(
            _workspace,
            "project.previous");

        var oldEngineMoved = false;
        var newEngineMoved = false;
        var projectFileReplaced = false;

        try
        {
            File.Copy(
                _projectFilePath,
                backupProject,
                overwrite: true);

            Directory.Move(
                _engineDirectory,
                backupEngine);

            oldEngineMoved = true;

            Directory.Move(
                _stagedEngineDirectory,
                _engineDirectory);

            newEngineMoved = true;

            File.Move(
                _stagedProjectFilePath,
                _projectFilePath,
                overwrite: true);

            projectFileReplaced = true;
        }
        catch (Exception updateException)
        {
            var rollbackErrors = new List<Exception>();

            if (projectFileReplaced)
            {
                try
                {
                    File.Copy(
                        backupProject,
                        _projectFilePath,
                        overwrite: true);
                }
                catch (Exception exception)
                {
                    rollbackErrors.Add(exception);
                }
            }

            if (newEngineMoved)
            {
                try
                {
                    if (Directory.Exists(_engineDirectory))
                    {
                        Directory.Delete(
                            _engineDirectory,
                            recursive: true);
                    }
                }
                catch (Exception exception)
                {
                    rollbackErrors.Add(exception);
                }
            }

            if (oldEngineMoved)
            {
                try
                {
                    if (Directory.Exists(backupEngine) &&
                        !Directory.Exists(_engineDirectory))
                    {
                        Directory.Move(
                            backupEngine,
                            _engineDirectory);
                    }
                }
                catch (Exception exception)
                {
                    rollbackErrors.Add(exception);
                }
            }

            if (rollbackErrors.Count > 0)
            {
                CanRestartGame = false;
                _preserveWorkspace = true;

                rollbackErrors.Insert(0, updateException);

                throw new AggregateException(
                    "Engine update failed and rollback was incomplete. " +
                    $"Recovery files were preserved at '{_workspace}'.",
                    rollbackErrors);
            }

            throw;
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        if (!_preserveWorkspace)
        {
            try
            {
                if (Directory.Exists(_workspace))
                {
                    Directory.Delete(
                        _workspace,
                        recursive: true);
                }
            }
            catch
            {
                // Preserve the original update result.
            }
        }
    }
}