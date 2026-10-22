
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Engine.Tests.Editor;

public sealed class DotnetWatchHotReloadTests
{
    [Fact]
    public void DotnetWatch_AppliesMethodBodyChange_WithoutRestartingApp()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineHotReload-{Guid.NewGuid():N}");

        Directory.CreateDirectory(root);

        Process? watcher = null;

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

            // Hot Reload должен сохранить процесс игры.
            Assert.Equal(originalPid, updatedPid);
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

            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
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
                        Console.WriteLine(
                            $"HOTRELOAD_VERSION={GetVersion()} PID={Environment.ProcessId}");

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
