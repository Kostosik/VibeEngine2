using Engine.ECS.Entities;
using Engine.Editor;
using Engine.Editor.Documents;

namespace Engine.Editor.UI.Workspace;

[Flags]
public enum EditorHierarchyActionSurface
{
    None = 0,
    CreateMenu = 1 << 0,
    EntityContextMenu = 1 << 1,
    EmptyAreaContextMenu = 1 << 2
}

public enum EditorHierarchyActionInvocation
{
    CreateMenu,
    EntityContextMenu,
    EmptyAreaContextMenu,
    KeyboardShortcut,
    Programmatic
}

public sealed class EditorHierarchyActionContext
{
    public EditorHierarchyActionContext(
        EditorContext editor,
        EditorDocument? document,
        EntityId? targetEntity,
        EditorHierarchyActionInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(editor);

        Editor = editor;
        Document = document;
        TargetEntity = targetEntity;
        Invocation = invocation;
    }

    public EditorContext Editor { get; }

    public EditorDocument? Document { get; }

    public EntityId? TargetEntity { get; }

    public EditorHierarchyActionInvocation Invocation { get; }
}