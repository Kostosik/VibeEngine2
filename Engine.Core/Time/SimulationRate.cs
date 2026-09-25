using Engine.Core.Math;
using Engine.Core.Time;

namespace Engine.Core.Time;

public readonly record struct SimulationRate
{
    public SimulationRate(
        int ticksPerSecond)
    {
        if (ticksPerSecond <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ticksPerSecond));
        }

        TicksPerSecond = ticksPerSecond;
    }

    public int TicksPerSecond { get; }

    public Duration TickDuration =>
        Duration.FromSeconds(
            1.0 / TicksPerSecond);

    public Fixed32 SimulationDelta =>
        Fixed32.FromRatio(
            1,
            TicksPerSecond);
}