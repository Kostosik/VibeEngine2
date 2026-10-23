using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Engine.Core.Diagnostics;
using Engine.Tooling.Debugging;

namespace Engine.Runtime;

internal sealed class EngineRebuildService : IDisposable
{
    private static readonly string[] RequiredAssemblies =
    {
        "Engine.Hosting.Desktop.dll",
        "Engine.Runtime.dll",
        "Engine.Graphics.OpenGL.dll",
        "Engine.Audio.OpenAL.dll",
        "Engine.Input.SilkNet.dll",
        "Engine.Serialization.dll",
        "Engine.Graphics2D.dll"
    };

    private static readonly string[] ProjectsToPublish =
    {
        Path.Combine(
            "Engine.Hosting.Desktop",
            "Engine.Hosting.Desktop.csproj"),

        Path.Combine(
            "Engine.Serialization",
            "Engine.Serialization.csproj"),

        Path.Combine(
            "Engine.Input.SilkNet",
            "Engine.Input.SilkNet.csproj")
    };

    private readonly ConcurrentQueue<PendingMessage> _messages = new();
    private readonly CancellationTokenSource _shutdown = new();

    private int _isRunning;
    private bool _disposed;

    public bool TryStart(
        string sourceDirectory,
        string configuration,
        string workingDirectory,
        out string message)
    {
        message = string.Empty;

        if (_disposed)
        {
            message = "The rebuild service has been disposed.";
            return false;
        }

        if (!IsConfiguration(configuration))
        {
            message = "Configuration must be Debug or Release.";
            return false;
        }

        if (!OperatingSystem.IsWindows() ||
            RuntimeInformation.ProcessArchitecture != Architecture.X64)
        {
            message = "rebuild-engine currently supports Windows x64 only.";
            return false;
        }

        string sourceRoot;

        try
        {
            sourceRoot = Path.GetFullPath(sourceDirectory);
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or NotSupportedException
                or PathTooLongException)
        {
            message = $"Invalid source directory: {exception.Message}";
            return false;
        }

        if (!Directory.Exists(sourceRoot))
        {
            message = $"Source directory does not exist: {sourceRoot}";
            return false;
        }

        if (!File.Exists(
                Path.Combine(sourceRoot, "VibeEngine2.sln")))
        {
            message = $"VibeEngine2.sln was not found in '{sourceRoot}'.";
            return false;
        }

        foreach (var relativePath in ProjectsToPublish)
        {
            if (!File.Exists(
                    Path.Combine(sourceRoot, relativePath)))
            {
                message = $"Required project was not found: {relativePath}";
                return false;
            }
        }

        ProjectDescriptor project;

        try
        {
            project = FindGameProject(workingDirectory);
        }
        catch (Exception exception) when (
            exception is IOException
                or UnauthorizedAccessException
                or JsonException
                or ArgumentException
                or InvalidDataException)
        {
            message = exception.Message;
            return false;
        }

        if (Interlocked.CompareExchange(
                ref _isRunning,
                1,
                0) != 0)
        {
            message = "An engine rebuild is already running.";
            return false;
        }

        var normalizedConfiguration =
            string.Equals(
                configuration,
                "Release",
                StringComparison.OrdinalIgnoreCase)
                ? "Release"
                : "Debug";

        _ = Task.Run(
            () => RebuildAsync(
                sourceRoot,
                project,
                normalizedConfiguration,
                _shutdown.Token));

        message =
            $"Engine rebuild started ({normalizedConfiguration}). " +
            "Build output will appear in this console.";

        return true;
    }

    public void DrainTo(DebugConsole? console)
    {
        if (console is null)
        {
            return;
        }

        while (_messages.TryDequeue(out var item))
        {
            console.Write(item.Level, item.Message);
        }
    }

    private async Task RebuildAsync(
        string sourceRoot,
        ProjectDescriptor project,
        string configuration,
        CancellationToken cancellationToken)
    {
        var workDirectory = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngine2-Rebuild-{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(workDirectory);

            Enqueue(
                DiagnosticLevel.Info,
                $"Building engine from '{sourceRoot}'.");

            var publishDirectories = new List<string>();

            foreach (var relativePath in ProjectsToPublish)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var projectPath = Path.Combine(
                    sourceRoot,
                    relativePath);

                var outputDirectory = Path.Combine(
                    workDirectory,
                    Path.GetFileNameWithoutExtension(relativePath));

                Directory.CreateDirectory(outputDirectory);

                Enqueue(
                    DiagnosticLevel.Info,
                    $"Publishing {Path.GetFileNameWithoutExtension(relativePath)}...");

                await RunDotnetAsync(
                    sourceRoot,
                    new[]
                    {
                        "publish",
                        projectPath,
                        "--configuration", configuration,
                        "--runtime", "win-x64",
                        "--self-contained", "false",
                        "-p:CopyLocalLockFileAssemblies=true",
                        "--output", outputDirectory
                    },
                    cancellationToken).ConfigureAwait(false);

                publishDirectories.Add(outputDirectory);
            }

            // Several engine modules are not direct dependencies
            // of Engine.Hosting.Desktop, so merge their outputs.
            var runtimeDirectory = Path.Combine(
                workDirectory,
                "runtime");

            Directory.CreateDirectory(runtimeDirectory);

            foreach (var publishDirectory in publishDirectories)
            {
                foreach (var file in Directory.EnumerateFiles(
                             publishDirectory,
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
                .Where(file => !File.Exists(
                    Path.Combine(runtimeDirectory, file)))
                .ToArray();

            if (missing.Length > 0)
            {
                throw new InvalidOperationException(
                    "Runtime output is incomplete. Missing: " +
                    string.Join(", ", missing));
            }

            cancellationToken.ThrowIfCancellationRequested();

            Enqueue(
                DiagnosticLevel.Info,
                "Build succeeded. Updating project engine DLLs...");

            ApplyRuntimeUpdate(
                project,
                runtimeDirectory,
                cancellationToken);

            Enqueue(
                DiagnosticLevel.Info,
                "Engine DLLs updated. Stop and start the game again.");
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            Enqueue(
                DiagnosticLevel.Warning,
                "Engine rebuild was cancelled.");
        }
        catch (Exception exception)
        {
            Enqueue(
                DiagnosticLevel.Error,
                $"Engine rebuild failed: {exception.Message}");
        }
        finally
        {
            TryDeleteDirectory(workDirectory);
            Interlocked.Exchange(ref _isRunning, 0);
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

        using var registration = cancellationToken.Register(() =>
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
                // The process may have exited already.
            }
        });

        var outputTask = PumpOutputAsync(
            process.StandardOutput,
            DiagnosticLevel.Info);

        var errorTask = PumpOutputAsync(
            process.StandardError,
            DiagnosticLevel.Warning);

        await process.WaitForExitAsync().ConfigureAwait(false);

        await Task.WhenAll(
            outputTask,
            errorTask).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"dotnet exited with code {process.ExitCode}. " +
                "See the preceding console output.");
        }

        async Task PumpOutputAsync(
            StreamReader reader,
            DiagnosticLevel level)
        {
            while (await reader.ReadLineAsync().ConfigureAwait(false)
                   is { } line)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    Enqueue(level, line);
                }
            }
        }
    }

    private static void ApplyRuntimeUpdate(
        ProjectDescriptor project,
        string runtimeDirectory,
        CancellationToken cancellationToken)
    {
        var projectRoot = project.RootDirectory;
        var engineDirectory = Path.Combine(projectRoot, "Engine");
        var suffix = Guid.NewGuid().ToString("N");

        var preparedDirectory = Path.Combine(
            projectRoot,
            $".Engine-rebuild-{suffix}");

        var backupDirectory = Path.Combine(
            projectRoot,
            $".Engine-backup-{suffix}");

        var temporaryProject = Path.Combine(
            projectRoot,
            $".project-rebuild-{suffix}.tmp");

        var backupProject = Path.Combine(
            projectRoot,
            $".project-backup-{suffix}.tmp");

        var oldDirectoryMoved = false;
        var newDirectoryMoved = false;
        var projectFileReplaced = false;
        var succeeded = false;

        try
        {
            CopyDirectory(engineDirectory, preparedDirectory);

            foreach (var file in Directory.EnumerateFiles(
                         runtimeDirectory,
                         "*.dll",
                         SearchOption.TopDirectoryOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();

                File.Copy(
                    file,
                    Path.Combine(
                        preparedDirectory,
                        Path.GetFileName(file)),
                    overwrite: true);
            }

            UpdateProjectReferences(
                project.ProjectFilePath,
                preparedDirectory,
                temporaryProject);

            File.Copy(
                project.ProjectFilePath,
                backupProject,
                overwrite: true);

            // After this point, finish the transaction without checking
            // cancellation so that the rollback path remains predictable.
            cancellationToken.ThrowIfCancellationRequested();

            Directory.Move(
                engineDirectory,
                backupDirectory);

            oldDirectoryMoved = true;

            Directory.Move(
                preparedDirectory,
                engineDirectory);

            newDirectoryMoved = true;

            File.Move(
                temporaryProject,
                project.ProjectFilePath,
                overwrite: true);

            projectFileReplaced = true;
            succeeded = true;
        }
        catch (Exception updateException)
        {
            try
            {
                if (projectFileReplaced &&
                    File.Exists(backupProject))
                {
                    File.Copy(
                        backupProject,
                        project.ProjectFilePath,
                        overwrite: true);
                }

                if (newDirectoryMoved &&
                    Directory.Exists(engineDirectory))
                {
                    Directory.Delete(
                        engineDirectory,
                        recursive: true);
                }

                if (oldDirectoryMoved &&
                    Directory.Exists(backupDirectory))
                {
                    Directory.Move(
                        backupDirectory,
                        engineDirectory);
                }
            }
            catch (Exception rollbackException)
            {
                throw new AggregateException(
                    "Engine update failed and rollback was incomplete. " +
                    $"Keep '{backupDirectory}' and '{backupProject}'.",
                    updateException,
                    rollbackException);
            }

            throw;
        }
        finally
        {
            TryDeleteDirectory(preparedDirectory);
            TryDeleteFile(temporaryProject);

            if (succeeded)
            {
                TryDeleteDirectory(backupDirectory);
                TryDeleteFile(backupProject);
            }
            else if (!oldDirectoryMoved)
            {
                TryDeleteFile(backupProject);
            }
        }
    }

    private static void UpdateProjectReferences(
        string projectFilePath,
        string engineDirectory,
        string outputPath)
    {
        var document = XDocument.Load(
            projectFilePath,
            LoadOptions.PreserveWhitespace);

        var root = document.Root
            ?? throw new InvalidDataException(
                "The project file has no root element.");

        var ns = root.Name.Namespace;

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

        foreach (var file in Directory.EnumerateFiles(
                     engineDirectory,
                     "*.dll",
                     SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileName(file);
            var relativePath = $"Engine\\{fileName}";

            if (TryGetAssemblyName(file, out var assemblyName))
            {
                var name = assemblyName.Name
                    ?? Path.GetFileNameWithoutExtension(fileName);

                var reference = root.Descendants(ns + "Reference")
                    .FirstOrDefault(element =>
                        ReferenceMatches(
                            (string?)element.Attribute("Include"),
                            name));

                if (reference is null)
                {
                    reference = new XElement(ns + "Reference");
                    referenceGroup.Add(reference);
                }

                reference.SetAttributeValue("Include", name);

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
                var nativeFile = root.Descendants(ns + "None")
                    .FirstOrDefault(element =>
                        PathEquals(
                            (string?)element.Attribute("Update"),
                            relativePath));

                if (nativeFile is null)
                {
                    nativeFile = new XElement(ns + "None");
                    nativeGroup.Add(nativeFile);
                }

                nativeFile.SetAttributeValue(
                    "Update",
                    relativePath);

                SetChildValue(
                    nativeFile,
                    ns + "CopyToOutputDirectory",
                    "PreserveNewest");

                SetChildValue(
                    nativeFile,
                    ns + "CopyToPublishDirectory",
                    "PreserveNewest");

                SetChildValue(
                    nativeFile,
                    ns + "TargetPath",
                    fileName);
            }
        }

        document.Save(outputPath);
    }

    private static bool ReferenceMatches(
        string? include,
        string assemblyName)
    {
        if (string.IsNullOrWhiteSpace(include))
        {
            return false;
        }

        var simpleName = include.Split(',')[0].Trim();

        return string.Equals(
                   simpleName,
                   assemblyName,
                   StringComparison.OrdinalIgnoreCase)
               || string.Equals(
                   Path.GetFileNameWithoutExtension(simpleName),
                   assemblyName,
                   StringComparison.OrdinalIgnoreCase);
    }

    private static bool PathEquals(
        string? left,
        string right)
    {
        return string.Equals(
            left?.Replace('/', '\\'),
            right.Replace('/', '\\'),
            StringComparison.OrdinalIgnoreCase);
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

    private static ProjectDescriptor FindGameProject(
        string startDirectory)
    {
        var current = new DirectoryInfo(
            Path.GetFullPath(startDirectory));

        while (current is not null)
        {
            var engineDirectory = Path.Combine(
                current.FullName,
                "Engine");

            if (Directory.Exists(engineDirectory))
            {
                foreach (var manifestPath in Directory.EnumerateFiles(
                             current.FullName,
                             "*.vibeproject",
                             SearchOption.TopDirectoryOnly))
                {
                    using var manifest = JsonDocument.Parse(
                        File.ReadAllText(manifestPath));

                    if (!manifest.RootElement.TryGetProperty(
                            "startupProject",
                            out var projectElement) ||
                        projectElement.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var relativePath = projectElement.GetString();

                    if (string.IsNullOrWhiteSpace(relativePath) ||
                        Path.IsPathRooted(relativePath))
                    {
                        throw new InvalidDataException(
                            $"Invalid startupProject in '{manifestPath}'.");
                    }

                    var projectPath = Path.GetFullPath(
                        Path.Combine(
                            current.FullName,
                            relativePath));

                    var projectRelativePath = Path.GetRelativePath(
                        current.FullName,
                        projectPath);

                    if (IsOutsideProjectRoot(projectRelativePath))
                    {
                        throw new InvalidDataException(
                            "The startup project points outside the project directory.");
                    }

                    if (!File.Exists(projectPath) ||
                        !string.Equals(
                            Path.GetExtension(projectPath),
                            ".csproj",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        throw new FileNotFoundException(
                            "The startup .csproj was not found.",
                            projectPath);
                    }

                    return new ProjectDescriptor(
                        current.FullName,
                        projectPath);
                }
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the game project. Start the game from a project " +
            "containing a .vibeproject file and an Engine directory.");
    }

    private static bool IsOutsideProjectRoot(string relativePath)
    {
        return Path.IsPathRooted(relativePath)
               || relativePath.Equals(
                   "..",
                   StringComparison.Ordinal)
               || relativePath.StartsWith(
                   $"..{Path.DirectorySeparatorChar}",
                   StringComparison.Ordinal)
               || relativePath.StartsWith(
                   $"..{Path.AltDirectorySeparatorChar}",
                   StringComparison.Ordinal);
    }

    private static void CopyDirectory(
        string source,
        string destination)
    {
        if (!Directory.Exists(source))
        {
            throw new DirectoryNotFoundException(
                $"Engine directory was not found: {source}");
        }

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
                Path.Combine(
                    destination,
                    Path.GetFileName(directory)));
        }
    }

    private static bool IsConfiguration(string value)
    {
        return string.Equals(
                   value,
                   "Debug",
                   StringComparison.OrdinalIgnoreCase)
               || string.Equals(
                   value,
                   "Release",
                   StringComparison.OrdinalIgnoreCase);
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
            // Cleanup must not hide the original result.
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Cleanup must not hide the original result.
        }
    }

    private void Enqueue(
        DiagnosticLevel level,
        string message)
    {
        _messages.Enqueue(new PendingMessage(level, message));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _shutdown.Cancel();
        _shutdown.Dispose();
    }

    private readonly record struct PendingMessage(
        DiagnosticLevel Level,
        string Message);

    private sealed record ProjectDescriptor(
        string RootDirectory,
        string ProjectFilePath);
}