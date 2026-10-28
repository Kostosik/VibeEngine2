using Engine.Core.Diagnostics;
using Engine.Editor.Projects;
using Engine.Scripting;
using Engine.Tooling.Debugging;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

namespace Engine.Editor.UI.Workspace;

internal sealed record ScriptImplementationChoice(
    string AssemblyName,
    string TypeName)
{
    public string DisplayName =>
        $"{TypeName} ({AssemblyName})";
}

internal static class ScriptImplementationDiscovery
{
    public static async Task<IReadOnlyList<ScriptImplementationChoice>>
        DiscoverAsync(
            GameProject project,
            DebugConsole? console = null)
    {
        ArgumentNullException.ThrowIfNull(project);

        console?.WriteLine(
            $"Building '{project.Name}' to discover C# scripts.");

        var targetPath = await GetTargetPathAsync(project);

        var buildResult = await RunAsync(
            CreateProcess(
                project.RootDirectory,
                "build",
                project.ProjectFilePath,
                "--configuration",
                "Debug",
                "--verbosity",
                "quiet"));

        if (buildResult.ExitCode != 0)
        {
            throw new InvalidOperationException(
                "Could not build the game project.\n" +
                buildResult.StandardOutput + "\n" +
                buildResult.StandardError);
        }

        if (!File.Exists(targetPath))
        {
            throw new FileNotFoundException(
                "The game assembly was not found after building.",
                targetPath);
        }

        var choices = new List<ScriptImplementationChoice>();
        var sharedScriptingAssembly =
            typeof(IEntityScript).Assembly.GetName().Name;

        var loadContext =
            new ProjectAssemblyLoadContext(targetPath);

        try
        {
            var directory =
                Path.GetDirectoryName(targetPath)!;

            foreach (var assemblyPath in
                     Directory.EnumerateFiles(directory, "*.dll"))
            {
                AssemblyName assemblyName;

                try
                {
                    assemblyName =
                        AssemblyName.GetAssemblyName(assemblyPath);
                }
                catch (BadImageFormatException)
                {
                    // Пропускаем DLL, которые не являются managed-сборками.
                    continue;
                }

                // Контракт IEntityScript должен быть тем же экземпляром
                // типа, который использует сам редактор.
                if (string.Equals(
                        assemblyName.Name,
                        sharedScriptingAssembly,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                try
                {
                    var assembly =
                        loadContext.Assemblies.FirstOrDefault(
                            loaded => AssemblyName.ReferenceMatchesDefinition(
                                loaded.GetName(),
                                assemblyName))
                        ?? loadContext.LoadFromAssemblyPath(
                            Path.GetFullPath(assemblyPath));

                    foreach (var type in GetLoadableTypes(
                                 assembly,
                                 console))
                    {
                        try
                        {
                            if (!ScriptRegistry
                                    .IsSupportedImplementationType(type))
                            {
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(type.FullName) ||
                                string.IsNullOrWhiteSpace(
                                    assembly.GetName().Name))
                            {
                                continue;
                            }

                            choices.Add(
                                new ScriptImplementationChoice(
                                    assembly.GetName().Name!,
                                    type.FullName));
                        }
                        catch (Exception exception) when (
                            exception is TypeLoadException or
                            NotSupportedException or
                            ReflectionTypeLoadException)
                        {
                            console?.Write(DiagnosticLevel.Warning,
                                $"Could not inspect C# type in " +
                                $"'{assembly.GetName().Name}': " +
                                exception.Message);
                        }
                    }
                }
                catch (Exception exception) when (
                    exception is BadImageFormatException or
                    FileLoadException or
                    FileNotFoundException or
                    ReflectionTypeLoadException)
                {
                    console?.Write(DiagnosticLevel.Warning,
                        $"Could not inspect assembly " +
                        $"'{Path.GetFileName(assemblyPath)}': " +
                        exception.Message);
                }
            }
        }
        finally
        {
            loadContext.Unload();
        }

        var result = choices
            .DistinctBy(
                choice => $"{choice.AssemblyName}\0{choice.TypeName}",
                StringComparer.Ordinal)
            .OrderBy(
                choice => choice.TypeName,
                StringComparer.Ordinal)
            .ThenBy(
                choice => choice.AssemblyName,
                StringComparer.Ordinal)
            .ToArray();

        console?.WriteLine(
            $"Found {result.Length} compatible C# script class(es).");

        return result;
    }

    private static IEnumerable<Type> GetLoadableTypes(
        Assembly assembly,
        DebugConsole? console)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            foreach (var loaderException in
                     exception.LoaderExceptions
                         .OfType<Exception>()
                         .DistinctBy(
                             error => error.Message,
                             StringComparer.Ordinal))
            {
                console?.Write(DiagnosticLevel.Warning,
                    $"Some types in '{assembly.GetName().Name}' " +
                    $"could not be loaded: {loaderException.Message}");
            }

            return exception.Types.OfType<Type>();
        }
    }

    private static async Task<string> GetTargetPathAsync(
        GameProject project)
    {
        var result = await RunAsync(
            CreateProcess(
                project.RootDirectory,
                "msbuild",
                project.ProjectFilePath,
                "-getProperty:TargetPath",
                "-property:Configuration=Debug"));

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                "Could not determine the game assembly path.\n" +
                result.StandardOutput + "\n" +
                result.StandardError);
        }

        string? targetPath = null;

        try
        {
            using var json =
                JsonDocument.Parse(result.StandardOutput);

            if (json.RootElement.TryGetProperty(
                    "Properties",
                    out var properties) &&
                properties.TryGetProperty(
                    "TargetPath",
                    out var targetPathElement))
            {
                targetPath = targetPathElement.GetString();
            }
        }
        catch (JsonException)
        {
            // Некоторые MSBuild-версии могут вернуть только значение.
        }

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            var candidate = result.StandardOutput.Trim();

            if (candidate.EndsWith(
                    ".dll",
                    StringComparison.OrdinalIgnoreCase))
            {
                targetPath = candidate.Trim('"');
            }
        }

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            throw new InvalidOperationException(
                "MSBuild returned an empty TargetPath.");
        }

        return Path.GetFullPath(
            targetPath,
            project.RootDirectory);
    }

    private static ProcessStartInfo CreateProcess(
        string workingDirectory,
        params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private static async Task<ProcessResult> RunAsync(
        ProcessStartInfo startInfo)
    {
        using var process = new Process
        {
            StartInfo = startInfo
        };

        if (!process.Start())
        {
            throw new InvalidOperationException(
                $"Could not start '{startInfo.FileName}'.");
        }

        var stdoutTask =
            process.StandardOutput.ReadToEndAsync();

        var stderrTask =
            process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return new ProcessResult(
            process.ExitCode,
            await stdoutTask,
            await stderrTask);
    }

    private sealed record ProcessResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);

    private sealed class ProjectAssemblyLoadContext :
        AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public ProjectAssemblyLoadContext(string assemblyPath)
            : base(isCollectible: true)
        {
            _resolver =
                new AssemblyDependencyResolver(assemblyPath);
        }

        protected override Assembly? Load(
            AssemblyName assemblyName)
        {
            if (string.Equals(
                    assemblyName.Name,
                    typeof(IEntityScript).Assembly.GetName().Name,
                    StringComparison.Ordinal))
            {
                return typeof(IEntityScript).Assembly;
            }

            var path =
                _resolver.ResolveAssemblyToPath(assemblyName);

            return path is null
                ? null
                : LoadFromAssemblyPath(path);
        }

        protected override IntPtr LoadUnmanagedDll(
            string unmanagedDllName)
        {
            var path =
                _resolver.ResolveUnmanagedDllToPath(
                    unmanagedDllName);

            return path is null
                ? IntPtr.Zero
                : LoadUnmanagedDllFromPath(path);
        }
    }
}