using Engine.Core.Time;
using Engine.ECS;

namespace Engine.Simulations.Rollback;

public sealed class RollbackBuffer
{
    private readonly Engine.ECS.World _world;
    private readonly int _capacity;

    private readonly List<Entry> _entries = new();

    public RollbackBuffer(
        Engine.ECS.World world,
        int capacity)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                "Rollback buffer capacity must be greater than zero.");
        }

        _world = world;
        _capacity = capacity;
    }

    public int Count =>
        _entries.Count;

    public void Capture(
        Tick tick)
    {
        var snapshot =
            _world.CreateSnapshot();

        for (var i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Tick != tick)
            {
                continue;
            }

            _entries[i] =
                new Entry(
                    tick,
                    snapshot);

            return;
        }

        _entries.Add(
            new Entry(
                tick,
                snapshot));

        if (_entries.Count > _capacity)
        {
            _entries.RemoveAt(0);
        }
    }

    public bool Restore(
        Tick tick)
    {
        for (var i = 0; i < _entries.Count; i++)
        {
            var entry =
                _entries[i];

            if (entry.Tick != tick)
            {
                continue;
            }

            _world.RestoreSnapshot(
                entry.Snapshot);

            return true;
        }

        return false;
    }

    public void Clear()
    {
        _entries.Clear();
    }

    private readonly record struct Entry(
        Tick Tick,
        WorldSnapshot Snapshot);
}