using System.Diagnostics;
using Engine.Editor.Projects;

namespace Engine.Tests.Editor;

public sealed class GameProjectRunnerBuildTests
{

    [Fact]
    public void Start_HotReloadMode_AppliesSourceChanges_AndStopEndsSession()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineRunnerWatch-{Guid.NewGuid():N}");

        Directory.CreateDirectory(root);

        try
        {
            var projectPath = Path.Combine(root, "WatchProbe.csproj");
            var manifestPath = Path.Combine(root, "WatchProbe.vibeproject");
            var sourcePath = Path.Combine(root, "Program.cs");

            File.WriteAllText(
                projectPath,
                """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <NuGetAudit>false</NuGetAudit>
                <RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources>
              </PropertyGroup>
            </Project>
            """);

            File.WriteAllText(
                manifestPath,
                """
            {
              "formatVersion": 1,
              "name": "WatchProbe",
              "startupProject": "WatchProbe.csproj",
              "startupScene": null
            }
            """);

            WriteHotReloadProgram(sourcePath, "V1");

            var project = GameProject.Open(manifestPath);

            using var runner = new GameProjectRunner();

            runner.Start(project, GameProjectRunMode.HotReload);

            Assert.True(runner.IsSessionActive);
            Assert.Equal(
                GameProjectRunMode.HotReload,
                runner.ActiveRunMode);

            var firstRun = DrainUntil(
                runner,
                outputs => outputs.Any(output =>
                    output.Message.Contains(
                        "HOTRELOAD_VERSION=V1",
                        StringComparison.Ordinal)));

            Assert.Contains(
                firstRun,
                output => output.Message.Contains("HOTRELOAD_VERSION=V1"));

            // Ждём, пока наблюдатель будет готов следить за изменениями.
            _ = DrainUntil(
                runner,
                outputs => outputs.Any(output =>
                    output.Message.Contains(
                        "Waiting for changes",
                        StringComparison.OrdinalIgnoreCase)),
                timeoutSeconds: 30);

            WriteHotReloadProgram(sourcePath, "V2");

            var updatedRun = DrainUntil(
                runner,
                outputs => outputs.Any(output =>
                    output.Message.Contains(
                        "HOTRELOAD_VERSION=V2",
                        StringComparison.Ordinal)),
                timeoutSeconds: 60);

            Assert.Contains(
                updatedRun,
                output => output.Message.Contains("HOTRELOAD_VERSION=V2"));

            Assert.True(runner.IsSessionActive);

            runner.Stop();

            Assert.False(runner.IsSessionActive);
            Assert.Null(runner.ActiveRunMode);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static void WriteHotReloadProgram(
        string path,
        string version)
    {
        File.WriteAllText(
            path,
            $$"""
        internal static class Program
        {
            private static async Task Main()
            {
                while (true)
                {
                    Console.WriteLine(
                        $"HOTRELOAD_VERSION={GetVersion()} PID={Environment.ProcessId}");

                    await Task.Delay(500);
                }
            }

            private static string GetVersion() => "{{version}}";
        }
        """ + Environment.NewLine);
    }


    [Fact]
    public void Restart_BuildsUpdatedSource_AndRecoversAfterBuildFailure()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineRunner-{Guid.NewGuid():N}");

        Directory.CreateDirectory(root);

        try
        {
            var projectPath = Path.Combine(root, "RunnerProbe.csproj");
            var manifestPath = Path.Combine(root, "RunnerProbe.vibeproject");
            var sourcePath = Path.Combine(root, "Program.cs");

            File.WriteAllText(
                projectPath,
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net8.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                    <NuGetAudit>false</NuGetAudit>
                    <RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources>
                  </PropertyGroup>
                </Project>
                """);

            File.WriteAllText(
                manifestPath,
                """
                {
                  "formatVersion": 1,
                  "name": "RunnerProbe",
                  "startupProject": "RunnerProbe.csproj",
                  "startupScene": null
                }
                """);

            var project = GameProject.Open(manifestPath);

            using var runner = new GameProjectRunner();

            // Первый запуск: проект компилируется и запускается.
            WriteProgram(sourcePath, "BUILD_PROBE_V1");

            runner.Start(project);

            var firstRun = DrainUntil(
                runner,
                outputs => outputs.Any(output =>
                    output.Message.Contains(
                        "BUILD_PROBE_V1",
                        StringComparison.Ordinal)));

            Assert.Contains(
                firstRun,
                output => !output.IsError &&
                    output.Message.Contains("BUILD_PROBE_V1"));

            runner.Stop();

            // Имитируем ошибку в исходнике.
            File.WriteAllText(
                sourcePath,
                """
                public static class BrokenProgram
                {
                    public static void Main( { }
                }
                """);

            runner.Start(project);

            var failedBuild = DrainUntil(
                runner,
                output =>
                    output.Any(item =>
                        item.Message.Contains(
                            "Game process exited with code 1.",
                            StringComparison.Ordinal)) &&
                    output.Any(item =>
                        item.Message.Contains(
                            "CS",
                            StringComparison.Ordinal)));

            Assert.Contains(
                failedBuild,
                output => output.Message.Contains(
                    "Game process exited with code 1.",
                    StringComparison.Ordinal));

            Assert.Contains(
                failedBuild,
                output => output.Message.Contains(
                    "CS",
                    StringComparison.Ordinal));

            Assert.False(runner.IsRunning);

            // Исправляем исходник и запускаем снова.
            WriteProgram(sourcePath, "BUILD_PROBE_V2");

            runner.Start(project);

            var recoveredRun = DrainUntil(
                runner,
                outputs => outputs.Any(output =>
                    output.Message.Contains(
                        "BUILD_PROBE_V2",
                        StringComparison.Ordinal)));

            Assert.Contains(
                recoveredRun,
                output => !output.IsError &&
                    output.Message.Contains("BUILD_PROBE_V2"));

            Assert.True(runner.IsRunning);

            runner.Stop();

            Assert.False(runner.IsRunning);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static void WriteProgram(
        string path,
        string message)
    {
        File.WriteAllText(
            path,
            $$"""
            Console.WriteLine("{{message}}");
            Console.Out.Flush();
            Thread.Sleep(TimeSpan.FromSeconds(30));
            """);
    }

    private static List<GameProjectOutput> DrainUntil(
        GameProjectRunner runner,
        Func<List<GameProjectOutput>, bool> complete,
        int timeoutSeconds = 90)
    {
        var outputs = new List<GameProjectOutput>();
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < TimeSpan.FromSeconds(timeoutSeconds))
        {
            while (runner.TryDequeueOutput(out var output))
            {
                if (output is not null)
                {
                    outputs.Add(output);
                }
            }

            if (complete(outputs))
            {
                return outputs;
            }

            Thread.Sleep(20);
        }

        throw new TimeoutException(
            "Timed out waiting for the game process. Output:" +
            Environment.NewLine +
            string.Join(
                Environment.NewLine,
                outputs.Select(output => output.Message)));
    }
}