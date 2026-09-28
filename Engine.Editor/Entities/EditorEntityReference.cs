using Engine.ECS.Entities;

namespace Engine.Editor.Entities;

public sealed class EditorEntityReference
{
    internal EditorEntityReference(
        EntityId entity)
    {
        SetEntity(entity);
    }

    public EntityId Entity { get; private set; }

    public bool IsAlive =>
        Entity.IsValid;

    internal void SetEntity(
        EntityId entity)
    {
        Entity = entity;
    }
}