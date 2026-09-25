using Engine.Core.Time;

namespace Engine.Core.Application;

public abstract class Application : IApplication
{
    public virtual void Initialize()
    {
    }

    public virtual void Update(
        TimeSnapshot time)
    {
    }

    public virtual void FixedUpdate(
        SimulationTime time)
    {
    }

    public virtual void Render(
        double interpolationAlpha)
    {
    }

    public virtual void Shutdown()
    {
    }
}