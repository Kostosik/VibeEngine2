using Engine.AI.Components;
using Engine.AI.Systems;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.ECS;
using Engine.ECS.Entities;

namespace Engine.Tests.AI;

internal static class AITestHelpers
{
    public static EntityId CreateAgent(World world)
    {
        var entity = world.CreateEntity();

        world.Add(entity, new AIComponent());

        return entity;
    }

    public static void Update(
        AISystem system,
        double elapsedSeconds,
        int tick = 0)
    {
        system.Update(
            new SystemContext(
                new TimeSnapshot(
                    Duration.FromMilliseconds(16),
                    Duration.FromSeconds(elapsedSeconds),
                    new Tick((ulong)tick))));
    }
}