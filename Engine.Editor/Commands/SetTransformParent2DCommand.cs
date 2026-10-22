using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.ECS.Systems;
using Engine.Editor.Entities;
using Engine.Transform;
using Engine.Worlds;

namespace Engine.Editor.Commands;

public sealed class SetTransformParent2DCommand : IEditorCommand
{
    private readonly World _world;
    private readonly EditorEntityReference _childReference;
    private readonly EntityId? _newParent;

    private bool _captured;
    private bool _hadOldParent;
    private TransformParent2D _oldParent;
    private bool _hadOldLocalTransform;
    private LocalTransform2D _oldLocalTransform;
    private LocalTransform2D _newLocalTransform;

    public SetTransformParent2DCommand(
        World world,
        EditorEntityReference childReference,
        EntityId? newParent)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(childReference);

        childReference.EnsureWorld(world);

        if (!childReference.IsAlive)
        {
            throw new ArgumentException(
                "The child entity is no longer alive.",
                nameof(childReference));
        }

        _world = world;
        _childReference = childReference;
        _newParent = newParent;
    }

    public void Execute()
    {
        EnsureChildExists();

        var child = _childReference.Entity;
        var ecs = _world.EcsWorld;

        if (!ecs.Has<WorldTransform2D>(child))
        {
            throw new InvalidOperationException(
                "Child entity requires WorldTransform2D.");
        }

        if (_newParent.HasValue)
        {
            ValidateParentChain(child, _newParent.Value);
        }
        else if (!ecs.Has<TransformParent2D>(child))
        {
            throw new InvalidOperationException(
                "The child entity does not have a parent.");
        }

        // Обновляем мировые трансформы до вычисления
        // нового локального трансформа.
        new TransformHierarchySystem(ecs).FixedUpdate(default);

        if (!_captured)
        {
            _hadOldParent = ecs.Has<TransformParent2D>(child);

            if (_hadOldParent)
            {
                _oldParent = ecs.Get<TransformParent2D>(child);
            }

            _hadOldLocalTransform = ecs.Has<LocalTransform2D>(child);

            if (_hadOldLocalTransform)
            {
                _oldLocalTransform = ecs.Get<LocalTransform2D>(child);
            }

            var childWorld = ReadWorldTransform(child);

            if (_newParent is EntityId parent)
            {
                var parentWorld = ReadWorldTransform(parent);

                _newLocalTransform = new LocalTransform2D(
                    ToLocalTransform(childWorld, parentWorld));
            }
            else
            {
                // После отвязки локальный трансформ становится
                // корневым и совпадает с прежним мировым.
                _newLocalTransform = new LocalTransform2D(childWorld);
            }

            _captured = true;
        }

        ApplyNewState();
    }

    public void Undo()
    {
        EnsureChildExists();

        if (!_captured)
        {
            throw new InvalidOperationException(
                "The command has not been executed.");
        }

        var ecs = _world.EcsWorld;
        var child = _childReference.Entity;

        if (_hadOldLocalTransform)
        {
            SetLocalTransform(child, _oldLocalTransform);
        }
        else if (ecs.Has<LocalTransform2D>(child))
        {
            ecs.Remove<LocalTransform2D>(child);
        }

        if (_hadOldParent)
        {
            SetParent(child, _oldParent.Parent);
        }
        else if (ecs.Has<TransformParent2D>(child))
        {
            ecs.Remove<TransformParent2D>(child);
        }

        new TransformHierarchySystem(ecs).FixedUpdate(default);
    }

    private void ApplyNewState()
    {
        var ecs = _world.EcsWorld;
        var child = _childReference.Entity;

        if (_newParent is EntityId parent)
        {
            ValidateParentChain(child, parent);

            SetLocalTransform(child, _newLocalTransform);
            SetParent(child, parent);
        }
        else
        {
            SetLocalTransform(child, _newLocalTransform);

            if (ecs.Has<TransformParent2D>(child))
            {
                ecs.Remove<TransformParent2D>(child);
            }
        }

        new TransformHierarchySystem(ecs).FixedUpdate(default);
    }

    private void ValidateParentChain(
        EntityId child,
        EntityId parent)
    {
        var ecs = _world.EcsWorld;
        var visited = new HashSet<EntityId>();
        var current = parent;

        while (true)
        {
            if (current == child)
            {
                throw new InvalidOperationException(
                    "The parent assignment would create a transform hierarchy cycle.");
            }

            if (!visited.Add(current))
            {
                throw new InvalidOperationException(
                    "The target parent already belongs to a cyclic hierarchy.");
            }

            if (!ecs.Exists(current))
            {
                throw new InvalidOperationException(
                    $"Parent entity '{current.Index}' does not exist.");
            }

            if (!ecs.Has<WorldTransform2D>(current))
            {
                throw new InvalidOperationException(
                    $"Parent entity '{current.Index}' requires WorldTransform2D.");
            }

            if (!ecs.Has<TransformParent2D>(current))
            {
                break;
            }

            if (!ecs.Has<LocalTransform2D>(current))
            {
                throw new InvalidOperationException(
                    $"Parent entity '{current.Index}' has TransformParent2D " +
                    "but no LocalTransform2D.");
            }

            current = ecs.Get<TransformParent2D>(current).Parent;
        }
    }

    private FixedTransform2D ReadWorldTransform(EntityId entity)
    {
        var transform = _world.EcsWorld.Get<WorldTransform2D>(entity);

        return new FixedTransform2D(transform.Position)
        {
            Rotation = transform.Rotation,
            Scale = transform.Scale
        };
    }

    private static FixedTransform2D ToLocalTransform(
        FixedTransform2D world,
        FixedTransform2D parent)
    {
        if (parent.Scale.X == Fixed32.Zero ||
            parent.Scale.Y == Fixed32.Zero)
        {
            throw new InvalidOperationException(
                "Cannot assign a parent with a zero scale.");
        }

        var offset = world.Position - parent.Position;

        var cosine = Fixed32.Cos(parent.Rotation);
        var sine = Fixed32.Sin(parent.Rotation);

        // Обратный поворот на угол родителя.
        var unrotated = new FixedVector2(
            offset.X * cosine + offset.Y * sine,
            -offset.X * sine + offset.Y * cosine);

        return new FixedTransform2D(
            new FixedVector2(
                unrotated.X / parent.Scale.X,
                unrotated.Y / parent.Scale.Y))
        {
            Rotation = world.Rotation - parent.Rotation,
            Scale = new FixedVector2(
                world.Scale.X / parent.Scale.X,
                world.Scale.Y / parent.Scale.Y)
        };
    }

    private void SetLocalTransform(
        EntityId child,
        LocalTransform2D value)
    {
        var ecs = _world.EcsWorld;

        if (ecs.Has<LocalTransform2D>(child))
        {
            ecs.Get<LocalTransform2D>(child) = value;
        }
        else
        {
            ecs.Add(child, value);
        }
    }

    private void SetParent(
        EntityId child,
        EntityId parent)
    {
        var ecs = _world.EcsWorld;

        if (ecs.Has<TransformParent2D>(child))
        {
            var component = ecs.Get<TransformParent2D>(child);
            component.Parent = parent;
            ecs.Get<TransformParent2D>(child) = component;
        }
        else
        {
            ecs.Add(child, new TransformParent2D(parent));
        }
    }

    private void EnsureChildExists()
    {
        if (!_childReference.IsAlive)
        {
            throw new InvalidOperationException(
                "The child entity is no longer alive.");
        }
    }
}