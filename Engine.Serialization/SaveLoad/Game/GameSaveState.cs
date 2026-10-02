using Engine.Worlds.Persistence;

namespace Engine.Serialization.SaveLoad.Game;

public sealed class GameSaveState
{
    public GameSaveState(
        int version,
        WorldSaveState world,
        IReadOnlyList<PersistentStateValue> persistentStates)
    {
        if (version < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(version));
        }

        ArgumentNullException.ThrowIfNull(
            world);

        ArgumentNullException.ThrowIfNull(
            persistentStates);

        Version =
            version;

        World =
            world;

        PersistentStates =
            persistentStates.ToArray();
    }

    public int Version { get; }

    public WorldSaveState World { get; }

    public IReadOnlyList<PersistentStateValue> PersistentStates { get; }
}