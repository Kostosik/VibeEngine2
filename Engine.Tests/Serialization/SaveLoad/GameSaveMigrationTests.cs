using Engine.Serialization.SaveLoad.Game;
using Engine.Worlds;
using Engine.Worlds.Persistence;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Serialization.SaveLoad;

public sealed class GameSaveMigrationTests
{

    [Fact]
    public void MigrateToCurrentVersion_ThrowsWhenMigrationIsMissing()
    {
        var migrationRegistry =
            new GameSaveMigrationRegistry();

        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(4, 4),
                ecsWorld);

        var state =
            new GameSaveState(
                version: 1,
                world: WorldPersistence.Capture(world),
                persistentStates:
                [
                ]);

        Assert.Throws<InvalidDataException>(
            () =>
                migrationRegistry.MigrateTo(
                    state,
                    2));
    }

    private sealed class TestMigration :
        IGameSaveMigration
    {
        public int FromVersion =>
            1;

        public int ToVersion =>
            2;

        public GameSaveState Migrate(
            GameSaveState state)
        {
            return new GameSaveState(
                version: 2,
                world: state.World,
                persistentStates: state.PersistentStates);
        }
    }
}