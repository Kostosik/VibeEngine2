using System.Diagnostics;
using System.Text;

namespace Engine.Editor.Projects;

public static class EngineRuntimeBuilder
{
        private static readonly string[] s_sourceProjectPaths =
        {
        Path.Combine(
            "Engine.Hosting.Desktop",
            "Engine.Hosting.Desktop.csproj"),

        Path.Combine(
            "Engine.Serialization",
            "Engine.Serialization.csproj"),

        Path.Combine(
            "Engine.Networking.Replication",
            "Engine.Networking.Replication.csproj"),

        Path.Combine(
            "Engine.Input.SilkNet",
            "Engine.Input.SilkNet.csproj"),

        Path.Combine(
            "Engine.Hosting",
            "Engine.Hosting.csproj"),

        Path.Combine(
            "Engine.Localization",
            "Engine.Localization.csproj"),

        Path.Combine(
            "Engine.Navigation",
            "Engine.Navigation.csproj"),
        Path.Combine(
            "Engine.Hosting",
            "Engine.Hosting.csproj"),

        Path.Combine(
            "Engine.Localization",
            "Engine.Localization.csproj"),

        Path.Combine(
            "Engine.Navigation",
            "Engine.Navigation.csproj")
        };

        private static readonly string[] s_requiredAssemblies =
        {
        "Engine.Animation.dll",
        "Engine.Audio.OpenAL.dll",
        "Engine.Audio.dll",
        "Engine.Camera.dll",
        "Engine.Content.dll",
        "Engine.Core.dll",
        "Engine.ECS.dll",
        "Engine.Graphics2D.dll",
        "Engine.Graphics.OpenGL.dll",
        "Engine.Graphics.dll",
        "Engine.Hosting.Desktop.dll",
        "Engine.Hosting.dll",
        "Engine.Input.SilkNet.dll",
        "Engine.Input.dll",
        "Engine.Jobs.dll",
        "Engine.Localization.dll",
        "Engine.Memory.dll",
        "Engine.Navigation.dll",
        "Engine.Networking.Replication.dll",
        "Engine.Networking.dll",
        "Engine.Particles.dll",
        "Engine.Physics.dll",
        "Engine.Runtime.dll",
        "Engine.Scripting.dll",
        "Engine.Serialization.dll",
        "Engine.Simulations.dll",
        "Engine.Tooling.dll",
        "Engine.Transform.dll",
        "Engine.UI.dll",
        "Engine.Worlds.dll"
        };

    public static IReadOnlyList<string> SourceProjectPaths { get; } =
        Array.AsReadOnly(s_sourceProjectPaths);

    public static IReadOnlyList<string> RequiredAssemblies { get; } =
        Array.AsReadOnly(s_requiredAssemblies);

    public static bool IsSourceRoot(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var root = Path.GetFullPath(path);

        return File.Exists(
                   Path.Combine(root, "VibeEngine2.sln"))
               && SourceProjectPaths.All(
                   relativePath => File.Exists(
                       Path.Combine(root, relativePath)));
    }

    public static string[] GetMissingAssemblies(
        string directory)
    {
        return RequiredAssemblies
            .Where(file => !File.Exists(
                Path.Combine(directory, file)))
            .ToArray();
    }

    public static bool ContainsRequiredAssemblies(
        string directory)
    {
        return Directory.Exists(directory)
               && GetMissingAssemblies(directory).Length == 0;
    }

    public static async Task BuildAsync(
        string sourceRoot,
        string outputDirectory,
        string configuration = "Debug",
        Action<string, bool>? onOutput = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        if (!string.Equals(
                configuration,
                "Debug",
                StringComparison.OrdinalIgnoreCase)
            && !string.Equals(
                configuration,
                "Release",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Configuration must be Debug or Release.",
                nameof(configuration));
        }

        sourceRoot = Path.GetFullPath(sourceRoot);
        outputDirectory = Path.GetFullPath(outputDirectory);

        if (!IsSourceRoot(sourceRoot))
        {
            throw new DirectoryNotFoundException(
                $"'{sourceRoot}' is not a valid VibeEngine2 source root.");
        }

        var workspace = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngine2-RuntimeBuild-{Guid.NewGuid():N}");

        var runtimeOutput = Path.Combine(
            workspace,
            "runtime");

        Directory.CreateDirectory(runtimeOutput);

        try
        {
            foreach (var relativeProjectPath in SourceProjectPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var projectPath = Path.Combine(
                    sourceRoot,
                    relativeProjectPath);

                var projectName = Path.GetFileNameWithoutExtension(
                    projectPath);

                var projectOutput = Path.Combine(
                    workspace,
                    $"publish-{projectName}");

                Directory.CreateDirectory(projectOutput);

                onOutput?.Invoke(
                    $"Publishing {projectName}...",
                    false);

                await PublishProjectAsync(
                    sourceRoot,
                    projectPath,
                    projectOutput,
                    configuration,
                    onOutput,
                    cancellationToken).ConfigureAwait(false);

                foreach (var file in Directory.EnumerateFiles(
                             projectOutput,
                             "*.dll",
                             SearchOption.TopDirectoryOnly))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    File.Copy(
                        file,
                        Path.Combine(
                            runtimeOutput,
                            Path.GetFileName(file)),
                        overwrite: true);
                }
            }

            var missing = GetMissingAssemblies(runtimeOutput);

            if (missing.Length > 0)
            {
                throw new InvalidOperationException(
                    "The built runtime is incomplete. Missing assemblies: "
                    + string.Join(", ", missing));
            }

            InstallDirectory(
                runtimeOutput,
                outputDirectory);

            onOutput?.Invoke(
                $"Runtime is ready: {outputDirectory}",
                false);
        }
        finally
        {
            TryDeleteDirectory(workspace);
        }
    }

    private static async Task PublishProjectAsync(
        string sourceRoot,
        string projectPath,
        string outputDirectory,
        string configuration,
        Action<string, bool>? onOutput,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = sourceRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true
        };

        startInfo.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en";

        startInfo.ArgumentList.Add("publish");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add(configuration);
        startInfo.ArgumentList.Add("--runtime");
        startInfo.ArgumentList.Add("win-x64");
        startInfo.ArgumentList.Add("--self-contained");
        startInfo.ArgumentList.Add("false");
        startInfo.ArgumentList.Add(
            "-p:CopyLocalLockFileAssemblies=true");
        startInfo.ArgumentList.Add("--output");
        startInfo.ArgumentList.Add(outputDirectory);

        using var process = new Process
        {
            StartInfo = startInfo
        };

        if (!process.Start())
        {
            throw new InvalidOperationException(
                $"Failed to start dotnet publish for '{projectPath}'.");
        }

        var log = new StringBuilder();
        var logLock = new object();

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

                lock (logLock)
                {
                    log.AppendLine(line);
                }

                onOutput?.Invoke(line, isStandardError);
            }
        }

        var stdoutTask = PumpOutputAsync(
            process.StandardOutput,
            isStandardError: false);

        var stderrTask = PumpOutputAsync(
            process.StandardError,
            isStandardError: true);

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
                    // The process may already have exited.
                }
            });

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
                // The process may already have exited.
            }

            await process.WaitForExitAsync().ConfigureAwait(false);
            await Task.WhenAll(
                stdoutTask,
                stderrTask).ConfigureAwait(false);

            throw;
        }

        await Task.WhenAll(
            stdoutTask,
            stderrTask).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (process.ExitCode != 0)
        {
            string details;

            lock (logLock)
            {
                details = log.ToString();
            }

            throw new InvalidOperationException(
                $"dotnet publish '{Path.GetFileName(projectPath)}' " +
                $"exited with code {process.ExitCode}."
                + Environment.NewLine
                + details);
        }
    }

    private static void InstallDirectory(
        string stagedDirectory,
        string outputDirectory)
    {
        var parentDirectory = Path.GetDirectoryName(outputDirectory)
            ?? throw new InvalidOperationException(
                "Cannot determine the runtime output directory.");

        Directory.CreateDirectory(parentDirectory);

        if (File.Exists(outputDirectory))
        {
            throw new IOException(
                $"A file exists at the runtime output path: {outputDirectory}");
        }

        string? backupDirectory = null;

        if (Directory.Exists(outputDirectory))
        {
            backupDirectory = outputDirectory
                + $".previous-{Guid.NewGuid():N}";

            Directory.Move(
                outputDirectory,
                backupDirectory);
        }

        try
        {
            Directory.Move(
                stagedDirectory,
                outputDirectory);
        }
        catch (Exception installException)
        {
            if (backupDirectory is not null
                && Directory.Exists(backupDirectory)
                && !Directory.Exists(outputDirectory))
            {
                try
                {
                    Directory.Move(
                        backupDirectory,
                        outputDirectory);
                }
                catch (Exception rollbackException)
                {
                    throw new AggregateException(
                        "Runtime installation failed and the previous " +
                        "runtime could not be restored.",
                        installException,
                        rollbackException);
                }
            }

            throw;
        }

        if (backupDirectory is not null)
        {
            TryDeleteDirectory(backupDirectory);
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(
                    path,
                    recursive: true);
            }
        }
        catch
        {
            // Preserve the original build result.
        }
    }
}