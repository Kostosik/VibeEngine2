using Engine.ECS.Prefabs;
using Engine.Serialization.Binary;
using Engine.Serialization.SaveLoad.Ecs;

namespace Engine.Serialization.SaveLoad.Prefabs;

public sealed class PrefabAssetSerializer :
    IBinarySerializer<PrefabAsset>
{
    private const int FormatVersion = 1;

    private readonly EcsComponentSerializerRegistry _registry;

    public PrefabAssetSerializer(
        EcsComponentSerializerRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        _registry = registry;
    }

    public void Serialize(
        ref SerializationWriter writer,
        PrefabAsset value)
    {
        ArgumentNullException.ThrowIfNull(value);

        writer.WriteInt32(FormatVersion);

        writer.WriteString(value.Name);

        writer.WriteUInt32(
            value.RootEntity.Value);

        EnsureCollectionLength(
            value.Entities.Count,
            writer.Context.MaxCollectionLength,
            "Prefab entity count");

        writer.WriteInt32(
            value.Entities.Count);

        foreach (var entity in value.Entities)
        {
            writer.WriteUInt32(
                entity.Id.Value);

            var components = entity.Components
                .OrderBy(
                    component => _registry
                        .GetByType(component.ComponentType)
                        .Id,
                    StringComparer.Ordinal)
                .ToArray();

            EnsureCollectionLength(
                components.Length,
                writer.Context.MaxCollectionLength,
                "Prefab component count");

            writer.WriteInt32(
                components.Length);

            foreach (var component in components)
            {
                var entry = _registry.GetByType(
                    component.ComponentType);

                writer.WriteString(
                    entry.Id);

                entry.SerializeValue(
                    ref writer,
                    component.BoxedValue);
            }
        }
    }

    public PrefabAsset Deserialize(
        ref SerializationReader reader)
    {
        var version = reader.ReadInt32();

        if (version < 1 || version > FormatVersion)
        {
            throw new InvalidDataException(
                $"Prefab format version '{version}' is not supported.");
        }

        var name = reader.ReadString()
            ?? throw new InvalidDataException(
                "Prefab name cannot be null.");

        var rootEntity = new PrefabEntityId(
            reader.ReadUInt32());

        if (!rootEntity.IsValid)
        {
            throw new InvalidDataException(
                "Prefab root entity ID is invalid.");
        }

        var entityCount = ReadCount(
            ref reader,
            minimum: 1,
            description: "Prefab entity count");

        var entities = new List<PrefabEntity>(
            entityCount);

        for (var i = 0; i < entityCount; i++)
        {
            var entityId = new PrefabEntityId(
                reader.ReadUInt32());

            if (!entityId.IsValid)
            {
                throw new InvalidDataException(
                    "Prefab entity ID is invalid.");
            }

            var componentCount = ReadCount(
                ref reader,
                minimum: 0,
                description: "Prefab component count");

            var components = new List<PrefabComponent>(
                componentCount);

            for (var componentIndex = 0;
                 componentIndex < componentCount;
                 componentIndex++)
            {
                var serializerId = reader.ReadString()
                    ?? throw new InvalidDataException(
                        "Prefab component serializer ID cannot be null.");

                var entry = _registry.GetById(
                    serializerId);

                var componentValue =
                    entry.DeserializeValue(
                        ref reader);

                components.Add(
                    PrefabComponent.Create(
                        entry.ComponentType,
                        componentValue));
            }

            entities.Add(
                new PrefabEntity(
                    entityId,
                    components));
        }

        // PrefabAsset performs final validation:
        // duplicate IDs, root existence and local entity references.
        return new PrefabAsset(
            name,
            rootEntity,
            entities);
    }

    private static int ReadCount(
        ref SerializationReader reader,
        int minimum,
        string description)
    {
        var count = reader.ReadInt32();

        if (count < minimum ||
            count > reader.Context.MaxCollectionLength)
        {
            throw new InvalidDataException(
                $"{description} '{count}' is invalid.");
        }

        return count;
    }

    private static void EnsureCollectionLength(
        int count,
        int maximum,
        string description)
    {
        if (count < 0 || count > maximum)
        {
            throw new InvalidDataException(
                $"{description} '{count}' exceeds the allowed limit.");
        }
    }
}