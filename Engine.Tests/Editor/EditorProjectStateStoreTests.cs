using System.Text.Json;
using Engine.Editor.Persistence;

namespace Engine.Tests.Editor;

public sealed class EditorProjectStateStoreTests
{
    [Fact]
    public void Save_WhenReplacingDirectory_CleansTemporaryFile()
    {
        var root = Directory.CreateTempSubdirectory();

        try
        {
            var statePath = Path.Combine(
                root.FullName,
                "editor-state.json");

            Directory.CreateDirectory(statePath);

            var store = new EditorProjectStateStore(statePath);

            var exception = Record.Exception(
                () => store.SaveLastProjectManifestPath(
                    Path.Combine(
                        root.FullName,
                        "Game",
                        "Game.vibeproject")));

            Assert.NotNull(exception);

            Assert.True(
                exception is IOException
                    or UnauthorizedAccessException,
                $"Unexpected exception type: {exception.GetType().FullName}");

            Assert.Empty(
                Directory.EnumerateFiles(root.FullName)
                    .Where(path =>
                        Path.GetExtension(path)
                            .Equals(
                                ".tmp",
                                StringComparison.OrdinalIgnoreCase)));
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public void Load_WhenStateFileDoesNotExist_ReturnsNull()
    {
        var root = Directory.CreateTempSubdirectory();

        try
        {
            var store = CreateStore(root);

            Assert.Null(store.LoadLastProjectManifestPath());
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public void SaveAndLoad_RoundTripsManifestPath()
    {
        var root = Directory.CreateTempSubdirectory();

        try
        {
            var store = CreateStore(root);

            var manifestPath = Path.Combine(
                root.FullName,
                "MyGame",
                "MyGame.vibeproject");

            store.SaveLastProjectManifestPath(manifestPath);

            Assert.Equal(
                Path.GetFullPath(manifestPath),
                store.LoadLastProjectManifestPath());
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public void SaveNull_ClearsLastProjectPath()
    {
        var root = Directory.CreateTempSubdirectory();

        try
        {
            var store = CreateStore(root);

            store.SaveLastProjectManifestPath(
                Path.Combine(root.FullName, "MyGame.vibeproject"));

            store.SaveLastProjectManifestPath(null);

            Assert.Null(store.LoadLastProjectManifestPath());
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public void Load_WhenStateFileContainsInvalidJson_ThrowsJsonException()
    {
        var root = Directory.CreateTempSubdirectory();

        try
        {
            var store = CreateStore(root);

            File.WriteAllText(
                Path.Combine(root.FullName, "editor-state.json"),
                "{");

            Assert.Throws<JsonException>(
                () => store.LoadLastProjectManifestPath());
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    private static EditorProjectStateStore CreateStore(
        DirectoryInfo root)
    {
        return new EditorProjectStateStore(
            Path.Combine(root.FullName, "editor-state.json"));
    }
}