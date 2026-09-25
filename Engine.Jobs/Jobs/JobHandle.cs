using System.Runtime.CompilerServices;
using Engine.Jobs.Scheduling;

namespace Engine.Jobs.Jobs;

public readonly struct JobHandle :
    IEquatable<JobHandle>
{
    private readonly JobNode? _node;

    internal JobHandle(
        JobNode node)
    {
        ArgumentNullException.ThrowIfNull(
            node);

        _node =
            node;
    }

    internal JobNode Node =>
        _node ??
        throw new InvalidOperationException(
            "Job handle is invalid.");

    public bool IsValid =>
        _node is not null;

    public bool Equals(
        JobHandle other)
    {
        return ReferenceEquals(
            _node,
            other._node);
    }

    public override bool Equals(
        object? obj)
    {
        return obj is JobHandle other &&
               Equals(other);
    }

    public override int GetHashCode()
    {
        return _node is null
            ? 0
            : RuntimeHelpers.GetHashCode(
                _node);
    }

    public static bool operator ==(
        JobHandle left,
        JobHandle right)
    {
        return left.Equals(
            right);
    }

    public static bool operator !=(
        JobHandle left,
        JobHandle right)
    {
        return !left.Equals(
            right);
    }

    public override string ToString()
    {
        return IsValid
            ? $"JobHandle({_node!.Id})"
            : "JobHandle(Invalid)";
    }
}