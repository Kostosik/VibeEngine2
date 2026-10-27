using Engine.Editor;
using Engine.Editor.UI.Workspace;

namespace Engine.Tests.Editor;

public sealed class HierarchyActionRegistryTests
{
    [Fact]
    public void Register_RejectsDuplicateIdsIgnoringCase()
    {
        var registry = new EditorHierarchyActionRegistry();

        registry.Register(CreateAction("entity.delete"));

        Assert.Throws<InvalidOperationException>(
            () => registry.Register(CreateAction("ENTITY.DELETE")));

        Assert.Single(registry.Actions);
    }

    [Fact]
    public void GetForSurface_FiltersAndOrdersActions()
    {
        var registry = new EditorHierarchyActionRegistry();

        var second = CreateAction(
            "entity.rename",
            EditorHierarchyActionSurface.EntityContextMenu,
            order: 20);

        var first = CreateAction(
            "entity.delete",
            EditorHierarchyActionSurface.EntityContextMenu,
            order: 10);

        registry.Register(second);
        registry.Register(
            CreateAction(
                "entity.create",
                EditorHierarchyActionSurface.CreateMenu));

        registry.Register(first);

        var result = registry.GetForSurface(
            EditorHierarchyActionSurface.EntityContextMenu);

        Assert.Equal(new[] { first, second }, result);
    }

    [Fact]
    public void Execute_DoesNotRunUnavailableAction()
    {
        var registry = new EditorHierarchyActionRegistry();
        var executions = 0;

        registry.Register(
            new EditorHierarchyActionDefinition(
                id: "entity.delete",
                label: "Delete",
                surfaces:
                    EditorHierarchyActionSurface.EntityContextMenu,
                canExecute: static _ => false,
                execute: _ => executions++));

        var context = new EditorHierarchyActionContext(
            new EditorContext(),
            document: null,
            targetEntity: null,
            EditorHierarchyActionInvocation.Programmatic);

        var result = registry.Execute("entity.delete", context);

        Assert.False(result);
        Assert.Equal(0, executions);
    }

    [Fact]
    public void Execute_RunsAvailableAction()
    {
        var registry = new EditorHierarchyActionRegistry();
        var executions = 0;

        registry.Register(
            new EditorHierarchyActionDefinition(
                id: "entity.create",
                label: "Create Entity",
                surfaces:
                    EditorHierarchyActionSurface.CreateMenu,
                canExecute: static _ => true,
                execute: _ => executions++));

        var context = new EditorHierarchyActionContext(
            new EditorContext(),
            document: null,
            targetEntity: null,
            EditorHierarchyActionInvocation.Programmatic);

        var result = registry.Execute("entity.create", context);

        Assert.True(result);
        Assert.Equal(1, executions);
    }

    [Fact]
    public void Unregister_RemovesAction()
    {
        var registry = new EditorHierarchyActionRegistry();

        registry.Register(CreateAction("entity.delete"));

        Assert.True(registry.Unregister("entity.delete"));
        Assert.Empty(registry.Actions);
        Assert.False(registry.Unregister("entity.delete"));
    }

    private static EditorHierarchyActionDefinition CreateAction(
        string id,
        EditorHierarchyActionSurface surfaces =
            EditorHierarchyActionSurface.EntityContextMenu,
        int order = 0)
    {
        return new EditorHierarchyActionDefinition(
            id,
            id,
            surfaces,
            static _ => true,
            static _ => { },
            order: order);
    }
}