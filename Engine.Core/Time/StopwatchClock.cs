using System.Diagnostics;

namespace Engine.Core.Time;

public sealed class StopwatchClock : IClock
{
    private readonly Stopwatch _stopwatch = new();

    public TimeSpan Elapsed =>
        _stopwatch.Elapsed;

    public bool IsRunning =>
        _stopwatch.IsRunning;

    public void Start()
    {
        _stopwatch.Start();
    }

    public void Restart()
    {
        _stopwatch.Restart();
    }

    public void Stop()
    {
        _stopwatch.Stop();
    }
}