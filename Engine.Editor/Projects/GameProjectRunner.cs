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
    private int _hotReloadIdleSignal;
    private Process? _process;

    private TaskCompletionSource<bool>? _outputReadersCompleted;

    private static readonly TimeSpan ProcessExitTimeout =
        TimeSpan.FromSeconds(5);

    private static readonly TimeSpan OutputDrainTimeout =
        TimeSpan.FromSeconds(1);

    private volatile bool _stopRequested;
    private bool _disposed;
    private GameProjectRunMode? _activeRunMode;
    /// <summary>
    /// Indicates whether the controlled process or watch session is active.
    /// In Hot Reload mode, this represents the watcher session,
    /// not necessarily the lifetime of the game process.
    /// </summary>
    public bool IsSessionActive
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

    /// <summary>
    /// The mode of the currently active session.
    /// </summary>
    public GameProjectRunMode? ActiveRunMode =>
        IsSessionActive ? _activeRunMode : null;

    /// <summary>
    /// Compatibility property for existing callers.
    /// In Hot Reload mode, this means the watcher session is active.
    /// </summary>
    public bool IsRunning => IsSessionActive;

    public void Start(
        GameProject project,
        GameProjectRunMode mode = GameProjectRunMode.Standard,
        IReadOnlyList<string>? applicationArguments = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(project);


        if (IsRunning)
        {
            throw new InvalidOperationException(
                "A game process is already running.");
        }

        System.Threading.Interlocked.Exchange(
    ref _hotReloadIdleSignal,
    0);

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

        switch (mode)
        {
            case GameProjectRunMode.Standard:
                startInfo.ArgumentList.Add("run");
                startInfo.ArgumentList.Add("--project");
                startInfo.ArgumentList.Add(projectFilePath);
                startInfo.ArgumentList.Add("--configuration");
                startInfo.ArgumentList.Add("Debug");
                startInfo.ArgumentList.Add("--no-launch-profile");
                break;

            case GameProjectRunMode.HotReload:
                startInfo.Environment["DOTNET_WATCH_SUPPRESS_EMOJIS"] = "1";

                startInfo.ArgumentList.Add("watch");
                startInfo.ArgumentList.Add("--project");
                startInfo.ArgumentList.Add(projectFilePath);
                startInfo.ArgumentList.Add("--non-interactive");
                startInfo.ArgumentList.Add("--verbose");
                startInfo.ArgumentList.Add("run");
                startInfo.ArgumentList.Add("--configuration");
                startInfo.ArgumentList.Add("Debug");
                startInfo.ArgumentList.Add("--no-launch-profile");
                break;



            default:
                throw new ArgumentOutOfRangeException(
                    nameof(mode),
                    mode,
                    "Unsupported game project run mode.");
        }

        if (applicationArguments is { Count: > 0 })
        {
            // Отделяем аргументы приложения от параметров .NET CLI.
            startInfo.ArgumentList.Add("--");

            foreach (var argument in applicationArguments)
            {
                if (argument is null)
                {
                    throw new ArgumentException(
                        "Application arguments cannot contain null values.",
                        nameof(applicationArguments));
                }

                startInfo.ArgumentList.Add(argument);
            }
        }

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        var completedOutputStreams = 0;

        var outputReadersCompleted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        void MarkOutputStreamCompleted()
        {
            if (System.Threading.Interlocked.Increment(
                    ref completedOutputStreams) == 2)
            {
                outputReadersCompleted.TrySetResult(true);
            }
        }

        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is null)
            {
                MarkOutputStreamCompleted();
                return;
            }
            if (args.Data is not null)
            {
                if (mode == GameProjectRunMode.HotReload &&
                    args.Data.Contains(
                        "Waiting for a file to change before restarting",
                        StringComparison.OrdinalIgnoreCase))
                {
                    System.Threading.Interlocked.Exchange(
                        ref _hotReloadIdleSignal,
                        1);
                }

                _output.Enqueue(
                    new GameProjectOutput(
                        IsErrorOutput(
                            args.Data,
                            mode,
                            isStandardError: false),
                        args.Data));
            }
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                if (mode == GameProjectRunMode.HotReload &&
                    args.Data.Contains(
                        "Waiting for a file to change before restarting",
                        StringComparison.OrdinalIgnoreCase))
                {
                    System.Threading.Interlocked.Exchange(
                        ref _hotReloadIdleSignal,
                        1);
                }

                _output.Enqueue(
                    new GameProjectOutput(
                        IsErrorOutput(
                            args.Data,
                            mode,
                            isStandardError: true),
                        args.Data));
            }
        };

        process.Exited += (_, _) =>
        {
            try
            {
                var exitCode = process.ExitCode;

                if (_stopRequested)
                {
                    return;
                }

                _output.Enqueue(
                    new GameProjectOutput(
                        exitCode != 0,
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
            _activeRunMode = mode;
            _outputReadersCompleted = outputReadersCompleted;
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
                _activeRunMode = null;
                _outputReadersCompleted = null;
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

    public bool TryConsumeHotReloadIdleSignal()
    {
        return System.Threading.Interlocked.Exchange(
            ref _hotReloadIdleSignal,
            0) != 0;
    }

    private static bool IsErrorOutput(
    string message,
    GameProjectRunMode mode,
    bool isStandardError)
    {
        if (mode == GameProjectRunMode.Standard &&
            isStandardError)
        {
            return true;
        }

        return message.Contains(
                   "error",
                   StringComparison.OrdinalIgnoreCase)
               || message.Contains(
                   "failed",
                   StringComparison.OrdinalIgnoreCase)
               || message.Contains(
                   "fatal",
                   StringComparison.OrdinalIgnoreCase);
    }

    public void Stop()
    {
        var process = _process;

        if (process is null)
        {
            return;
        }

        var outputReadersCompleted = _outputReadersCompleted;
        _stopRequested = true;

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);

                _output.Enqueue(
                    new GameProjectOutput(
                        false,
                        "Stopping game process."));
            }

            if (!process.WaitForExit(
                    (int)ProcessExitTimeout.TotalMilliseconds))
            {
                _output.Enqueue(
                    new GameProjectOutput(
                        true,
                        "Timed out waiting for the game process to exit."));

                return;
            }

            // Даём stdout/stderr время дочитать последние сообщения,
            // но не ждём бесконечно, если дочерний процесс удерживает поток.
            _ = outputReadersCompleted?.Task.Wait(OutputDrainTimeout);
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
                    _activeRunMode = null;
                    _outputReadersCompleted = null;
                }

                process.Dispose();
            }
        }
    }

    public bool TryDequeueOutput(out GameProjectOutput? output)
    {
        return _output.TryDequeue(out output);
    }

    internal void ReleaseExitedProcess()
    {
        var process = _process;

        if (process is null || !process.HasExited)
        {
            return;
        }

        // Не блокируем редактор, если дочерний процесс
        // продолжает удерживать перенаправленный вывод.
        _ = _outputReadersCompleted?.Task.Wait(OutputDrainTimeout);

        if (ReferenceEquals(_process, process))
        {
            _process = null;
            _activeRunMode = null;
            _outputReadersCompleted = null;
        }

        process.Dispose();
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
        _activeRunMode = null;
        _outputReadersCompleted = null;

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