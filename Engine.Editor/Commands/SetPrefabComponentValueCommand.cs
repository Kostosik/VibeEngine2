using Engine.ECS.Prefabs;
using Engine.Editor.Documents;

namespace Engine.Editor.Commands;

internal sealed class SetPrefabComponentValueCommand : IEditorCommand
{
    private readonly PrefabDocument _document;
    private readonly PrefabEntityId _entityId;
    private readonly Type _componentType;
    private readonly object _oldValue;
    private readonly object _newValue;

    public SetPrefabComponentValueCommand(
        PrefabDocument document,
        PrefabEntityId entityId,
        Type componentType,
        object oldValue,
        object newValue)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(componentType);
        ArgumentNullException.ThrowIfNull(oldValue);
        ArgumentNullException.ThrowIfNull(newValue);

        _document = document;
        _entityId = entityId;
        _componentType = componentType;
        _oldValue = oldValue;
        _newValue = newValue;
    }

    public void Execute()
    {
        _document.ReplaceComponentValue(
            _entityId,
            _componentType,
            _newValue);
    }

    public void Undo()
    {
        _document.ReplaceComponentValue(
            _entityId,
            _componentType,
            _oldValue);
    }
}