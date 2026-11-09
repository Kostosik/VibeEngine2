using Engine.AI.Scheduling;
using Engine.Core.Time;

namespace Engine.AI.Components;

public struct AIUpdateSettings :
    IEquatable<AIUpdateSettings>
{
    public AIUpdateGroup Group { get; set; }

    public int Priority { get; set; }

    /// <summary>
    /// Custom interval. Zero means use the group's interval.
    /// </summary>
    public Duration UpdateInterval { get; set; }

    public AIUpdateSettings(
        AIUpdateGroup group = AIUpdateGroup.EveryUpdate,
        int priority = 0,
        Duration updateInterval = default)
    {
        Group = group;
        Priority = priority;
        UpdateInterval = updateInterval;
    }

    public bool Equals(AIUpdateSettings other)
    {
        return Group == other.Group &&
               Priority == other.Priority &&
               UpdateInterval == other.UpdateInterval;
    }

    public override bool Equals(object? obj)
    {
        return obj is AIUpdateSettings other &&
               Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Group,
            Priority,
            UpdateInterval);
    }

    public static bool operator ==(
        AIUpdateSettings left,
        AIUpdateSettings right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(
        AIUpdateSettings left,
        AIUpdateSettings right)
    {
        return !left.Equals(right);
    }
}