using Engine.ECS.Entities;
using Engine.ECS.Prefabs;

namespace Engine.ECS.Components;

public struct TransformParent3D :
    IPrefabEntityReferenceRemappable
{
    private EntityId _parent;

    public TransformParent3D(EntityId parent)
    {
        ValidateParent(parent);
        _parent = parent;
    }

    public EntityId Parent
    {
        readonly get => _parent;
        set
        {
            ValidateParent(value);
            _parent = value;
        }
    }

    public object RemapPrefabEntityReferences(
        Func<EntityId, EntityId> remap)
    {
        ArgumentNullException.ThrowIfNull(remap);

        return new TransformParent3D(
            remap(Parent));
    }

    private static void ValidateParent(EntityId parent)
    {
        if (!parent.IsValid)
        {
            throw new ArgumentException(
                "Parent entity must be valid.",
                nameof(parent));
        }
    }
}