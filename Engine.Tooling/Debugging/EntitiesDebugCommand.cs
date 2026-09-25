using Engine.Tooling.Inspection;

namespace Engine.Tooling.Debugging;

public sealed class EntitiesDebugCommand :
    IDebugCommand
{
    private readonly WorldInspectionService _inspection;

    public EntitiesDebugCommand(
        WorldInspectionService inspection)
    {
        ArgumentNullException.ThrowIfNull(
            inspection);

        _inspection = inspection;
    }

    public string Name =>
        "entities";

    public string Description =>
        "Lists active entities.";

    public DebugCommandResult Execute(
        IReadOnlyList<string> arguments)
    {
        if (arguments.Count != 0)
        {
            return DebugCommandResult.Fail(
                "Usage: entities");
        }

        var entities =
            _inspection.GetEntities();

        if (entities.Count == 0)
        {
            return DebugCommandResult.Ok(
                "No active entities.");
        }

        var lines =
            new List<string>(
                entities.Count + 1)
            {
            $"Entities ({entities.Count}):"
            };

        foreach (var entity in entities)
        {
            lines.Add(
                $"  {entity.Index}:{entity.Generation}");
        }

        return DebugCommandResult.Ok(
            string.Join(
                Environment.NewLine,
                lines));
    }
}