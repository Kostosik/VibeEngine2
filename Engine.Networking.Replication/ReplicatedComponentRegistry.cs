using Engine.Serialization.Binary;

namespace Engine.Networking.Replication;

public sealed class ReplicatedComponentRegistry
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

    public IReadOnlyCollection<string> Ids =>
        _byId.Keys;

    public void Register<T>(
        string id,
        IBinarySerializer<T> serializer)
        where T : struct
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            id);

        ArgumentNullException.ThrowIfNull(
            serializer);

        if (_byId.ContainsKey(
                id))
        {
            throw new InvalidOperationException(
                $"Replicated component ID '{id}' is already registered.");
        }

        var type =
            typeof(T);

        if (_byType.ContainsKey(
                type))
        {
            throw new InvalidOperationException(
                $"Replicated component type '{type.FullName}' is already registered.");
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

    public Entry GetById(
        string id)
    {
        if (!_byId.TryGetValue(
                id,
                out var entry))
        {
            throw new InvalidDataException(
                $"No replicated component is registered for ID '{id}'.");
        }

        return entry;
    }

    public Entry GetByType(
        Type type)
    {
        if (!_byType.TryGetValue(
                type,
                out var entry))
        {
            throw new InvalidOperationException(
                $"No replicated component is registered for type '{type.FullName}'.");
        }

        return entry;
    }

    public abstract class Entry
    {
        public abstract string Id { get; }

        public abstract Type ComponentType { get; }

        public abstract byte[] Capture(
            Engine.ECS.World world,
            Engine.ECS.Entities.EntityId entity,
            SerializationContext context);

        public abstract void Apply(
            Engine.ECS.World world,
            Engine.ECS.Entities.EntityId entity,
            ReadOnlySpan<byte> payload,
            SerializationContext context);

        public abstract void Remove(
    Engine.ECS.World world,
    Engine.ECS.Entities.EntityId entity);

        public abstract void Validate(
    ReadOnlySpan<byte> payload,
    SerializationContext context);
    }

    public sealed class Entry<T> :
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

        internal IBinarySerializer<T> Serializer =>
            _serializer;

        public override void Validate(
    ReadOnlySpan<byte> payload,
    SerializationContext context)
        {
            BinarySerializer.Deserialize(
                payload,
                _serializer,
                context);
        }

        public override byte[] Capture(
    Engine.ECS.World world,
    Engine.ECS.Entities.EntityId entity,
    SerializationContext context)
        {
            if (!world.Inspector.TryGetComponent(
                    entity,
                    typeof(T),
                    out var component))
            {
                throw new InvalidOperationException(
                    $"Entity '{entity}' does not contain replicated component '{typeof(T).FullName}'.");
            }

            if (component is not T value)
            {
                throw new InvalidOperationException(
                    $"Component '{typeof(T).FullName}' was returned with an unexpected runtime type.");
            }

            return BinarySerializer.Serialize(
                value,
                _serializer,
                context);
        }

        public override void Remove(
    Engine.ECS.World world,
    Engine.ECS.Entities.EntityId entity)
        {
            if (world.Has<T>(entity))
            {
                world.Remove<T>(entity);
            }
        }

        public override void Apply(
            Engine.ECS.World world,
            Engine.ECS.Entities.EntityId entity,
            ReadOnlySpan<byte> payload,
            SerializationContext context)
        {
            var value =
                BinarySerializer.Deserialize(
                    payload,
                    _serializer,
                    context);

            if (world.Has<T>(
                    entity))
            {
                world.Remove<T>(
                    entity);
            }

            world.Add(
                entity,
                value);
        }
    }

    internal bool TryGetById(
    string id,
    out Entry entry) =>
    _byId.TryGetValue(id, out entry!);

    internal bool TryGetByType(
        Type componentType,
        out Entry entry) =>
        _byType.TryGetValue(componentType, out entry!);
}