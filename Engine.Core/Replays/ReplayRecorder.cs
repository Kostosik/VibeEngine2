using Engine.Core.Commands;
using Engine.Core.Time;

namespace Engine.Core.Replays;

public sealed class ReplayRecorder : IDisposable
{
    private readonly Replay _replay;
    private readonly CommandDispatcher _dispatcher;

    private Tick _currentTick;

    private bool _recording;
    private bool _disposed;

    public ReplayRecorder(
        Replay replay,
        CommandDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(replay);
        ArgumentNullException.ThrowIfNull(dispatcher);

        _replay = replay;
        _dispatcher = dispatcher;

        _dispatcher.CommandDispatched +=
            OnCommandDispatched;
    }

    public bool IsRecording =>
        _recording;

    public void Start(
        Tick initialTick)
    {
        EnsureNotDisposed();

        if (_recording)
        {
            throw new InvalidOperationException(
                "Replay recording has already started.");
        }

        _currentTick = initialTick;
        _recording = true;
    }

    public void SetTick(
        Tick tick)
    {
        EnsureNotDisposed();

        if (!_recording)
        {
            throw new InvalidOperationException(
                "Replay recording has not started.");
        }

        _currentTick = tick;
    }

    public void Stop()
    {
        EnsureNotDisposed();

        _recording = false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _dispatcher.CommandDispatched -=
            OnCommandDispatched;

        _recording = false;
        _disposed = true;
    }

    private void OnCommandDispatched(
        ICommand command)
    {
        if (!_recording)
        {
            return;
        }

        _replay
            .GetOrCreateFrame(_currentTick)
            .Add(command);
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}