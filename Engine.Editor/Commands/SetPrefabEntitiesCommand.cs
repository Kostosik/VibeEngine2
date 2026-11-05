using Engine.ECS.Prefabs;
using Engine.Editor.Documents;

namespace Engine.Editor.Commands;

internal sealed class SetPrefabEntitiesCommand : IEditorCommand
{
    private readonly PrefabDocument _document;
    private readonly IReadOnlyList<PrefabEntity> _before;
    private readonly IReadOnlyList<PrefabEntity> _after;

    public SetPrefabEntitiesCommand(
        PrefabDocument document,
        IReadOnlyList<PrefabEntity> before,
        IReadOnlyList<PrefabEntity> after)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        _document = document;
        _before = Array.AsReadOnly(before.ToArray());
        _after = Array.AsReadOnly(after.ToArray());
    }

    public void Execute()
    {
        _document.ReplaceEntities(_after);
    }

    public void Undo()
    {
        _document.ReplaceEntities(_before);
    }
}