using Engine.ECS.Entities;
using Engine.ECS.Persistence;
using Engine.Serialization.Binary;

namespace Engine.Serialization.SaveLoad.Ecs;

public sealed class EcsComponentSerializerRegistry
{
    private readonly Dictionary<
        string,
        Entry> _byId =
        new(
            StringComparer.Ordinal);

    private readonly Dictionary<
        Type,
        Entry> _byType =
        new();

    public void Register<T>(
        string id,
        IBinarySerializer<T> serializer)
        where T : struct
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            id);

        ArgumentNullException.ThrowIfNull(
            serializer);

        if (_byId.ContainsKey(id))
        {
            throw new InvalidOperationException(
                $"Serializer ID '{id}' is already registered.");
        }

        var type =
            typeof(T);

        if (_byType.ContainsKey(type))
        {
            throw new InvalidOperationException(
                $"A serializer for component type '{type.FullName}' is already registered.");
        }

        var entry =
            new Entry<T>(
                id,
                serializer);

        _byId.Add(
            id,
            entry);

        _byType.Add(
            type,
            entry);
    }

    internal Entry GetById(
        string id)
    {
        if (!_byId.TryGetValue(
                id,
                out var entry))
        {
            throw new InvalidDataException(
                $"No ECS component serializer is registered for ID '{id}'.");
        }

        return entry;
    }

    internal Entry GetByType(
        Type type)
    {
        if (!_byType.TryGetValue(
                type,
                out var entry))
        {
            throw new InvalidOperationException(
                $"No ECS component serializer is registered for type '{type.FullName}'.");
        }

        return entry;
    }

    internal abstract class Entry
    {
        public abstract string Id { get; }

        public abstract Type ComponentType { get; }

        public abstract void Serialize(
            ref SerializationWriter writer,
            WorldComponentState state);

        public abstract WorldComponentState Deserialize(
            ref SerializationReader reader,
            int count,
            SerializationContext context);
    }

    private sealed class Entry<T> :
        Entry
        where T : struct
    {
        private readonly IBinarySerializer<T> _serializer;

        public Entry(
            string id,
            IBinarySerializer<T> serializer)
        {
            Id =
                id;

            _serializer =
                serializer;
        }

        public override string Id { get; }

        public override Type ComponentType =>
            typeof(T);

        public override void Serialize(
            ref SerializationWriter writer,
            WorldComponentState state)
        {
            if (state is not
                WorldComponentState<T> typedState)
            {
                throw new InvalidOperationException(
                    $"Component state type '{state.ComponentType.FullName}' " +
                    $"does not match registered type '{typeof(T).FullName}'.");
            }

            for (var i = 0;
                 i < typedState.Count;
                 i++)
            {
                var entity =
                    typedState.Entities[i];

                writer.WriteUInt32(
                    entity.Index);

                writer.WriteUInt32(
                    entity.Generation);

                _serializer.Serialize(
                    ref writer,
                    typedState.Components[i]);
            }
        }

        public override WorldComponentState Deserialize(
            ref SerializationReader reader,
            int count,
            SerializationContext context)
        {
            var entities =
                new EntityId[count];

            var components =
                new T[count];

            for (var i = 0;
                 i < count;
                 i++)
            {
                entities[i] =
                    new EntityId(
                        reader.ReadUInt32(),
                        reader.ReadUInt32());

                components[i] =
                    _serializer.Deserialize(
                        ref reader);
            }

            return new WorldComponentState<T>(
                entities,
                components);
        }
    }
}