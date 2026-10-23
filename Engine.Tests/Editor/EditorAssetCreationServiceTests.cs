
using Engine.Editor.Assets;

namespace Engine.Tests.Editor;

public sealed class EditorAssetCreationServiceTests
{
    [Fact]
    public void CreateTextFile_WritesContentsAndSelectsCreatedAsset()
    {
        using var root = new TemporaryAssetRoot();

        var browser = root.CreateBrowser();
        var service = new EditorAssetCreationService(browser);

        const string contents = """{"name":"Искра"}""";

        var path = service.CreateTextFile(
            "Spark",
            ".particle.json",
            contents);

        Assert.Equal(
            Path.Combine(root.Path, "Spark.particle.json"),
            path);

        Assert.Equal(contents, File.ReadAllText(path));
        Assert.Equal(path, browser.SelectedAsset?.Path);
        Assert.False(browser.SelectedEntry!.IsDirectory);
    }

    [Fact]
    public void CreateDirectory_CreatesAndSelectsFolder()
    {
        using var root = new TemporaryAssetRoot();

        var browser = root.CreateBrowser();
        var service = new EditorAssetCreationService(browser);

        var path = service.CreateDirectory("My Custom Folder");

        Assert.True(Directory.Exists(path));
        Assert.Equal(path, browser.SelectedEntry?.Path);
        Assert.True(browser.SelectedEntry!.IsDirectory);
    }

    [Fact]
    public void CreateTextFile_DoesNotOverwriteExistingFile()
    {
        using var root = new TemporaryAssetRoot();

        var existingPath = Path.Combine(
            root.Path,
            "Spark.particle.json");

        File.WriteAllText(existingPath, "original");

        var service = new EditorAssetCreationService(
            root.CreateBrowser());

        Assert.Throws<IOException>(
            () => service.CreateTextFile(
                "Spark",
                ".particle.json",
                "replacement"));

        Assert.Equal("original", File.ReadAllText(existingPath));
    }

    [Fact]
    public void Create_WhenTargetDirectoryIsProvided_CreatesAndSelectsAssetThere()
    {
        using var root = new TemporaryAssetRoot();

        var targetDirectory = Directory.CreateDirectory(
            Path.Combine(root.Path, "My Custom Folder"));

        var browser = root.CreateBrowser();
        var service = new EditorAssetCreationService(browser);

        var definition =
            EditorAssetCreationRegistry.CreateDefault()
                .GetRequired("particle-effect");

        var path = service.Create(
            definition,
            "Smoke",
            targetDirectory.FullName);

        Assert.True(File.Exists(path));

        Assert.Equal(
            targetDirectory.FullName,
            browser.CurrentPath);

        Assert.Equal(
            path,
            browser.SelectedAsset?.Path);
    }

    [Fact]
    public void CreateDirectory_RejectsExistingDirectory()
    {
        using var root = new TemporaryAssetRoot();

        Directory.CreateDirectory(
            Path.Combine(root.Path, "Effects"));

        var service = new EditorAssetCreationService(
            root.CreateBrowser());

        Assert.Throws<IOException>(
            () => service.CreateDirectory("Effects"));
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("nested/name")]
    [InlineData(@"nested\name")]
    [InlineData(".")]
    [InlineData("..")]
    public void CreateTextFile_RejectsInvalidName(string name)
    {
        using var root = new TemporaryAssetRoot();

        var service = new EditorAssetCreationService(
            root.CreateBrowser());

        Assert.Throws<ArgumentException>(
            () => service.CreateTextFile(
                name,
                ".particle.json",
                "{}"));

        Assert.Empty(Directory.GetFiles(root.Path));
    }

    [Fact]
    public void CreateTextFile_RejectsInvalidExtension()
    {
        using var root = new TemporaryAssetRoot();

        var service = new EditorAssetCreationService(
            root.CreateBrowser());

        Assert.Throws<ArgumentException>(
            () => service.CreateTextFile(
                "Spark",
                "../outside.json",
                "{}"));

        Assert.Empty(Directory.GetFiles(root.Path));
    }

    private sealed class TemporaryAssetRoot : IDisposable
    {
        private readonly DirectoryInfo _directory =
            Directory.CreateTempSubdirectory();

        public string Path => _directory.FullName;

        public EditorAssetBrowser CreateBrowser()
        {
            return new EditorAssetBrowser(
                new FileSystemAssetSource(),
                Path);
        }

        public void Dispose()
        {
            _directory.Delete(recursive: true);
        }
    }
}
