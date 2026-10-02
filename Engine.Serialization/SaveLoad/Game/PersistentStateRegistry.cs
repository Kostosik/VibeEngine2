using Engine.Serialization.Binary;

namespace Engine.Serialization.SaveLoad.Game;

public sealed class PersistentStateRegistry
{
    private readonly Dictionary<
        string,
        Entry> _entries =
        new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> Ids =>
        _entries.Keys;

    public void Register<T>(
        string id,
        Func<T> capture,
        Action<T> restore,
        IBinarySerializer<T> serializer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            id);

        ArgumentNullException.ThrowIfNull(
            capture);

        ArgumentNullException.ThrowIfNull(
            restore);

        ArgumentNullException.ThrowIfNull(
            serializer);

        if (_entries.ContainsKey(
                id))
        {
            throw new InvalidOperationException(
                $"Persistent state ID '{id}' is already registered.");
        }

        _entries.Add(
            id,
            new Entry<T>(
                id,
                capture,
                restore,
                serializer));
    }

    internal IReadOnlyList<PersistentStateValue> Capture()
    {
        return _entries
            .OrderBy(
                pair => pair.Key,
                StringComparer.Ordinal)
            .Select(
                pair =>
                    new PersistentStateValue(
                        pair.Key,
                        pair.Value.Capture()))
            .ToArray();
    }

    internal void Validate(
        IReadOnlyList<PersistentStateValue> states)
    {
        ArgumentNullException.ThrowIfNull(
            states);

        var ids =
            new HashSet<string>(
                StringComparer.Ordinal);

        foreach (var state in states)
        {
            ArgumentNullException.ThrowIfNull(
                state);

            if (!ids.Add(
                    state.Id))
            {
                throw new InvalidDataException(
                    $"Persistent state ID '{state.Id}' appears more than once.");
            }

            if (!_entries.ContainsKey(
                    state.Id))
            {
                throw new InvalidDataException(
                    $"No persistent state handler is registered for ID '{state.Id}'.");
            }
        }
    }

    internal void Restore(
        IReadOnlyList<PersistentStateValue> states)
    {
        Validate(
            states);

        foreach (var state in states)
        {
            _entries[state.Id].Restore(
                state.Value);
        }
    }

    internal Entry GetEntry(
        string id)
    {
        if (!_entries.TryGetValue(
                id,
                out var entry))
        {
            throw new InvalidDataException(
                $"No persistent state handler is registered for ID '{id}'.");
        }

        return entry;
    }

    internal abstract class Entry
    {
        public abstract string Id { get; }

        public abstract object? Capture();

        public abstract void Restore(
            object? value);

        public abstract void Serialize(
            ref SerializationWriter writer,
            object? value);

        public abstract object? Deserialize(
            ref SerializationReader reader);
    }

    private sealed class Entry<T> :
        Entry
    {
        private readonly Func<T> _capture;
        private readonly Action<T> _restore;
        private readonly IBinarySerializer<T> _serializer;

        public Entry(
            string id,
            Func<T> capture,
            Action<T> restore,
            IBinarySerializer<T> serializer)
        {
            Id =
                id;

            _capture =
                capture;

            _restore =
                restore;

            _serializer =
                serializer;
        }

        public override string Id { get; }

        public override object? Capture()
        {
            return _capture();
        }

        public override void Restore(
            object? value)
        {
            if (value is not T typedValue)
            {
                throw new InvalidDataException(
                    $"Persistent state value for ID '{Id}' " +
                    $"is not of type '{typeof(T).FullName}'.");
            }

            _restore(
                typedValue);
        }

        public override void Serialize(
            ref SerializationWriter writer,
            object? value)
        {
            if (value is not T typedValue)
            {
                throw new InvalidDataException(
                    $"Persistent state value for ID '{Id}' " +
                    $"is not of type '{typeof(T).FullName}'.");
            }

            _serializer.Serialize(
                ref writer,
                typedValue);
        }

        public override object? Deserialize(
            ref SerializationReader reader)
        {
            return _serializer.Deserialize(
                ref reader);
        }
    }
}

public sealed class PersistentStateValue
{
    public PersistentStateValue(
        string id,
        object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            id);

        Id =
            id;

        Value =
            value;
    }

    public string Id { get; }

    public object? Value { get; }
}