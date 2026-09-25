using Engine.Core.Time;

namespace Engine.Tests.Application;

internal sealed class TestClock : IClock
{
    public TimeSpan Elapsed { get; private set; }

    public bool IsRunning { get; private set; }

    public void Start()
    {
        IsRunning = true;
    }

    public void Restart()
    {
        Elapsed = TimeSpan.Zero;
        IsRunning = true;
    }

    public void Stop()
    {
        IsRunning = false;
    }

    public void Advance(
        TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(elapsed));
        }

        Elapsed += elapsed;
    }
}