using Engine.Core.Application;
using Engine.Core.Simulations;
using Engine.Core.Systems;
using Engine.Core.Time;

namespace Engine.Simulations;

public abstract class SimulationState : IApplicationState
{
    protected SimulationState(
        ISimulation simulation)
    {
        ArgumentNullException.ThrowIfNull(simulation);

        Simulation = simulation;
    }

    protected ISimulation Simulation { get; }

    public virtual void Initialize(
        ApplicationStateContext context)
    {
        Simulation.Initialize();
    }

    public virtual void Update(
        TimeSnapshot time)
    {
        Simulation.Update(
            new SystemContext(time));
    }

    public virtual void FixedUpdate(
        SimulationTime time)
    {
        Simulation.FixedUpdate(
            new FixedSystemContext(time));
    }

    public virtual void Render(
        double interpolationAlpha)
    {
    }

    public virtual void Shutdown()
    {
        Simulation.Shutdown();
    }
}