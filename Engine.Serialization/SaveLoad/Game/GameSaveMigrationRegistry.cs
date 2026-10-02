namespace Engine.Serialization.SaveLoad.Game;

public interface IGameSaveMigration
{
    int FromVersion { get; }

    int ToVersion { get; }

    GameSaveState Migrate(
        GameSaveState state);
}

public sealed class GameSaveMigrationRegistry
{
    private readonly Dictionary<
        int,
        IGameSaveMigration> _migrations =
        new();

    public void Register(
        IGameSaveMigration migration)
    {
        ArgumentNullException.ThrowIfNull(
            migration);

        if (migration.FromVersion < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(migration),
                "Migration source version must be greater than or equal to one.");
        }

        if (migration.ToVersion <=
            migration.FromVersion)
        {
            throw new ArgumentException(
                "Migration target version must be greater than its source version.",
                nameof(migration));
        }

        if (_migrations.ContainsKey(
                migration.FromVersion))
        {
            throw new InvalidOperationException(
                $"A migration from version '{migration.FromVersion}' is already registered.");
        }

        _migrations.Add(
            migration.FromVersion,
            migration);
    }

    public GameSaveState MigrateTo(
        GameSaveState state,
        int targetVersion)
    {
        ArgumentNullException.ThrowIfNull(
            state);

        if (targetVersion < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetVersion));
        }

        var current =
            state;

        while (current.Version <
               targetVersion)
        {
            if (!_migrations.TryGetValue(
                    current.Version,
                    out var migration))
            {
                throw new InvalidDataException(
                    $"No game save migration is registered from version '{current.Version}'.");
            }

            if (migration.ToVersion >
                targetVersion)
            {
                throw new InvalidDataException(
                    $"Migration from version '{migration.FromVersion}' " +
                    $"jumps to '{migration.ToVersion}', beyond target version '{targetVersion}'.");
            }

            current =
                migration.Migrate(
                    current);

            if (current.Version !=
                migration.ToVersion)
            {
                throw new InvalidDataException(
                    $"Migration from version '{migration.FromVersion}' " +
                    $"did not produce expected version '{migration.ToVersion}'.");
            }
        }

        if (current.Version >
            targetVersion)
        {
            throw new InvalidDataException(
                $"Save state version '{current.Version}' is newer than " +
                $"the supported target version '{targetVersion}'.");
        }

        return current;
    }
}