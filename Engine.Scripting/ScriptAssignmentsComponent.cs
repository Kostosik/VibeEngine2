using Engine.Core.Determinism;

namespace Engine.Scripting;

public readonly struct ScriptAssignmentsComponent :
    IDeterministicState
{
    private readonly ScriptAssignment[]? _assignments;

    public ScriptAssignmentsComponent(
        IEnumerable<ScriptAssignment> assignments)
    {
        ArgumentNullException.ThrowIfNull(
            assignments);

        var values = assignments
            .Select(
                assignment =>
                    new ScriptAssignment(
                        assignment.ScriptId,
                        assignment.ConfigurationJson))
            .OrderBy(
                assignment => assignment.ScriptId,
                StringComparer.Ordinal)
            .ToArray();

        for (var i = 1; i < values.Length; i++)
        {
            if (string.Equals(
                    values[i - 1].ScriptId,
                    values[i].ScriptId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Script '{values[i].ScriptId}' " +
                    "is assigned more than once.");
            }
        }

        _assignments = values;
    }

    public int Count =>
        _assignments?.Length ?? 0;

    public ReadOnlySpan<ScriptAssignment> Assignments =>
        _assignments ?? Array.Empty<ScriptAssignment>();

    public bool Contains(
        string scriptId)
    {
        foreach (var assignment in Assignments)
        {
            if (string.Equals(
                    assignment.ScriptId,
                    scriptId,
                    StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public bool TryGet(
        string scriptId,
        out ScriptAssignment assignment)
    {
        foreach (var candidate in Assignments)
        {
            if (string.Equals(
                    candidate.ScriptId,
                    scriptId,
                    StringComparison.Ordinal))
            {
                assignment = candidate;
                return true;
            }
        }

        assignment = default;
        return false;
    }

    public ScriptAssignmentsComponent WithAdded(
        ScriptAssignment assignment)
    {
        if (Contains(assignment.ScriptId))
        {
            throw new InvalidOperationException(
                $"Script '{assignment.ScriptId}' " +
                "is already assigned.");
        }

        var values =
            new ScriptAssignment[Count + 1];

        Assignments.CopyTo(values);

        values[^1] =
            new ScriptAssignment(
                assignment.ScriptId,
                assignment.ConfigurationJson);

        return new ScriptAssignmentsComponent(values);
    }

    public ScriptAssignmentsComponent Without(
        string scriptId)
    {
        var values =
            new List<ScriptAssignment>(Count);

        var removed = false;

        foreach (var assignment in Assignments)
        {
            if (string.Equals(
                    assignment.ScriptId,
                    scriptId,
                    StringComparison.Ordinal))
            {
                removed = true;
                continue;
            }

            values.Add(assignment);
        }

        return removed
            ? new ScriptAssignmentsComponent(values)
            : this;
    }

    public void AddToHash(
        ref DeterministicStateHasher hasher)
    {
        hasher.AddInt32(Count);

        foreach (var assignment in Assignments)
        {
            hasher.AddString(
                assignment.ScriptId);

            hasher.AddString(
                assignment.ConfigurationJson);
        }
    }
}