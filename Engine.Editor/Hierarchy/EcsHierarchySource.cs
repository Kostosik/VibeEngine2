using Engine.ECS.Components;
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
                    GetParentId(entity)));
        }

        return nodes;
    }

    private object? GetParentId(
        EntityId entity)
    {
        if (!_world.EcsWorld.Has<TransformParent2D>(
                entity))
        {
            return null;
        }

        return _world
            .EcsWorld
            .Get<TransformParent2D>(
                entity)
            .Parent;
    }
}