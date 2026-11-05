using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.ECS.Prefabs;
using Engine.Editor.Documents;
using Engine.Transform;

namespace Engine.Editor.Commands;

/// <summary>
/// Changes a prefab entity's parent while preserving its world transform.
/// </summary>
public sealed class ReparentPrefabEntityCommand : IEditorCommand
{
    private readonly PrefabDocument _document;
    private readonly IReadOnlyList<PrefabEntity> _before;
    private readonly IReadOnlyList<PrefabEntity> _after;

    public ReparentPrefabEntityCommand(
        PrefabDocument document,
        PrefabEntityId entityId,
        PrefabEntityId newParentId)
    {
        ArgumentNullException.ThrowIfNull(document);

        _document = document;

        var asset = document.Asset;

        var entity = FindEntity(asset, entityId);
        var newParent = FindEntity(asset, newParentId);

        if (entityId == asset.RootEntity)
        {
            throw new ArgumentException(
                "The prefab root entity cannot be moved.",
                nameof(entityId));
        }

        if (entityId == newParentId)
        {
            throw new ArgumentException(
                "An entity cannot become its own parent.",
                nameof(newParentId));
        }

        EnsureNoCycle(asset, entity, newParent);

        if (TryGetParentId(entity, out var currentParentId) &&
            currentParentId == newParentId)
        {
            throw new InvalidOperationException(
                "The entity already belongs to this parent.");
        }

        // Calculate transforms from the hierarchy rather than relying
        // on potentially stale cached WorldTransform2D values.
        var entityWorld = CalculateWorldTransform(
            asset,
            entity,
            new HashSet<PrefabEntityId>());

        var parentWorld = CalculateWorldTransform(
            asset,
            newParent,
            new HashSet<PrefabEntityId>());

        var newLocal = ToLocalTransform(
            entityWorld,
            parentWorld);

        _before = Array.AsReadOnly(asset.Entities.ToArray());

        var updatedComponents = entity.Components.ToList();

        ReplaceComponent(
            updatedComponents,
            new TransformParent2D(
                new EntityId(newParentId.Value, 0)));

        ReplaceComponent(
            updatedComponents,
            new LocalTransform2D(newLocal));

        var after = asset.Entities.ToArray();

        var entityIndex = Array.FindIndex(
            after,
            item => item.Id == entityId);

        after[entityIndex] = new PrefabEntity(
            entityId,
            updatedComponents);

        // Constructing the updated asset validates all entity references
        // before the command is added to the history.
        _ = new PrefabAsset(
            asset.Name,
            asset.RootEntity,
            after);

        _after = Array.AsReadOnly(after);
    }

    public void Execute()
    {
        _document.ReplaceEntities(_after);
    }

    public void Undo()
    {
        _document.ReplaceEntities(_before);
    }

    private static PrefabEntity FindEntity(
        PrefabAsset asset,
        PrefabEntityId entityId)
    {
        return asset.Entities.FirstOrDefault(
            entity => entity.Id == entityId)
            ?? throw new ArgumentException(
                $"Prefab entity '{entityId.Value}' was not found.");
    }

    private static void EnsureNoCycle(
        PrefabAsset asset,
        PrefabEntity entity,
        PrefabEntity newParent)
    {
        var visited = new HashSet<PrefabEntityId>();
        var current = newParent;

        while (true)
        {
            if (current.Id == entity.Id)
            {
                throw new InvalidOperationException(
                    "An entity cannot become a child of its descendant.");
            }

            if (!visited.Add(current.Id))
            {
                throw new InvalidOperationException(
                    "The prefab hierarchy already contains a cycle.");
            }

            if (!TryGetParentId(current, out var parentId))
            {
                return;
            }

            current = FindEntity(asset, parentId);
        }
    }

    private static bool TryGetParentId(
        PrefabEntity entity,
        out PrefabEntityId parentId)
    {
        var component = entity.Components
            .OfType<PrefabComponent<TransformParent2D>>()
            .FirstOrDefault();

        if (component is null)
        {
            parentId = PrefabEntityId.Invalid;
            return false;
        }

        var reference = component.Component.Parent;

        if (!reference.IsValid || reference.Generation != 0)
        {
            throw new InvalidOperationException(
                $"Entity '{entity.Id.Value}' has an invalid prefab parent reference.");
        }

        parentId = new PrefabEntityId(reference.Index);
        return true;
    }

    private static FixedTransform2D CalculateWorldTransform(
        PrefabAsset asset,
        PrefabEntity entity,
        HashSet<PrefabEntityId> resolving)
    {
        if (!resolving.Add(entity.Id))
        {
            throw new InvalidOperationException(
                "The prefab hierarchy contains a transform cycle.");
        }

        if (!TryGetComponent<WorldTransform2D>(
                entity,
                out var worldTransform))
        {
            throw new InvalidOperationException(
                $"Entity '{entity.Id.Value}' requires WorldTransform2D.");
        }

        if (!TryGetComponent<LocalTransform2D>(
                entity,
                out var localTransform))
        {
            if (TryGetComponent<TransformParent2D>(
                    entity,
                    out _))
            {
                throw new InvalidOperationException(
                    $"Entity '{entity.Id.Value}' has TransformParent2D " +
                    "but no LocalTransform2D.");
            }

            return ToFixedTransform(worldTransform);
        }

        if (!TryGetComponent<TransformParent2D>(
                entity,
                out var parentComponent))
        {
            return localTransform.Value;
        }

        var parentReference = parentComponent.Parent;

        if (!parentReference.IsValid ||
            parentReference.Generation != 0)
        {
            throw new InvalidOperationException(
                $"Entity '{entity.Id.Value}' has an invalid prefab parent reference.");
        }

        var parentId = new PrefabEntityId(parentReference.Index);
        var parent = FindEntity(asset, parentId);

        var parentWorld = CalculateWorldTransform(
            asset,
            parent,
            resolving);

        return FixedTransformOperations2D.Combine(
            parentWorld,
            localTransform.Value);
    }

    private static FixedTransform2D ToFixedTransform(
        WorldTransform2D transform)
    {
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

    private static bool TryGetComponent<T>(
        PrefabEntity entity,
        out T value)
        where T : struct
    {
        var component = entity.Components
            .OfType<PrefabComponent<T>>()
            .FirstOrDefault();

        if (component is null)
        {
            value = default;
            return false;
        }

        value = component.Component;
        return true;
    }

    private static void ReplaceComponent<T>(
        List<PrefabComponent> components,
        T value)
        where T : struct
    {
        var replacement = PrefabComponent.Create(
            typeof(T),
            value);

        var index = components.FindIndex(
            component => component.ComponentType == typeof(T));

        if (index >= 0)
        {
            components[index] = replacement;
        }
        else
        {
            components.Add(replacement);
        }
    }
}