using Engine.ECS.Entities;
using Engine.Worlds;

namespace Engine.Editor.Hierarchy;

public sealed class EcsHierarchySource :
    IEditorHierarchySource
{
    private readonly World _world;

    public EcsHierarchySource(
        World world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        _world = world;
    }

    public IReadOnlyList<EditorHierarchyNode> GetNodes()
    {
        var entities =
            _world
                .EcsWorld
                .Inspector
                .GetEntities();

        var nodes =
            new List<EditorHierarchyNode>(
                entities.Count);

        foreach (var entity in entities)
        {
            nodes.Add(
                new EditorHierarchyNode(
                    entity,
                    $"Entity {entity.Index}",
                    null));
        }

        return nodes;
    }
}