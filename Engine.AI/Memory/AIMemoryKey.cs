
namespace Engine.AI.Memory;

/// <summary>
/// Strongly typed identity key for an agent memory entry.
/// Keys use reference identity, so keep them in static readonly fields.
/// </summary>
public sealed class AIMemoryKey<T>
    where T : notnull
{
    public string Name { get; }

    public AIMemoryKey(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
}
