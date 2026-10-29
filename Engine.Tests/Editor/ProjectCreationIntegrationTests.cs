using System.Diagnostics;
using System.Text;
using Engine.Editor.Projects;

namespace Engine.Tests.Editor;

public sealed class ProjectCreationIntegrationTests
{

    [Fact]
    public async Task Create_GeneratedProject_CanPublishOutsideEngineRepository()
    {
        using var root = new TemporaryDirectory();

        var sourceRoot = FindSourceRoot();
        var runtimeDirectory = Path.Combine(root.Path, "Runtime");

        // Собираем настоящую бинарную поставку движка.
        await EngineRuntimeBuilder.BuildAsync(
            sourceRoot,
            runtimeDirectory,
            "Debug");

        var projectsDirectory = Path.Combine(
            root.Path,
            "Projects");

        var projectDirectory = Path.Combine(
            projectsDirectory,
            "GeneratedGame");

        // Создаём проект штатным генератором редактора.
        var service = new ProjectCreationService(
            runtimeDirectory);

        var createdDirectory = service.Create(
            new ProjectCreationRequest(
                "GeneratedGame",
                projectsDirectory,
                projectDirectory));

        Assert.Equal(
            Path.GetFullPath(projectDirectory),
            createdDirectory);

        Assert.False(
            IsWithinDirectory(sourceRoot, createdDirectory),
            "The generated project must be outside the engine repository.");

        // Добавляем ресурс так же, как это сделал бы разработчик игры.
        var assetPath = Path.Combine(
            createdDirectory,
            "Assets",
            "Probe",
            "marker.txt");

        Directory.CreateDirectory(
            Path.GetDirectoryName(assetPath)!);

        File.WriteAllText(
            assetPath,
            "generated-project-asset",
            new UTF8Encoding(false));

        // После создания проект должен использовать собственную
        // копию библиотек, а не исходный каталог поставки.
        Directory.Delete(
            runtimeDirectory,
            recursive: true);

        var publishDirectory = Path.Combine(
            root.Path,
            "Publish");

        var result = RunDotnet(
            createdDirectory,
            "publish",
            Path.Combine(
                createdDirectory,
                "GeneratedGame.csproj"),
            "--configuration",
            "Release",
            "--output",
            publishDirectory,
            "-p:NuGetAudit=false",
            "-p:RestoreIgnoreFailedSources=true");

        Assert.True(
            result.ExitCode == 0,
            "Generated project failed to publish."
            + Environment.NewLine
            + result.Output);

        // Публикация должна содержать исполняемую сборку,
        // необходимые библиотеки и файлы игрового проекта.
        Assert.True(File.Exists(
            Path.Combine(publishDirectory, "GeneratedGame.dll")));

        Assert.True(File.Exists(
            Path.Combine(publishDirectory, "Engine.Runtime.dll")));

        Assert.True(File.Exists(
            Path.Combine(publishDirectory, "Engine.Hosting.Desktop.dll")));

        Assert.True(File.Exists(
            Path.Combine(publishDirectory, "GeneratedGame.vibeproject")));

        Assert.True(File.Exists(
            Path.Combine(publishDirectory, "Scenes", "Main.world")));

        Assert.Equal(
            "generated-project-asset",
            File.ReadAllText(
                Path.Combine(
                    publishDirectory,
                    "Assets",
                    "Probe",
                    "marker.txt")));
    }

    private static string FindSourceRoot()
    {
        for (
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            current is not null;
            current = current.Parent)
        {
            if (EngineRuntimeBuilder.IsSourceRoot(current.FullName))
            {
                return current.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not find the VibeEngine2 source repository.");
    }

    private static bool IsWithinDirectory(
        string directory,
        string path)
    {
        var relativePath = Path.GetRelativePath(
            Path.GetFullPath(directory),
            Path.GetFullPath(path));

        return !Path.IsPathRooted(relativePath)
            && relativePath != ".."
            && !relativePath.StartsWith(
                $"..{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal)
            && !relativePath.StartsWith(
                $"..{Path.AltDirectorySeparatorChar}",
                StringComparison.Ordinal);
    }

    private static (int ExitCode, string Output) RunDotnet(
        string workingDirectory,
        params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName =
                Environment.GetEnvironmentVariable("DOTNET_HOST_PATH")
                ?? "dotnet",

            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process
        {
            StartInfo = startInfo
        };

        var output = new StringBuilder();
        var outputLock = new object();

        void AppendOutput(string? line)
        {
            if (line is null)
            {
                return;
            }

            lock (outputLock)
            {
                output.AppendLine(line);
            }
        }

        string ReadOutput()
        {
            lock (outputLock)
            {
                return output.ToString();
            }
        }

        process.OutputDataReceived +=
            (_, args) => AppendOutput(args.Data);

        process.ErrorDataReceived +=
            (_, args) => AppendOutput(args.Data);

        if (!process.Start())
        {
            throw new InvalidOperationException(
                "Failed to start dotnet publish.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit(180_000))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();

            throw new TimeoutException(
                "dotnet publish timed out."
                + Environment.NewLine
                + ReadOutput());
        }

        // Дожидаемся обработки перенаправленного вывода.
        process.WaitForExit();

        return (process.ExitCode, ReadOutput());
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory =
            Directory.CreateTempSubdirectory();

        public string Path => _directory.FullName;

        public void Dispose()
        {
            if (_directory.Exists)
            {
                _directory.Delete(recursive: true);
            }
        }
    }
}