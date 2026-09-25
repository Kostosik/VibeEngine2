using Engine.Worlds.Spatial;

namespace Engine.Worlds.Chunks;

public sealed class ChunkLifecycle
{
    public ChunkLifecycle(
        ChunkPosition position)
    {
        Position =
            position;

        Residency =
            ChunkResidencyState.Unloaded;

        Simulation =
            ChunkSimulationState.Suspended;

        Presentation =
            ChunkPresentationState.Irrelevant;
    }

    public ChunkPosition Position { get; }

    public ChunkResidencyState Residency { get; private set; }

    public ChunkSimulationState Simulation { get; private set; }

    public ChunkPresentationState Presentation { get; private set; }

    public void BeginLoading()
    {
        EnsureResidency(
            ChunkResidencyState.Unloaded);

        Residency =
            ChunkResidencyState.Loading;
    }

    public void CompleteLoading()
    {
        EnsureResidency(
            ChunkResidencyState.Loading);

        Residency =
            ChunkResidencyState.Loaded;

        Simulation =
            ChunkSimulationState.Simulating;
    }

    public void CancelLoading()
    {
        EnsureResidency(
            ChunkResidencyState.Loading);

        Residency =
            ChunkResidencyState.Unloaded;
    }

    public void BeginUnloading()
    {
        EnsureResidency(
            ChunkResidencyState.Loaded);

        if (Simulation !=
            ChunkSimulationState.Suspended)
        {
            throw new InvalidOperationException(
                "A simulating chunk cannot begin unloading.");
        }

        Residency =
            ChunkResidencyState.Unloading;
    }

    public void CompleteUnloading()
    {
        EnsureResidency(
            ChunkResidencyState.Unloading);

        Residency =
            ChunkResidencyState.Unloaded;

        Simulation =
            ChunkSimulationState.Suspended;

        Presentation =
            ChunkPresentationState.Irrelevant;
    }

    public void SetSimulation(
        ChunkSimulationState state)
    {
        if (state ==
            ChunkSimulationState.Simulating &&
            Residency !=
            ChunkResidencyState.Loaded)
        {
            throw new InvalidOperationException(
                "A chunk must be loaded before simulation can be enabled.");
        }

        Simulation =
            state;
    }

    public void SetPresentation(
        ChunkPresentationState state)
    {
        Presentation =
            state;
    }

    private void EnsureResidency(
        ChunkResidencyState expected)
    {
        if (Residency != expected)
        {
            throw new InvalidOperationException(
                $"Chunk '{Position}' is in residency state " +
                $"'{Residency}', expected '{expected}'.");
        }
    }
}