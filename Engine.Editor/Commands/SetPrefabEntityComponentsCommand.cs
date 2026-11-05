using Engine.ECS.Prefabs;
using Engine.Editor.Documents;

namespace Engine.Editor.Commands;

internal sealed class SetPrefabEntityComponentsCommand : IEditorCommand
{
    private readonly PrefabDocument _document;
    private readonly PrefabEntityId _entityId;
    private readonly IReadOnlyList<PrefabComponent> _before;
    private readonly IReadOnlyList<PrefabComponent> _after;

    public SetPrefabEntityComponentsCommand(
        PrefabDocument document,
        PrefabEntityId entityId,
        IReadOnlyList<PrefabComponent> before,
        IReadOnlyList<PrefabComponent> after)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        _document = document;
        _entityId = entityId;
        _before = Array.AsReadOnly(before.ToArray());
        _after = Array.AsReadOnly(after.ToArray());
    }

    public void Execute()
    {
        _document.ReplaceEntityComponents(_entityId, _after);
    }

    public void Undo()
    {
        _document.ReplaceEntityComponents(_entityId, _before);
    }
}