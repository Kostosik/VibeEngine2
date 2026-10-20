using System.Collections.Concurrent;
using System.Diagnostics;

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

    public void Start(string projectDirectory)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);

        if (IsRunning)
        {
            throw new InvalidOperationException(
                "A game process is already running.");
        }

        ReleaseExitedProcess();

        var fullDirectory = Path.GetFullPath(projectDirectory);

        if (!Directory.Exists(fullDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Project directory does not exist: {fullDirectory}");
        }

        var projectFiles = Directory.GetFiles(
            fullDirectory,
            "*.csproj",
            SearchOption.TopDirectoryOnly);

        if (projectFiles.Length != 1)
        {
            throw new InvalidOperationException(
                "The project directory must contain exactly one .csproj file.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = fullDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectFiles[0]);
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
                _output.Enqueue(
                    new GameProjectOutput(
                        false,
                        $"Game process exited with code {process.ExitCode}."));
            }
            catch (InvalidOperationException)
            {
                // The process may already have been disposed.
            }
        };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException(
                    "Failed to start the game process.");
            }

            _process = process;

            _output.Enqueue(
                new GameProjectOutput(
                    false,
                    $"Starting game project: {Path.GetFileName(fullDirectory)}"));

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
            if (process.HasExited)
            {
                return;
            }

            process.Kill(entireProcessTree: true);

            _output.Enqueue(
                new GameProjectOutput(
                    false,
                    "Stopping game process."));
        }
        catch (InvalidOperationException)
        {
            // The process may have exited before Stop was called.
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