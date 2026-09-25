using Engine.ECS;
using Engine.ECS.Entities;
using Engine.ECS.Persistence;
using Engine.Serialization.Binary;

namespace Engine.Serialization.SaveLoad.Ecs;

public sealed class EcsWorldStateSerializer :
    IBinarySerializer<EcsWorldState>
{
    private readonly EcsComponentSerializerRegistry _registry;

    public EcsWorldStateSerializer(
        EcsComponentSerializerRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(
            registry);

        _registry =
            registry;
    }

    public void Serialize(
        ref SerializationWriter writer,
        EcsWorldState value)
    {
        ArgumentNullException.ThrowIfNull(
            value);

        WriteEntityState(
            ref writer,
            value.Entities);

        if (value.Components.Count >
            writer.Context.MaxCollectionLength)
        {
            throw new InvalidDataException(
                "Component storage count exceeds the maximum allowed collection length.");
        }

        writer.WriteInt32(
            value.Components.Count);

        var components =
            value.Components
                .OrderBy(
                    component =>
                        _registry
                            .GetByType(
                                component.ComponentType)
                            .Id,
                    StringComparer.Ordinal)
                .ToArray();

        foreach (var component in components)
        {
            var entry =
                _registry.GetByType(
                    component.ComponentType);

            writer.WriteString(
                entry.Id);

            if (component.Count >
                writer.Context.MaxCollectionLength)
            {
                throw new InvalidDataException(
                    $"Component count for '{entry.Id}' exceeds the maximum allowed collection length.");
            }

            writer.WriteInt32(
                component.Count);

            entry.Serialize(
                ref writer,
                component);
        }
    }

    public EcsWorldState Deserialize(
        ref SerializationReader reader)
    {
        var entities =
            ReadEntityState(
                ref reader);

        var componentCount =
            ReadCount(
                ref reader,
                reader.Context.MaxCollectionLength);

        var components =
            new List<WorldComponentState>(
                componentCount);

        for (var i = 0;
             i < componentCount;
             i++)
        {
            var id =
                reader.ReadString();

            if (id is null)
            {
                throw new InvalidDataException(
                    "ECS component serializer ID cannot be null.");
            }

            var entry =
                _registry.GetById(
                    id);

            var count =
                ReadCount(
                    ref reader,
                    reader.Context.MaxCollectionLength);

            var state =
                entry.Deserialize(
                    ref reader,
                    count,
                    reader.Context);

            components.Add(
                state);
        }

        return new EcsWorldState(
            entities,
            components);
    }

    private static void WriteEntityState(
        ref SerializationWriter writer,
        EntityStoreState state)
    {
        writer.WriteUInt32(
            state.NextIndex);

        WriteUInt32Array(
            ref writer,
            state.Generations);

        WriteUInt32Array(
            ref writer,
            state.ActiveIndices);

        WriteUInt32Array(
            ref writer,
            state.FreeIndices);
    }

    private static EntityStoreState ReadEntityState(
        ref SerializationReader reader)
    {
        var nextIndex =
            reader.ReadUInt32();

        var generations =
            ReadUInt32Array(
                ref reader);

        var activeIndices =
            ReadUInt32Array(
                ref reader);

        var freeIndices =
            ReadUInt32Array(
                ref reader);

        return new EntityStoreState(
            nextIndex,
            generations,
            activeIndices,
            freeIndices);
    }

    private static void WriteUInt32Array(
        ref SerializationWriter writer,
        IReadOnlyList<uint> values)
    {
        if (values.Count >
            writer.Context.MaxCollectionLength)
        {
            throw new InvalidDataException(
                "Entity state collection exceeds the maximum allowed collection length.");
        }

        writer.WriteInt32(
            values.Count);

        foreach (var value in values)
        {
            writer.WriteUInt32(
                value);
        }
    }

    private static uint[] ReadUInt32Array(
        ref SerializationReader reader)
    {
        var count =
            ReadCount(
                ref reader,
                reader.Context.MaxCollectionLength);

        var values =
            new uint[count];

        for (var i = 0;
             i < count;
             i++)
        {
            values[i] =
                reader.ReadUInt32();
        }

        return values;
    }

    private static int ReadCount(
        ref SerializationReader reader,
        int maximum)
    {
        var count =
            reader.ReadInt32();

        if (count < 0)
        {
            throw new InvalidDataException(
                $"Serialized collection length '{count}' is invalid.");
        }

        if (count >
            maximum)
        {
            throw new InvalidDataException(
                $"Serialized collection length '{count}' exceeds the maximum allowed length '{maximum}'.");
        }

        return count;
    }
}