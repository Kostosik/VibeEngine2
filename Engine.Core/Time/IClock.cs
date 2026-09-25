namespace Engine.Core.Time;

public interface IClock
{
    TimeSpan Elapsed { get; }

    bool IsRunning { get; }

    void Start();

    void Restart();

    void Stop();
}