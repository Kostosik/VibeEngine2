using Engine.Core.Application;
using Engine.Core.Time;
using Silk.NET.OpenGL;

namespace Engine.Tests.Application;

internal sealed class TestApplication : IApplication
{
    public int InitializeCount { get; private set; }

    public int UpdateCount { get; private set; }

    public int FixedUpdateCount { get; private set; }

    public int RenderCount { get; private set; }

    public int ShutdownCount { get; private set; }

    public List<TimeSnapshot> Updates { get; } = new();

    public List<SimulationTime> FixedUpdates { get; } = new();

    public void Initialize()
    {
        InitializeCount++;
    }



    public void Update(
        TimeSnapshot time)
    {
        UpdateCount++;

        Updates.Add(time);
    }

    public void FixedUpdate(
        SimulationTime time)
    {
        FixedUpdateCount++;

        FixedUpdates.Add(time);
    }

    public void Render(double delta)
    {
        RenderCount++;
    }

    public void Shutdown()
    {
        ShutdownCount++;
    }
}