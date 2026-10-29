using System.Diagnostics;
using Engine.Editor.Projects;

namespace Engine.Tests.Editor;

public sealed class GameProjectRunnerBuildTests
{
    [Fact]
    public void Start_RuntimeException_ReportsExceptionAndStackTrace()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineRunnerRuntimeFailure-{Guid.NewGuid():N}");

        Directory.CreateDirectory(root);

        try
        {
            const string projectName = "RuntimeFailureProbe";

            var projectPath = Path.Combine(
                root,
                $"{projectName}.csproj");

            var manifestPath = Path.Combine(
                root,
                $"{projectName}.vibeproject");

            var sourcePath = Path.Combine(
                root,
                "Program.cs");

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
                $$"""
            {
              "formatVersion": 1,
              "name": "{{projectName}}",
              "startupProject": "{{projectName}}.csproj",
              "startupScene": null
            }
            """);

            File.WriteAllText(
                sourcePath,
                """
            throw new InvalidOperationException(
                "RUNTIME_PROBE_FAILURE");
            """);

            var project = GameProject.Open(manifestPath);

            using var runner = new GameProjectRunner();

            runner.Start(project);

            var output = DrainUntil(
                runner,
                outputs =>
                    outputs.Any(item =>
                        item.Message.Contains(
                            "RUNTIME_PROBE_FAILURE",
                            StringComparison.Ordinal))
                    &&
                    outputs.Any(item =>
                        item.Message.StartsWith(
                            "Game process exited with code ",
                            StringComparison.Ordinal)));

            Assert.Contains(
                output,
                item => item.IsError &&
                    item.Message.Contains(
                        "RUNTIME_PROBE_FAILURE",
                        StringComparison.Ordinal));

            Assert.Contains(
                output,
                item => item.IsError &&
                    item.Message.Contains(
                        "Program.cs",
                        StringComparison.Ordinal));

            Assert.Contains(
                output,
                item => item.IsError &&
                    item.Message.StartsWith(
                        "Game process exited with code ",
                        StringComparison.Ordinal));

            // Исправляем игру после runtime-аварии.
            File.WriteAllText(
                sourcePath,
                """
    Console.WriteLine("RUNTIME_RECOVERY_SUCCESS");
    """);

            runner.Start(project);

            var recoveredRun = DrainUntil(
                runner,
                outputs =>
                    outputs.Any(item =>
                        item.Message.Contains(
                            "RUNTIME_RECOVERY_SUCCESS",
                            StringComparison.Ordinal))
                    &&
                    outputs.Any(item =>
                        item.Message == "Game process exited with code 0."));

            Assert.Contains(
                recoveredRun,
                item => !item.IsError &&
                    item.Message.Contains(
                        "RUNTIME_RECOVERY_SUCCESS",
                        StringComparison.Ordinal));

            Assert.Contains(
                recoveredRun,
                item => !item.IsError &&
                    item.Message == "Game process exited with code 0.");

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

    [Fact]
    public void SessionManager_RuntimeFailure_ReportsErrorAndAllowsNewSession()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineSessionRuntimeFailure-{Guid.NewGuid():N}");

        Directory.CreateDirectory(root);

        try
        {
            const string projectName = "RuntimeFailureProbe";

            var projectPath = Path.Combine(
                root,
                $"{projectName}.csproj");

            var manifestPath = Path.Combine(
                root,
                $"{projectName}.vibeproject");

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
                $$"""
            {
              "formatVersion": 1,
              "name": "{{projectName}}",
              "startupProject": "{{projectName}}.csproj",
              "startupScene": null
            }
            """);

            File.WriteAllText(
                sourcePath,
                """
            throw new InvalidOperationException(
                "SESSION_RUNTIME_PROBE_FAILURE");
            """);

            var project = GameProject.Open(manifestPath);

            using var manager = new GameProjectSessionManager();

            var failedSessionId = manager.Start(
                project,
                "RuntimeFailureSession");

            var failedOutput = DrainUntil(
                manager,
                outputs =>
                    outputs.Any(output =>
                        output.SessionId == failedSessionId &&
                        output.IsError &&
                        output.Message.Contains(
                            "SESSION_RUNTIME_PROBE_FAILURE",
                            StringComparison.Ordinal))
                    &&
                    outputs.Any(output =>
                        output.SessionId == failedSessionId &&
                        output.IsError &&
                        output.Message.Contains(
                            "Program.cs",
                            StringComparison.Ordinal))
                    &&
                    outputs.Any(output =>
                        output.SessionId == failedSessionId &&
                        output.Message.StartsWith(
                            "Game process exited with code ",
                            StringComparison.Ordinal)));

            Assert.Contains(
                failedOutput,
                output => output.SessionId == failedSessionId &&
                    output.IsError &&
                    output.Message.Contains(
                        "SESSION_RUNTIME_PROBE_FAILURE",
                        StringComparison.Ordinal));

            Assert.Contains(
                failedOutput,
                output => output.SessionId == failedSessionId &&
                    output.IsError &&
                    output.Message.Contains(
                        "Program.cs",
                        StringComparison.Ordinal));

            // Исправляем исходный код и создаём новую сессию.
            File.WriteAllText(
                sourcePath,
                """
            Console.WriteLine("SESSION_RUNTIME_RECOVERY_SUCCESS");
            """);

            var recoveredSessionId = manager.Start(
                project,
                "RuntimeFailureSession");

            var recoveredOutput = DrainUntil(
                manager,
                outputs =>
                    outputs.Any(output =>
                        output.SessionId == recoveredSessionId &&
                        output.Message.Contains(
                            "SESSION_RUNTIME_RECOVERY_SUCCESS",
                            StringComparison.Ordinal))
                    &&
                    outputs.Any(output =>
                        output.SessionId == recoveredSessionId &&
                        output.Message ==
                            "Game process exited with code 0."));

            Assert.Contains(
                recoveredOutput,
                output => output.SessionId == recoveredSessionId &&
                    !output.IsError &&
                    output.Message.Contains(
                        "SESSION_RUNTIME_RECOVERY_SUCCESS",
                        StringComparison.Ordinal));

            Assert.Contains(
                recoveredOutput,
                output => output.SessionId == recoveredSessionId &&
                    !output.IsError &&
                    output.Message == "Game process exited with code 0.");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Start_ShortLivedGame_ReportsExitAndRemovesInactiveSession()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineRunnerExit-{Guid.NewGuid():N}");

        Directory.CreateDirectory(root);

        try
        {
            var projectPath = Path.Combine(
                root,
                "ExitProbe.csproj");

            var manifestPath = Path.Combine(
                root,
                "ExitProbe.vibeproject");

            var sourcePath = Path.Combine(
                root,
                "Program.cs");

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
              "name": "ExitProbe",
              "startupProject": "ExitProbe.csproj",
              "startupScene": null
            }
            """);

            File.WriteAllText(
                sourcePath,
                """
            Console.WriteLine("EXIT_PROBE");
            """);

            var project = GameProject.Open(manifestPath);

            using var manager = new GameProjectSessionManager();

            var sessionId = manager.Start(
                project,
                "ExitProbe");

            var output = DrainUntil(
                manager,
                outputs =>
                    outputs.Any(item =>
                        item.SessionId == sessionId &&
                        item.Message == "EXIT_PROBE")
                    &&
                    outputs.Any(item =>
                        item.SessionId == sessionId &&
                        item.Message == "Game process exited with code 0."));

            Assert.Contains(
                output,
                item => item.SessionId == sessionId &&
                        item.Message == "EXIT_PROBE");

            Assert.Contains(
                output,
                item => item.SessionId == sessionId &&
                        item.Message == "Game process exited with code 0.");

            // Получение списка очищает завершённые сессии.
            Assert.Empty(manager.Sessions);

            Assert.DoesNotContain(
                manager.Sessions,
                session => session.IsActive);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Dispose_StopsRunningGameProcessTree()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineRunnerDispose-{Guid.NewGuid():N}");

        Directory.CreateDirectory(root);

        try
        {
            var projectPath = Path.Combine(
                root,
                "DisposeProbe.csproj");

            var manifestPath = Path.Combine(
                root,
                "DisposeProbe.vibeproject");

            var sourcePath = Path.Combine(
                root,
                "Program.cs");

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
              "name": "DisposeProbe",
              "startupProject": "DisposeProbe.csproj",
              "startupScene": null
            }
            """);

            var project = GameProject.Open(manifestPath);

            File.WriteAllText(
    sourcePath,
    """
    if (args.Length == 1 && args[0] == "--child")
    {
        Thread.Sleep(TimeSpan.FromSeconds(30));
        return;
    }

    var childStartInfo = new System.Diagnostics.ProcessStartInfo
    {
        FileName = "dotnet",
        UseShellExecute = false
    };

    childStartInfo.ArgumentList.Add("exec");
    childStartInfo.ArgumentList.Add(
        System.Reflection.Assembly.GetExecutingAssembly().Location);
    childStartInfo.ArgumentList.Add("--child");

    using var child = System.Diagnostics.Process.Start(childStartInfo)
        ?? throw new InvalidOperationException(
            "Failed to start child process.");

    Console.WriteLine($"CHILD_PID={child.Id}");
    Console.Out.Flush();

    Thread.Sleep(TimeSpan.FromSeconds(30));
    """);

            using var runner = new GameProjectRunner();

            runner.Start(project);

            var output = DrainUntil(
                runner,
                outputs => outputs.Any(item =>
                    item.Message.StartsWith(
                        "CHILD_PID=",
                        StringComparison.Ordinal)));

            var pidMessage = Assert.Single(
                output.Where(item =>
                    item.Message.StartsWith(
                        "CHILD_PID=",
                        StringComparison.Ordinal)));

            var childPid = int.Parse(
                pidMessage.Message["CHILD_PID=".Length..]);

            using var childProcess = Process.GetProcessById(childPid);

            Assert.True(runner.IsSessionActive);

            Assert.False(
                childProcess.HasExited,
                "The child process exited before runner disposal.");

            runner.Dispose();

            Assert.True(
                childProcess.WaitForExit(5000),
                "The game process was not terminated after runner disposal.");

            Assert.False(runner.IsSessionActive);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Dispose_StopsAllRunningSessions()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineSessionsDispose-{Guid.NewGuid():N}");

        Directory.CreateDirectory(root);

        using var manager = new GameProjectSessionManager();

        try
        {
            var firstProject = CreateProbeProject(
                Path.Combine(root, "First"),
                "FirstProbe");

            var secondProject = CreateProbeProject(
                Path.Combine(root, "Second"),
                "SecondProbe");

            var firstId = manager.Start(
                firstProject,
                "First",
                applicationArguments: new[] { "first" });

            var firstOutput = DrainUntil(
                manager,
                outputs => outputs.Any(item =>
                    item.SessionId == firstId &&
                    item.Message.StartsWith(
                        "SESSION_PID=first:",
                        StringComparison.Ordinal)));

            var firstMessage = Assert.Single(
                firstOutput.Where(item =>
                    item.SessionId == firstId &&
                    item.Message.StartsWith(
                        "SESSION_PID=first:",
                        StringComparison.Ordinal)));

            var firstPid = int.Parse(
                firstMessage.Message.Split(':')[1]);

            using var firstProcess =
                Process.GetProcessById(firstPid);

            Assert.False(firstProcess.HasExited);

            var secondId = manager.Start(
                secondProject,
                "Second",
                applicationArguments: new[] { "second" });

            var secondOutput = DrainUntil(
                manager,
                outputs => outputs.Any(item =>
                    item.SessionId == secondId &&
                    item.Message.StartsWith(
                        "SESSION_PID=second:",
                        StringComparison.Ordinal)));

            var secondMessage = Assert.Single(
                secondOutput.Where(item =>
                    item.SessionId == secondId &&
                    item.Message.StartsWith(
                        "SESSION_PID=second:",
                        StringComparison.Ordinal)));

            var secondPid = int.Parse(
                secondMessage.Message.Split(':')[1]);

            using var secondProcess =
                Process.GetProcessById(secondPid);

            Assert.False(firstProcess.HasExited);
            Assert.False(secondProcess.HasExited);
            Assert.Equal(2, manager.Sessions.Count);

            manager.Dispose();

            Assert.True(
                firstProcess.WaitForExit(5000),
                "The first game process was not terminated.");

            Assert.True(
                secondProcess.WaitForExit(5000),
                "The second game process was not terminated.");

            Assert.Empty(manager.Sessions);
        }
        finally
        {
            manager.Dispose();

            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }

        static GameProject CreateProbeProject(
            string directory,
            string projectName)
        {
            Directory.CreateDirectory(directory);

            var projectPath = Path.Combine(
                directory,
                $"{projectName}.csproj");

            var manifestPath = Path.Combine(
                directory,
                $"{projectName}.vibeproject");

            File.WriteAllText(
                projectPath,
                """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources>
              </PropertyGroup>
            </Project>
            """);

            File.WriteAllText(
                manifestPath,
                $$"""
            {
              "formatVersion": 1,
              "name": "{{projectName}}",
              "startupProject": "{{projectName}}.csproj",
              "startupScene": null
            }
            """);

            File.WriteAllText(
                Path.Combine(directory, "Program.cs"),
                """
            Console.WriteLine(
                $"SESSION_PID={args.Single()}:{Environment.ProcessId}");

            Console.Out.Flush();

            Thread.Sleep(TimeSpan.FromMinutes(2));
            """);

            return GameProject.Open(manifestPath);
        }
    }

    [Fact]
    public void Stop_AndDispose_AreIdempotent()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineRunnerIdempotent-{Guid.NewGuid():N}");

        Directory.CreateDirectory(root);

        try
        {
            var projectPath = Path.Combine(root, "Probe.csproj");
            var manifestPath = Path.Combine(root, "Probe.vibeproject");
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
                <RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources>
              </PropertyGroup>
            </Project>
            """);

            File.WriteAllText(
                manifestPath,
                """
            {
              "formatVersion": 1,
              "name": "Probe",
              "startupProject": "Probe.csproj",
              "startupScene": null
            }
            """);

            File.WriteAllText(
                sourcePath,
                """
            Console.WriteLine($"STOP_PID={Environment.ProcessId}");
            Console.Out.Flush();

            Thread.Sleep(TimeSpan.FromSeconds(30));
            """);

            var project = GameProject.Open(manifestPath);
            using var runner = new GameProjectRunner();

            runner.Start(project);

            var output = DrainUntil(
                runner,
                outputs => outputs.Any(item =>
                    item.Message.StartsWith(
                        "STOP_PID=",
                        StringComparison.Ordinal)));

            var message = Assert.Single(
                output.Where(item =>
                    item.Message.StartsWith(
                        "STOP_PID=",
                        StringComparison.Ordinal)));

            var pid = int.Parse(
                message.Message["STOP_PID=".Length..]);

            using var gameProcess = Process.GetProcessById(pid);

            Assert.True(runner.IsSessionActive);
            Assert.False(gameProcess.HasExited);

            runner.Stop();
            runner.Stop();

            runner.Dispose();
            runner.Dispose();

            Assert.True(
                gameProcess.WaitForExit(5000),
                "The game process remained alive after repeated shutdown calls.");

            Assert.False(runner.IsSessionActive);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Stop_WhenExitedProcessHasDescendantHoldingOutput_DoesNotWaitIndefinitely()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineRunnerStopTimeout-{Guid.NewGuid():N}");

        Directory.CreateDirectory(root);

        Process? descendant = null;
        Task? stopTask = null;
        var runner = new GameProjectRunner();

        try
        {
            var projectPath = Path.Combine(root, "Probe.csproj");
            var manifestPath = Path.Combine(root, "Probe.vibeproject");
            var sourcePath = Path.Combine(root, "Program.cs");

            File.WriteAllText(projectPath, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources>
                <NuGetAudit>false</NuGetAudit>
              </PropertyGroup>
            </Project>
            """);

            File.WriteAllText(manifestPath, """
            {
              "formatVersion": 1,
              "name": "Probe",
              "startupProject": "Probe.csproj",
              "startupScene": null
            }
            """);

            File.WriteAllText(sourcePath, """
            using System.Diagnostics;
            using System.Reflection;

            if (args.Length == 1 && args[0] == "--orphan-child")
            {
                Thread.Sleep(TimeSpan.FromSeconds(30));
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                UseShellExecute = false
            };

            startInfo.ArgumentList.Add(
                Assembly.GetExecutingAssembly().Location);
            startInfo.ArgumentList.Add("--orphan-child");

            using var child = Process.Start(startInfo)
                ?? throw new InvalidOperationException(
                    "Failed to start descendant process.");

            Console.WriteLine($"ORPHAN_PID={child.Id}");
            Console.Out.Flush();
            """);

            runner.Start(GameProject.Open(manifestPath));

            var output = DrainUntil(
                runner,
                outputs =>
                    outputs.Any(item =>
                        item.Message.StartsWith(
                            "ORPHAN_PID=",
                            StringComparison.Ordinal))
                    &&
                    outputs.Any(item =>
                        item.Message ==
                        "Game process exited with code 0."),
                timeoutSeconds: 30);

            var pidMessage = Assert.Single(
                output.Where(item =>
                    item.Message.StartsWith(
                        "ORPHAN_PID=",
                        StringComparison.Ordinal)));

            var pid = int.Parse(
                pidMessage.Message["ORPHAN_PID=".Length..]);

            descendant = Process.GetProcessById(pid);

            Assert.False(runner.IsSessionActive);

            stopTask = Task.Run(runner.Stop);

            await Task.WhenAny(
                stopTask,
                Task.Delay(TimeSpan.FromSeconds(7)));

            Assert.True(
                stopTask.IsCompleted,
                "Stop blocked indefinitely while waiting for redirected output.");

            await stopTask;
        }
        finally
        {
            // Завершаем дочерний процесс.
            try
            {
                if (descendant is not null && !descendant.HasExited)
                {
                    descendant.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Процесс мог уже завершиться.
            }

            // Дожидаемся завершения процесса, чтобы он не удерживал Probe.dll.
            try
            {
                if (descendant is not null)
                {
                    await descendant
                        .WaitForExitAsync()
                        .WaitAsync(TimeSpan.FromSeconds(5));
                }
            }
            catch
            {
                // Не скрываем исходную ошибку теста.
            }

            // Дожидаемся завершения Stop без блокировки потока.
            try
            {
                if (stopTask is not null)
                {
                    await stopTask.WaitAsync(TimeSpan.FromSeconds(5));
                }
            }
            catch
            {
                // Не скрываем исходную ошибку теста.
            }

            descendant?.Dispose();
            runner.Dispose();

            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Stop_StopsDescendantProcesses()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineRunnerTree-{Guid.NewGuid():N}");

        Directory.CreateDirectory(root);

        try
        {
            var projectPath = Path.Combine(
                root,
                "ProcessTreeProbe.csproj");

            var manifestPath = Path.Combine(
                root,
                "ProcessTreeProbe.vibeproject");

            var sourcePath = Path.Combine(
                root,
                "Program.cs");

            File.WriteAllText(
                projectPath,
                """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources>
              </PropertyGroup>
            </Project>
            """);

            File.WriteAllText(
                manifestPath,
                """
            {
              "formatVersion": 1,
              "name": "ProcessTreeProbe",
              "startupProject": "ProcessTreeProbe.csproj",
              "startupScene": null
            }
            """);

            File.WriteAllText(
                sourcePath,
                """
            using System.Diagnostics;
            using System.Reflection;

            if (args.Length == 1 && args[0] == "--descendant")
            {
                Console.WriteLine(
                    $"DESCENDANT_PID={Environment.ProcessId}");

                Console.Out.Flush();
                Thread.Sleep(TimeSpan.FromSeconds(30));
                return;
            }

            using var descendant = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "dotnet",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    ArgumentList =
                    {
                        Assembly.GetEntryAssembly()!.Location,
                        "--descendant"
                    }
                })
                ?? throw new InvalidOperationException(
                    "Failed to start descendant process.");

            var pidLine = descendant.StandardOutput.ReadLine()
                ?? throw new InvalidOperationException(
                    "Descendant did not report its process ID.");

            Console.WriteLine(pidLine);
            Console.Out.Flush();

            Thread.Sleep(TimeSpan.FromSeconds(30));
            """);

            var project = GameProject.Open(manifestPath);

            using var runner = new GameProjectRunner();

            runner.Start(project);

            var output = DrainUntil(
                runner,
                outputs => outputs.Any(item =>
                    item.Message.StartsWith(
                        "DESCENDANT_PID=",
                        StringComparison.Ordinal)));

            var pidMessage = Assert.Single(
                output.Where(item =>
                    item.Message.StartsWith(
                        "DESCENDANT_PID=",
                        StringComparison.Ordinal)));

            var descendantPid = int.Parse(
                pidMessage.Message["DESCENDANT_PID=".Length..]);

            using var descendantProcess =
                Process.GetProcessById(descendantPid);

            Assert.True(runner.IsSessionActive);
            Assert.False(descendantProcess.HasExited);

            runner.Stop();

            Assert.True(
                descendantProcess.WaitForExit(5000),
                "The descendant process was not terminated after runner.Stop.");

            Assert.False(runner.IsSessionActive);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Start_ApplicationArguments_ArePassedToGameUnchanged()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineRunnerArgs-{Guid.NewGuid():N}");

        Directory.CreateDirectory(root);

        try
        {
            var projectPath = Path.Combine(
                root,
                "RunnerArgsProbe.csproj");

            var manifestPath = Path.Combine(
                root,
                "RunnerArgsProbe.vibeproject");

            var sourcePath = Path.Combine(
                root,
                "Program.cs");

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
              "name": "RunnerArgsProbe",
              "startupProject": "RunnerArgsProbe.csproj",
              "startupScene": null
            }
            """);

            File.WriteAllText(
                sourcePath,
                """
            Console.WriteLine($"ARG_COUNT={args.Length}");

            for (var i = 0; i < args.Length; i++)
            {
                Console.WriteLine($"ARG_{i}={args[i]}");
            }

            Console.Out.Flush();
            Thread.Sleep(TimeSpan.FromSeconds(30));
            """);

            var project = GameProject.Open(manifestPath);

            using var runner = new GameProjectRunner();

            string[] applicationArguments =
            [
                "--server",
            "--bind",
            "127.0.0.1",
            "--label",
            "server node 1"
            ];

            runner.Start(
                project,
                GameProjectRunMode.Standard,
                applicationArguments);

            var output = DrainUntil(
                runner,
                outputs =>
                    outputs.Any(item =>
                        item.Message == "ARG_COUNT=5")
                    &&
                    outputs.Any(item =>
                        item.Message == "ARG_4=server node 1"));

            Assert.Contains(
                output,
                item => item.Message == "ARG_COUNT=5");

            Assert.Contains(
                output,
                item => item.Message == "ARG_0=--server");

            Assert.Contains(
                output,
                item => item.Message == "ARG_1=--bind");

            Assert.Contains(
                output,
                item => item.Message == "ARG_2=127.0.0.1");

            Assert.Contains(
                output,
                item => item.Message == "ARG_3=--label");

            Assert.Contains(
                output,
                item => item.Message == "ARG_4=server node 1");

            runner.Stop();

            Assert.False(runner.IsSessionActive);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Start_TwoSessions_RunsConcurrently_AndStopsIndependently()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineSessions-{Guid.NewGuid():N}");

        Directory.CreateDirectory(root);

        try
        {
            var projectPath = Path.Combine(
                root,
                "SessionProbe.csproj");

            var manifestPath = Path.Combine(
                root,
                "SessionProbe.vibeproject");

            var sourcePath = Path.Combine(
                root,
                "Program.cs");

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
              "name": "SessionProbe",
              "startupProject": "SessionProbe.csproj",
              "startupScene": null
            }
            """);

            File.WriteAllText(
                sourcePath,
                """
            Console.WriteLine(
                $"SESSION_ARG={args.Single()} PID={Environment.ProcessId}");

            Console.Out.Flush();

            Thread.Sleep(TimeSpan.FromSeconds(30));
            """);

            var project = GameProject.Open(manifestPath);

            using var manager = new GameProjectSessionManager();

            var serverId = manager.Start(
                project,
                "Server",
                applicationArguments: new[] { "--server" });

            var serverOutput = DrainUntil(
                manager,
                outputs => outputs.Any(output =>
                    output.SessionId == serverId &&
                    output.Message.Contains(
                        "SESSION_ARG=--server",
                        StringComparison.Ordinal)));

            Assert.Contains(
                serverOutput,
                output => output.SessionId == serverId &&
                    output.Message.Contains("SESSION_ARG=--server"));

            var clientId = manager.Start(
                project,
                "Client",
                applicationArguments: new[] { "--client" });

            var clientOutput = DrainUntil(
                manager,
                outputs => outputs.Any(output =>
                    output.SessionId == clientId &&
                    output.Message.Contains(
                        "SESSION_ARG=--client",
                        StringComparison.Ordinal)));

            Assert.Contains(
                clientOutput,
                output => output.SessionId == clientId &&
                    output.Message.Contains("SESSION_ARG=--client"));

            Assert.Equal(2, manager.Sessions.Count);

            Assert.True(
                manager.Sessions.Single(session =>
                    session.Id == serverId).IsActive);

            Assert.True(
                manager.Sessions.Single(session =>
                    session.Id == clientId).IsActive);

            // Остановка сервера не должна останавливать клиента.
            manager.Stop(serverId);

            Assert.False(
                manager.Sessions.Single(session =>
                    session.Id == serverId).IsActive);

            Assert.True(
                manager.Sessions.Single(session =>
                    session.Id == clientId).IsActive);

            manager.Stop(clientId);

            Assert.False(
                manager.Sessions.Single(session =>
                    session.Id == clientId).IsActive);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

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
    output => output.IsError &&
        output.Message.Contains(
            "CS",
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

    private static List<GameProjectSessionOutput> DrainUntil(
    GameProjectSessionManager manager,
    Func<List<GameProjectSessionOutput>, bool> complete,
    int timeoutSeconds = 90)
    {
        var outputs = new List<GameProjectSessionOutput>();
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < TimeSpan.FromSeconds(timeoutSeconds))
        {
            while (manager.TryDequeueOutput(out var output))
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
            "Timed out waiting for game sessions. Output:" +
            Environment.NewLine +
            string.Join(
                Environment.NewLine,
                outputs.Select(output =>
                    $"[{output.SessionName}] {output.Message}")));
    }
}