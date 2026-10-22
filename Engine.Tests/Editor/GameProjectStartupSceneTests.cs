using Engine.Editor;
using Engine.Editor.Documents;
using Engine.Editor.Projects;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class GameProjectStartupSceneTests
{
    [Fact]
    public void SetStartupScene_PersistsRelativePath()
    {
        var root = CreateProject();

        try
        {
            var project = GameProject.Open(
                Path.Combine(root, "TestGame.vibeproject"));

            project.SetStartupScene(
                Path.Combine(root, "Scenes", "Arena.world"));

            Assert.Equal(
                "Scenes/Arena.world",
                project.StartupSceneRelativePath);

            var reopened = GameProject.Open(
                Path.Combine(root, "TestGame.vibeproject"));

            Assert.Equal(
                "Scenes/Arena.world",
                reopened.StartupSceneRelativePath);

            Assert.Equal(
                "TestGame.csproj",
                Path.GetRelativePath(
                    root,
                    reopened.ProjectFilePath)
                    .Replace('\\', '/'));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void SetStartupScene_RejectsPathOutsideProject()
    {
        var root = CreateProject();

        try
        {
            var project = GameProject.Open(
                Path.Combine(root, "TestGame.vibeproject"));

            var outsidePath = Path.Combine(
                Path.GetDirectoryName(root)!,
                $"{Path.GetFileName(root)}-outside.world");

            Assert.Throws<ArgumentException>(
                () => project.SetStartupScene(outsidePath));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void SetStartupScene_RequiresDocumentBelongingToActiveProject()
    {
        var root = CreateProject();

        try
        {
            var project = GameProject.Open(
                Path.Combine(root, "TestGame.vibeproject"));

            var outsideScene = Path.Combine(
                Path.GetDirectoryName(root)!,
                $"{Path.GetFileName(root)}-external.world");

            File.WriteAllText(outsideScene, "test");

            var editor = new EditorContext();
            editor.SetActiveGameProject(project);

            using var ecsWorld = new Engine.ECS.World();

            using var world = new World(
                new ChunkSize(16, 16),
                ecsWorld);

            using var document = editor.OpenDocument(
                world,
                outsideScene);

            Assert.Throws<InvalidOperationException>(
                () => editor.SetStartupScene(document));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateProject()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngine2-Test-{Guid.NewGuid():N}");

        Directory.CreateDirectory(
            Path.Combine(root, "Scenes"));

        File.WriteAllText(
            Path.Combine(root, "TestGame.csproj"),
            "<Project />");

        File.WriteAllText(
            Path.Combine(root, "TestGame.vibeproject"),
            """
            {
              "formatVersion": 1,
              "name": "TestGame",
              "startupProject": "TestGame.csproj",
              "startupScene": "Scenes/Main.world"
            }
            """);

        File.WriteAllText(
            Path.Combine(root, "Scenes", "Main.world"),
            "test");

        File.WriteAllText(
            Path.Combine(root, "Scenes", "Arena.world"),
            "test");

        return root;
    }
}