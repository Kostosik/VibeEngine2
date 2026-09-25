namespace Engine.Core.Application;

public interface IApplicationState
{
    void Initialize(ApplicationStateContext context);

    void Update(
        Engine.Core.Time.TimeSnapshot time);

    void FixedUpdate(
        Engine.Core.Time.SimulationTime time);

    void Render(
        double interpolationAlpha);

    void Shutdown();
}