using Engine.Serialization.Binary;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Worlds;

namespace Engine.Serialization.SaveLoad.Game;

public sealed class GameSaveStateSerializer :
    IBinarySerializer<GameSaveState>
{
    public const int CurrentVersion = 1;

    private readonly WorldSaveStateSerializer _worldSerializer;
    private readonly PersistentStateRegistry _persistentStates;

    public GameSaveStateSerializer(
        WorldSaveStateSerializer worldSerializer,
        PersistentStateRegistry persistentStates)
    {
        ArgumentNullException.ThrowIfNull(
            worldSerializer);

        ArgumentNullException.ThrowIfNull(
            persistentStates);

        _worldSerializer =
            worldSerializer;

        _persistentStates =
            persistentStates;
    }

    public void Serialize(
        ref SerializationWriter writer,
        GameSaveState value)
    {
        ArgumentNullException.ThrowIfNull(
            value);

        if (value.Version !=
            CurrentVersion)
        {
            throw new InvalidOperationException(
                $"Game save version '{value.Version}' cannot be written. " +
                $"Expected current version '{CurrentVersion}'.");
        }

        writer.WriteInt32(
            value.Version);

        _worldSerializer.Serialize(
            ref writer,
            value.World);

        if (value.PersistentStates.Count >
            writer.Context.MaxCollectionLength)
        {
            throw new InvalidDataException(
                "Persistent state count exceeds the maximum allowed collection length.");
        }

        _persistentStates.Validate(
            value.PersistentStates);

        var states =
            value.PersistentStates
                .OrderBy(
                    state => state.Id,
                    StringComparer.Ordinal)
                .ToArray();

        writer.WriteInt32(
            states.Length);

        foreach (var state in states)
        {
            writer.WriteString(
                state.Id);

            var entry =
                _persistentStates.GetEntry(
                    state.Id);

            entry.Serialize(
                ref writer,
                state.Value);
        }
    }

    public GameSaveState Deserialize(
        ref SerializationReader reader)
    {
        var version =
            reader.ReadInt32();

        if (version < 1)
        {
            throw new InvalidDataException(
                $"Game save version '{version}' is invalid.");
        }

        if (version >
            CurrentVersion)
        {
            throw new InvalidDataException(
                $"Game save version '{version}' is newer than " +
                $"the supported version '{CurrentVersion}'.");
        }

        var world =
            _worldSerializer.Deserialize(
                ref reader);

        var count =
            ReadCount(
                ref reader);

        var states =
            new List<PersistentStateValue>(
                count);

        for (var i = 0;
             i < count;
             i++)
        {
            var id =
                reader.ReadString();

            if (id is null)
            {
                throw new InvalidDataException(
                    "Persistent state ID cannot be null.");
            }

            var entry =
                _persistentStates.GetEntry(
                    id);

            states.Add(
                new PersistentStateValue(
                    id,
                    entry.Deserialize(
                        ref reader)));
        }

        _persistentStates.Validate(
            states);

        return new GameSaveState(
            version,
            world,
            states);
    }

    private static int ReadCount(
        ref SerializationReader reader)
    {
        var count =
            reader.ReadInt32();

        if (count < 0)
        {
            throw new InvalidDataException(
                $"Serialized collection length '{count}' is invalid.");
        }

        if (count >
            reader.Context.MaxCollectionLength)
        {
            throw new InvalidDataException(
                $"Serialized collection length '{count}' exceeds " +
                $"the maximum allowed length '{reader.Context.MaxCollectionLength}'.");
        }

        return count;
    }
}