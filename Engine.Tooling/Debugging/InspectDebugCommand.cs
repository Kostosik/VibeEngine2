using Engine.ECS.Entities;
using Engine.Tooling.Inspection;

namespace Engine.Tooling.Debugging;

public sealed class InspectDebugCommand :
    IDebugCommand
{
    private readonly WorldInspectionService _inspection;

    public InspectDebugCommand(
        WorldInspectionService inspection)
    {
        ArgumentNullException.ThrowIfNull(
            inspection);

        _inspection = inspection;
    }

    public string Name =>
        "inspect";

    public string Description =>
        "Inspects an entity. Usage: inspect <index>";

    public DebugCommandResult Execute(
        IReadOnlyList<string> arguments)
    {
        if (arguments.Count != 1)
        {
            return DebugCommandResult.Fail(
                "Usage: inspect <entityIndex>");
        }

        if (!uint.TryParse(
                arguments[0],
                out var index))
        {
            return DebugCommandResult.Fail(
                $"Invalid entity index '{arguments[0]}'.");
        }

        var entity =
            FindEntity(index);

        if (!entity.IsValid)
        {
            return DebugCommandResult.Fail(
                $"Entity '{index}' was not found.");
        }

        var inspection =
            _inspection.InspectEntity(
                entity);

        var lines =
            new List<string>
            {
            $"Entity {entity.Index}:{entity.Generation}"
            };

        foreach (var component
                 in inspection.Components)
        {
            lines.Add(
                $"  {component.Type.Name}: {component.Value}");
        }

        return DebugCommandResult.Ok(
            string.Join(
                Environment.NewLine,
                lines));
    }

    private EntityId FindEntity(
        uint index)
    {
        foreach (var entity
                 in _inspection.GetEntities())
        {
            if (entity.Index == index)
            {
                return entity;
            }
        }

        return EntityId.Invalid;
    }
}