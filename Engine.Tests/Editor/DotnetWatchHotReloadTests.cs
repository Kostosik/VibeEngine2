
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Engine.Tests.Editor;

public sealed class DotnetWatchHotReloadTests
{
    [Fact]
    public async Task DotnetWatch_AppliesMethodBodyChange_WithoutRestartingApp()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineHotReload-{Guid.NewGuid():N}");

        Directory.CreateDirectory(root);

        Process? watcher = null;

        var appProcesses = new Dictionary<int, Process>();

        void TrackApp(int pid)
        {
            if (appProcesses.ContainsKey(pid))
            {
                return;
            }

            try
            {
                appProcesses.Add(pid, Process.GetProcessById(pid));
            }
            catch (ArgumentException)
            {
                // Процесс уже завершился.
            }
        }

        try
        {
            var projectPath = Path.Combine(
                root,
                "HotReloadProbe.csproj");

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

            WriteProgram(sourcePath, "V1");

            var output = new ConcurrentQueue<string>();

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = root,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                CreateNoWindow = true
            };

            startInfo.Environment["DOTNET_NOLOGO"] = "1";
            startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
            startInfo.Environment["DOTNET_WATCH_SUPPRESS_EMOJIS"] = "1";

            startInfo.ArgumentList.Add("watch");
            startInfo.ArgumentList.Add("run");
            startInfo.ArgumentList.Add("--project");
            startInfo.ArgumentList.Add(projectPath);
            startInfo.ArgumentList.Add("--non-interactive");
            startInfo.ArgumentList.Add("--verbose");

            watcher = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            watcher.OutputDataReceived += (_, args) =>
            {
                if (args.Data is not null)
                {
                    output.Enqueue(args.Data);
                }
            };

            watcher.ErrorDataReceived += (_, args) =>
            {
                if (args.Data is not null)
                {
                    output.Enqueue("[stderr] " + args.Data);
                }
            };

            Assert.True(
                watcher.Start(),
                "Failed to start dotnet watch.");

            watcher.BeginOutputReadLine();
            watcher.BeginErrorReadLine();

            var firstRun = WaitForOutput(
                watcher,
                output,
                "HOTRELOAD_VERSION=V1",
                TimeSpan.FromSeconds(90));

            var originalPid = ExtractPid(firstRun);
            TrackApp(originalPid);
            var originalIteration = ExtractWatchIteration(firstRun);

            // Дожидаемся готовности наблюдателя к изменениям.
            _ = WaitForOutput(
                watcher,
                output,
                "Waiting for changes",
                TimeSpan.FromSeconds(30));

            // Меняем только тело метода.
            WriteProgram(sourcePath, "V2");

            // Ждём фактического применения нового кода.
            var updatedRun = WaitForOutput(
                watcher,
                output,
                "HOTRELOAD_VERSION=V2",
                TimeSpan.FromSeconds(60));

            var updatedPid = ExtractPid(updatedRun);
            TrackApp(updatedPid);
            // Hot Reload должен сохранить процесс игры.
            Assert.Equal(originalPid, updatedPid);

            // Имитируем ошибку компиляции.
            File.WriteAllText(
                sourcePath,
                """
    internal static class Program
    {
        private static string GetVersion() => ;
    }
    """);

            // Ждём диагностическое сообщение компилятора.
            _ = WaitForOutput(
                watcher,
                output,
                "error CS",
                TimeSpan.FromSeconds(60));

            // Наблюдатель должен оставаться работающим.
            Assert.False(
                watcher.HasExited,
                "dotnet watch exited after a compilation error.");

            // Исправляем код и проверяем восстановление.
            WriteProgram(sourcePath, "V3");

            var recoveredRun = WaitForOutput(
                watcher,
                output,
                "HOTRELOAD_VERSION=V3",
                TimeSpan.FromSeconds(60));
            TrackApp(ExtractPid(recoveredRun));
            Assert.Equal(
    originalIteration,
    ExtractWatchIteration(recoveredRun));

            // После исправления должен обновиться код,
            // а PID приложения должен остаться прежним.

            // Меняем сигнатуру метода — такое изменение
            // требует перезапуска приложения.
            WriteProgramWithEntryPointChange(sourcePath);

            var restartedRun = WaitForOutput(
                watcher,
                output,
                "HOTRELOAD_VERSION=V4",
                TimeSpan.FromSeconds(60));

            var restartedPid = ExtractPid(restartedRun);

            // Приложение должно было запуститься заново.
            Assert.NotEqual(
                ExtractWatchIteration(recoveredRun),
                ExtractWatchIteration(restartedRun));
        }
        finally
        {
            if (watcher is not null)
            {
                try
                {
                    if (!watcher.HasExited)
                    {
                        watcher.Kill(entireProcessTree: true);
                    }

                    watcher.WaitForExit();
                }
                catch
                {
                    // Не скрываем результат теста из-за очистки.
                }
                finally
                {
                    watcher.Dispose();
                }
            }

            foreach (var app in appProcesses.Values)
            {
                try
                {
                    if (!app.HasExited)
                    {
                        app.Kill(entireProcessTree: true);
                    }

                    await app.WaitForExitAsync()
                        .WaitAsync(TimeSpan.FromSeconds(5));
                }
                finally
                {
                    app.Dispose();
                }
            }

            if (Directory.Exists(root))
            {
                await ProcessTestCleanup.DeleteDirectoryWithRetryAsync(root);
            }
        }
    }


    [Fact]
    public void DotnetWatch_RemainsWatchingAfterApplicationExits()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineWatchExit-{Guid.NewGuid():N}");

        Directory.CreateDirectory(root);

        Process? watcher = null;

        try
        {
            var projectPath = Path.Combine(
                root,
                "WatchExitProbe.csproj");

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

            // Приложение печатает сообщение и сразу завершается.
            File.WriteAllText(
                sourcePath,
                """
            internal static class Program
            {
                private static void Main()
                {
                    Console.WriteLine(
                        $"HOTRELOAD_APP_EXITED PID={Environment.ProcessId}");
                }
            }
            """);

            var output = new ConcurrentQueue<string>();

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = root,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                CreateNoWindow = true
            };

            startInfo.Environment["DOTNET_NOLOGO"] = "1";
            startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
            startInfo.Environment["DOTNET_WATCH_SUPPRESS_EMOJIS"] = "1";

            startInfo.ArgumentList.Add("watch");
            startInfo.ArgumentList.Add("run");
            startInfo.ArgumentList.Add("--project");
            startInfo.ArgumentList.Add(projectPath);
            startInfo.ArgumentList.Add("--non-interactive");
            startInfo.ArgumentList.Add("--verbose");

            watcher = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            watcher.OutputDataReceived += (_, args) =>
            {
                if (args.Data is not null)
                    output.Enqueue(args.Data);
            };

            watcher.ErrorDataReceived += (_, args) =>
            {
                if (args.Data is not null)
                    output.Enqueue("[stderr] " + args.Data);
            };

            Assert.True(watcher.Start());

            watcher.BeginOutputReadLine();
            watcher.BeginErrorReadLine();

            var exitOutput = WaitForOutput(
                watcher,
                output,
                "HOTRELOAD_APP_EXITED",
                TimeSpan.FromSeconds(90));

            var gamePid = ExtractPid(exitOutput);

            // Доказываем, что завершилось именно приложение.
            WaitForProcessExit(
                gamePid,
                TimeSpan.FromSeconds(15));

            // Наблюдатель должен продолжить работу.
            Assert.True(
                IsAliveFor(watcher, TimeSpan.FromSeconds(2)),
                "dotnet watch exited together with the application.");

            // Меняем исходник после завершения приложения.
            // Наблюдатель должен снова запустить проект.
            WriteProgram(sourcePath, "V2");

            var restartedRun = WaitForOutput(
                watcher,
                output,
                "HOTRELOAD_VERSION=V2",
                TimeSpan.FromSeconds(60));

            Assert.Contains(
                "HOTRELOAD_VERSION=V2",
                restartedRun,
                StringComparison.Ordinal);
        }
        finally
        {
            if (watcher is not null)
            {
                try
                {
                    if (!watcher.HasExited)
                        watcher.Kill(entireProcessTree: true);

                    watcher.WaitForExit();
                }
                catch
                {
                    // Не скрываем результат теста при очистке.
                }
                finally
                {
                    watcher.Dispose();
                }
            }

            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }


    private static void WriteProgramThatExits(string path)
    {
        File.WriteAllText(
            path,
            """
        namespace ChangedEntryPoint;

        internal static class Program
        {
            private static async Task Main()
            {
                while (ShouldContinue())
                {
                    var iteration =
                        Environment.GetEnvironmentVariable(
                            "DOTNET_WATCH_ITERATION") ?? "missing";

                    Console.WriteLine(
                        $"HOTRELOAD_VERSION=V4 PID={Environment.ProcessId} " +
                        $"WATCH_ITERATION={iteration}");

                    await Task.Delay(1000);
                }

                Console.WriteLine(
                    $"HOTRELOAD_APP_EXITED PID={Environment.ProcessId}");
            }

            private static bool ShouldContinue() => false;

            private static string GetVersion(int marker) => "V4";
        }
        """ + Environment.NewLine);
    }

    private static void WaitForProcessExit(
        int pid,
        TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                using var process = Process.GetProcessById(pid);

                if (process.HasExited)
                {
                    return;
                }
            }
            catch (ArgumentException)
            {
                // Процесс уже завершился.
                return;
            }

            Thread.Sleep(25);
        }

        throw new TimeoutException(
            $"Application process {pid} did not exit.");
    }

    private static bool IsAliveFor(
        Process process,
        TimeSpan duration)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < duration)
        {
            if (process.HasExited)
            {
                return false;
            }

            Thread.Sleep(25);
        }

        return !process.HasExited;
    }

    private static void WriteProgramWithEntryPointChange(
        string path)
    {
        File.WriteAllText(
            path,
            """
        namespace ChangedEntryPoint;

        internal static class Program
        {
            private static async Task Main()
            {
                while (ShouldContinue())
                {
                    var iteration =
                        Environment.GetEnvironmentVariable(
                            "DOTNET_WATCH_ITERATION") ?? "missing";

                    Console.WriteLine(
                        $"HOTRELOAD_VERSION=V4 PID={Environment.ProcessId} " +
                        $"WATCH_ITERATION={iteration}");

                    await Task.Delay(1000);
                }

                Console.WriteLine(
                    $"HOTRELOAD_APP_EXITED PID={Environment.ProcessId}");
            }

            private static bool ShouldContinue() => true;

            private static string GetVersion(int marker) => "V4";
        }
        """ + Environment.NewLine);
    }

    private static void WriteProgramWithSignatureChange(
    string path)
    {
        File.WriteAllText(
            path,
            """
        internal static class Program
        {
            private static async Task Main()
            {
                while (true)
                {
                    var watchIteration =
        Environment.GetEnvironmentVariable("DOTNET_WATCH_ITERATION")
        ?? "missing";

            Console.WriteLine(
        $"HOTRELOAD_VERSION={GetVersion(42)} " +
        $"PID={Environment.ProcessId} " +
        $"WATCH_ITERATION={watchIteration}");
                    await Task.Delay(1000);
                }
            }

            private static string GetVersion(int marker) => "V4";
        }
        """ + Environment.NewLine);
    }

    private static int ExtractWatchIteration(string line)
    {
        var match = Regex.Match(
            line,
            @"\bWATCH_ITERATION=(\d+)\b");

        Assert.True(
            match.Success,
            $"Could not extract watch iteration from: {line}");

        return int.Parse(match.Groups[1].Value);
    }

    private static void WriteProgram(
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
            var watchIteration =
                Environment.GetEnvironmentVariable("DOTNET_WATCH_ITERATION")
                ?? "missing";

            Console.WriteLine(
                $"HOTRELOAD_VERSION={GetVersion()} " +
                $"PID={Environment.ProcessId} " +
                $"WATCH_ITERATION={watchIteration}");

                        await Task.Delay(1000);
                    }
                }

                private static string GetVersion() => "{{version}}";
            }
            """ + Environment.NewLine);
    }

    private static int ExtractPid(string line)
    {
        var match = Regex.Match(
            line,
            @"\bPID=(\d+)\b");

        Assert.True(
            match.Success,
            $"Could not extract application PID from: {line}");

        return int.Parse(match.Groups[1].Value);
    }

    private static string WaitForOutput(
        Process process,
        ConcurrentQueue<string> output,
        string expected,
        TimeSpan timeout)
    {
        var collected = new List<string>();
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            while (output.TryDequeue(out var line))
            {
                collected.Add(line);

                if (line.Contains(
                    expected,
                    StringComparison.Ordinal))
                {
                    return line;
                }
            }

            if (process.HasExited)
            {
                process.WaitForExit();

                while (output.TryDequeue(out var line))
                {
                    collected.Add(line);
                }

                throw new InvalidOperationException(
                    $"dotnet watch exited with code {process.ExitCode} " +
                    $"before producing '{expected}'.{Environment.NewLine}" +
                    string.Join(Environment.NewLine, collected));
            }

            Thread.Sleep(25);
        }

        var diagnostics = collected
    .Where(line => !line.Contains(
        "HOTRELOAD_VERSION=",
        StringComparison.Ordinal))
    .TakeLast(100);

        throw new TimeoutException(
            $"Timed out waiting for '{expected}'." +
            Environment.NewLine +
            string.Join(
                Environment.NewLine,
                diagnostics));

        throw new TimeoutException(
            $"Timed out waiting for '{expected}'." +
            Environment.NewLine +
            string.Join(
                Environment.NewLine,
                collected.TakeLast(50)));
    }
}
