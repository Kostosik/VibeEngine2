namespace Engine.ECS.Entities;

public readonly record struct EntityId(
    uint Index,
    uint Generation)
{
    public bool IsValid =>
        Index != 0;

    public static EntityId Invalid =>
        default;
}