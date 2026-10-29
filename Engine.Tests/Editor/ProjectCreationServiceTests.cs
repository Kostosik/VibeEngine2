using Engine.Editor.Projects;

namespace Engine.Tests.Editor;

public sealed class ProjectCreationServiceTests
{
    [Fact]
    public void Create_WhenRuntimeCopyFails_RemovesStagingDirectoryAndDoesNotPublishPartialProject()
    {
        using var root = new TemporaryDirectory();

        var runtimeDirectory = Path.Combine(root.Path, "Runtime");
        Directory.CreateDirectory(runtimeDirectory);

        foreach (var assemblyName in EngineRuntimeBuilder.RequiredAssemblies)
        {
            File.WriteAllText(
                Path.Combine(runtimeDirectory, assemblyName),
                "test");
        }

        // Файл существует, но не разрешает другим процессам читать его.
        var lockedFilePath = Path.Combine(runtimeDirectory, "Locked.dll");
        File.WriteAllText(lockedFilePath, "locked");

        using var lockedFile = new FileStream(
            lockedFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.None);

        var parentDirectory = Path.Combine(root.Path, "Projects");
        var projectDirectory = Path.Combine(parentDirectory, "MyGame");

        var service = new ProjectCreationService(runtimeDirectory);

        var exception = Record.Exception(() => service.Create(
            new ProjectCreationRequest(
                "MyGame",
                parentDirectory,
                projectDirectory)));

        Assert.NotNull(exception);

        Assert.True(
            exception is IOException or UnauthorizedAccessException,
            $"Unexpected exception type: {exception.GetType().FullName}");

        // Незавершённый проект не должен появиться по целевому пути.
        Assert.False(Directory.Exists(projectDirectory));
        Assert.False(File.Exists(projectDirectory));

        // Временная директория должна быть удалена после ошибки.
        Assert.Empty(
            Directory.EnumerateDirectories(
                parentDirectory,
                ".vibeengine-project-*.tmp",
                SearchOption.TopDirectoryOnly));
    }

    [Fact]
    public void Create_GeneratedProgram_RegistersUiAssetLoader()
    {
        using var root = new TemporaryDirectory();

        var runtimeDirectory =
            Path.Combine(root.Path, "Runtime");

        Directory.CreateDirectory(runtimeDirectory);

        foreach (var assemblyName in EngineRuntimeBuilder.RequiredAssemblies)
        {
            File.WriteAllText(
                Path.Combine(runtimeDirectory, assemblyName),
                "test");
        }

        var parentDirectory =
            Path.Combine(root.Path, "Projects");

        var projectDirectory =
            Path.Combine(parentDirectory, "UiLoadingGame");

        var service =
            new ProjectCreationService(runtimeDirectory);

        var createdDirectory = service.Create(
            new ProjectCreationRequest(
                "UiLoadingGame",
                parentDirectory,
                projectDirectory));

        var program = File.ReadAllText(
            Path.Combine(createdDirectory, "Program.cs"));

        Assert.Contains("BinaryContentLoader<UiAsset>", program);
        Assert.Contains("UiAssetSerializer", program);
        Assert.Contains(".ui", program);
    }

    [Fact]
    public void Create_CreatesMissingParentDirectories()
    {
        using var root = new TemporaryDirectory();

        var runtimeDirectory =
            Path.Combine(root.Path, "Runtime");

        Directory.CreateDirectory(runtimeDirectory);

        foreach (var assemblyName in EngineRuntimeBuilder.RequiredAssemblies)
        {
            File.WriteAllText(
                Path.Combine(runtimeDirectory, assemblyName),
                "test");
        }

        var parentDirectory =
            Path.Combine(
                root.Path,
                "NewParent",
                "Nested");

        var projectDirectory =
            Path.Combine(
                parentDirectory,
                "MyGame");

        var request = new ProjectCreationRequest(
            "MyGame",
            parentDirectory,
            projectDirectory);

        var service =
            new ProjectCreationService(runtimeDirectory);

        var createdDirectory =
            service.Create(request);

        Assert.Equal(
            Path.GetFullPath(projectDirectory),
            createdDirectory);

        Assert.True(
            Directory.Exists(parentDirectory));

        Assert.True(
            File.Exists(
                Path.Combine(
                    projectDirectory,
                    "MyGame.csproj")));

        Assert.True(
            File.Exists(
                Path.Combine(
                    projectDirectory,
                    "MyGame.vibeproject")));
    }

    [Fact]
    public void Create_RejectsMissingRootDirectory()
    {
        using var root = new TemporaryDirectory();

        var parentDirectory =
            Path.Combine(
                root.Path,
                "NewParent");

        var projectDirectory =
            Path.Combine(
                parentDirectory,
                "MyGame");

        var request = new ProjectCreationRequest(
            "MyGame",
            parentDirectory,
            projectDirectory);

        var service =
            new ProjectCreationService(
                rootDirectoryExists: static _ => false);

        Assert.Throws<DirectoryNotFoundException>(
            () => service.Create(request));

        Assert.False(
            Directory.Exists(parentDirectory));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory =
            Directory.CreateTempSubdirectory();

        public string Path => _directory.FullName;

        public void Dispose()
        {
            if (_directory.Exists)
            {
                _directory.Delete(recursive: true);
            }
        }
    }
}