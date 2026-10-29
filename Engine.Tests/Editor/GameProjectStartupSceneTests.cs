using Engine.Editor;
using Engine.Editor.Documents;
using Engine.Editor.Projects;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class GameProjectStartupSceneTests
{
    [Fact]
    public void Open_WhenStartupProjectIsSymbolicLinkOutsideProject_Throws()
    {
        var tempRoot = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineProjectSymlink-{Guid.NewGuid():N}");

        var projectRoot = Path.Combine(tempRoot, "Project");
        var externalRoot = Path.Combine(tempRoot, "External");

        var manifestPath = Path.Combine(
            projectRoot,
            "Game.vibeproject");

        var externalProjectPath = Path.Combine(
            externalRoot,
            "ExternalGame.csproj");

        var linkedProjectPath = Path.Combine(
            projectRoot,
            "LinkedGame.csproj");

        Directory.CreateDirectory(projectRoot);
        Directory.CreateDirectory(externalRoot);

        try
        {
            File.WriteAllText(
                externalProjectPath,
                "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

            File.WriteAllText(
                manifestPath,
                "{\"formatVersion\":1,\"name\":\"SymlinkTest\",\"startupProject\":\"LinkedGame.csproj\"}");

            try
            {
                File.CreateSymbolicLink(
                    linkedProjectPath,
                    externalProjectPath);
            }
            catch (Exception exception) when (
                exception is UnauthorizedAccessException or
                IOException or
                PlatformNotSupportedException)
            {
                return;
            }

            Assert.Throws<InvalidDataException>(
                () => GameProject.Open(manifestPath));
        }
        finally
        {
            if (File.Exists(linkedProjectPath))
            {
                File.Delete(linkedProjectPath);
            }

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void Open_WhenStartupSceneIsSymbolicLinkOutsideProject_Throws()
    {
        var tempRoot = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineStartupSceneSymlink-{Guid.NewGuid():N}");

        var projectRoot = Path.Combine(tempRoot, "Project");
        var externalRoot = Path.Combine(tempRoot, "External");
        var scenesRoot = Path.Combine(projectRoot, "Scenes");

        var manifestPath = Path.Combine(
            projectRoot,
            "Game.vibeproject");

        var projectFilePath = Path.Combine(
            projectRoot,
            "Game.csproj");

        var externalScenePath = Path.Combine(
            externalRoot,
            "Outside.world");

        var linkedScenePath = Path.Combine(
            scenesRoot,
            "Linked.world");

        Directory.CreateDirectory(scenesRoot);
        Directory.CreateDirectory(externalRoot);

        try
        {
            File.WriteAllText(
                projectFilePath,
                "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

            File.WriteAllText(
                externalScenePath,
                "external scene");

            File.WriteAllText(
                manifestPath,
                """
            {
              "formatVersion": 1,
              "name": "SymlinkTest",
              "startupProject": "Game.csproj",
              "startupScene": "Scenes/Linked.world"
            }
            """);

            try
            {
                File.CreateSymbolicLink(
                    linkedScenePath,
                    externalScenePath);
            }
            catch (Exception exception) when (
                exception is UnauthorizedAccessException or
                IOException or
                PlatformNotSupportedException)
            {
                return;
            }

            Assert.Throws<InvalidDataException>(
                () => GameProject.Open(manifestPath));
        }
        finally
        {
            if (File.Exists(linkedScenePath))
            {
                File.Delete(linkedScenePath);
            }

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void SetStartupScene_WhenSceneIsSymbolicLinkOutsideProject_Throws()
    {
        var tempRoot = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineSetStartupSceneSymlink-{Guid.NewGuid():N}");

        var projectRoot = Path.Combine(tempRoot, "Project");
        var externalRoot = Path.Combine(tempRoot, "External");

        var manifestPath = Path.Combine(
            projectRoot,
            "Game.vibeproject");

        var projectFilePath = Path.Combine(
            projectRoot,
            "Game.csproj");

        var externalScenePath = Path.Combine(
            externalRoot,
            "Outside.world");

        var linkedScenePath = Path.Combine(
            projectRoot,
            "Linked.world");

        Directory.CreateDirectory(projectRoot);
        Directory.CreateDirectory(externalRoot);

        try
        {
            File.WriteAllText(
                projectFilePath,
                "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

            File.WriteAllText(
                externalScenePath,
                "external scene");

            File.WriteAllText(
                manifestPath,
                """
            {
              "formatVersion": 1,
              "name": "SymlinkTest",
              "startupProject": "Game.csproj"
            }
            """);

            GameProject project = GameProject.Open(manifestPath);

            try
            {
                File.CreateSymbolicLink(
                    linkedScenePath,
                    externalScenePath);
            }
            catch (Exception exception) when (
                exception is UnauthorizedAccessException or
                IOException or
                PlatformNotSupportedException)
            {
                return;
            }

            Assert.Throws<InvalidDataException>(
                () => project.SetStartupScene(linkedScenePath));
        }
        finally
        {
            if (File.Exists(linkedScenePath))
            {
                File.Delete(linkedScenePath);
            }

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void Open_RejectsStartupSceneOutsideProject()
    {
        var root = CreateProject();

        try
        {
            var manifestPath = Path.Combine(
                root,
                "TestGame.vibeproject");

            File.WriteAllText(
                manifestPath,
                """
            {
              "formatVersion": 1,
              "name": "TestGame",
              "startupProject": "TestGame.csproj",
              "startupScene": "../outside.world"
            }
            """);

            Assert.Throws<InvalidDataException>(
                () => GameProject.Open(manifestPath));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

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