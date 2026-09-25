namespace Engine.ECS.Persistence;

public sealed class EcsWorldState
{
    public EcsWorldState(
        EntityStoreState entities,
        IReadOnlyList<WorldComponentState> components)
    {
        ArgumentNullException.ThrowIfNull(
            entities);

        ArgumentNullException.ThrowIfNull(
            components);

        Entities =
            entities;

        Components =
            components.ToArray();
    }

    public EntityStoreState Entities { get; }

    public IReadOnlyList<WorldComponentState> Components { get; }
}