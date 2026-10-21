using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace Engine.Editor.Projects;

public sealed record GameProjectOutput(
    bool IsError,
    string Message);

/// <summary>
/// Launches and controls a game project as a separate process.
/// </summary>
public sealed class GameProjectRunner : IDisposable
{
    private readonly ConcurrentQueue<GameProjectOutput> _output = new();

    private Process? _process;
    private bool _stopRequested;
    private bool _disposed;

    public bool IsRunning
    {
        get
        {
            var process = _process;

            if (process is null)
            {
                return false;
            }

            try
            {
                return !process.HasExited;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }

    public void Start(GameProject project)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(project);

        if (IsRunning)
        {
            throw new InvalidOperationException(
                "A game process is already running.");
        }

        ReleaseExitedProcess();

        var fullDirectory = Path.GetFullPath(
            project.RootDirectory);

        var projectFilePath = Path.GetFullPath(
            project.ProjectFilePath);

        if (!Directory.Exists(fullDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Project directory does not exist: {fullDirectory}");
        }

        if (!File.Exists(projectFilePath))
        {
            throw new FileNotFoundException(
                "Startup project file was not found.",
                projectFilePath);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = fullDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true
        };

        // Задаём язык интерфейса .NET CLI.
        // Это также помогает обеспечить UTF-8 для его вывода.
        startInfo.Environment["DOTNET_CLI_UI_LANGUAGE"] = "ru-RU";

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectFilePath);
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add("Debug");
        startInfo.ArgumentList.Add("--no-launch-profile");

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                _output.Enqueue(
                    new GameProjectOutput(false, args.Data));
            }
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                _output.Enqueue(
                    new GameProjectOutput(true, args.Data));
            }
        };

        process.Exited += (_, _) =>
        {
            try
            {
                var exitCode = process.ExitCode;

                _output.Enqueue(
                    new GameProjectOutput(
                        exitCode != 0 && !_stopRequested,
                        $"Game process exited with code {exitCode}."));
            }
            catch (InvalidOperationException)
            {
                // The process may already have been disposed.
            }
        };

        try
        {
            _stopRequested = false;
            if (!process.Start())
            {
                throw new InvalidOperationException(
                    "Failed to start the game process.");
            }

            _process = process;

            _output.Enqueue(
                new GameProjectOutput(
                    false,
                    $"Starting game project: {project.Name}"));

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
        catch
        {
            if (ReferenceEquals(_process, process))
            {
                _process = null;
            }

            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Preserve the original exception.
            }

            process.Dispose();
            throw;
        }
    }

    public void Stop()
    {
        var process = _process;

        if (process is null)
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                _stopRequested = true;
                process.Kill(entireProcessTree: true);

                _output.Enqueue(
                    new GameProjectOutput(
                        false,
                        "Stopping game process."));
            }

            // Дожидаемся завершения процесса и обработки
            // асинхронного вывода из перенаправленных потоков.
            process.WaitForExit();
        }
        catch (InvalidOperationException)
        {
            // Процесс мог завершиться до вызова Stop.
        }
        finally
        {
            if (process.HasExited)
            {
                if (ReferenceEquals(_process, process))
                {
                    _process = null;
                }

                process.Dispose();
            }
        }
    }

    public bool TryDequeueOutput(out GameProjectOutput? output)
    {
        return _output.TryDequeue(out output);
    }

    private void ReleaseExitedProcess()
    {
        var process = _process;

        if (process is null || !process.HasExited)
        {
            return;
        }

        process.WaitForExit();

        process.Dispose();
        _process = null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        var process = _process;
        _process = null;

        if (process is null)
        {
            return;
        }
        _stopRequested = true;
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            process.WaitForExit(1000);
        }
        catch
        {
            // Do not prevent editor shutdown.
        }
        finally
        {
            process.Dispose();
        }
    }
}