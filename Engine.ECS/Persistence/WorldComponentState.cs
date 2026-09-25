using Engine.ECS.Entities;

namespace Engine.ECS.Persistence;

public abstract class WorldComponentState
{
    public abstract Type ComponentType { get; }

    public abstract int Count { get; }

    public abstract EntityId GetEntity(
        int index);

    public abstract object GetComponent(
        int index);

    internal abstract void Restore(
        World world);
}

public sealed class WorldComponentState<T> :
    WorldComponentState
    where T : struct
{
    public WorldComponentState(
        EntityId[] entities,
        T[] components)
    {
        ArgumentNullException.ThrowIfNull(
            entities);

        ArgumentNullException.ThrowIfNull(
            components);

        if (entities.Length !=
            components.Length)
        {
            throw new ArgumentException(
                "Entity and component counts must match.");
        }

        Entities =
            entities.ToArray();

        Components =
            components.ToArray();
    }

    public EntityId[] Entities { get; }

    public T[] Components { get; }

    public override Type ComponentType =>
        typeof(T);

    public override int Count =>
        Entities.Length;

    public override EntityId GetEntity(
        int index)
    {
        return Entities[index];
    }

    public override object GetComponent(
        int index)
    {
        return Components[index];
    }

    internal override void Restore(
        World world)
    {
        world.RestoreComponentState(
            this);
    }
}