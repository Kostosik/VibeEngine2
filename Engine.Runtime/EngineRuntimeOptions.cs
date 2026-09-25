using Engine.Core.Time;
using Engine.Physics;
using Engine.Worlds.Spatial;

namespace Engine.Runtime;

public sealed class EngineRuntimeOptions
{
    public EngineRuntimeOptions()
    {
        ChunkSize =
            new ChunkSize(
                32,
                32);

        SimulationRate =
            new SimulationRate(
                20);

        Physics =
            new PhysicsSettings2D();

        ToolingEnabled =
            true;
    }

    public ChunkSize ChunkSize { get; }

    public SimulationRate SimulationRate { get; }

    public PhysicsSettings2D Physics { get; }

    public bool ToolingEnabled { get; }
}