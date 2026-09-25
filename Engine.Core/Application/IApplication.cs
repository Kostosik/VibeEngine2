using Engine.Core.Time;

namespace Engine.Core.Application;

public interface IApplication
{
    void Initialize();

    void Update(TimeSnapshot time);

    void FixedUpdate(SimulationTime time);

    void Render(
    double interpolationAlpha);

    void Shutdown();
}