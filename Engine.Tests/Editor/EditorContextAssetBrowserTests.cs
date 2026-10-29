using Engine.Editor;
using Engine.Editor.Assets;
using Engine.Editor.Projects;

namespace Engine.Tests.Editor;

public sealed class EditorContextAssetBrowserTests
{
    [Fact]
    public void SetActiveGameProject_UpdatesAssetBrowserRootAndSelection()
    {
        var temp = Directory.CreateTempSubdirectory();

        try
        {
            var editorAssets = Directory.CreateDirectory(
                Path.Combine(temp.FullName, "EditorAssets"));

            File.WriteAllText(
                Path.Combine(editorAssets.FullName, "editor-only.asset"),
                "editor");

            var firstManifest = CreateProject(
                temp.FullName,
                "FirstGame",
                "first.asset");

            var secondManifest = CreateProject(
                temp.FullName,
                "SecondGame",
                "second.asset");

            var editor = new EditorContext();

            editor.InitializeAssetBrowser(
                new FileSystemAssetSource(),
                editorAssets.FullName);

            var browser = editor.AssetBrowser!;

            // Изначально используется каталог редактора.
            Assert.Equal(
                Path.GetFullPath(editorAssets.FullName),
                browser.RootPath);

            Assert.Contains(
                browser.Entries,
                entry => entry.Name == "editor-only.asset");

            // Переключаемся на первый игровой проект.
            var firstProject = GameProject.Open(firstManifest);

            editor.SetActiveGameProject(firstProject);

            var firstAssets = Path.Combine(
                Path.GetDirectoryName(firstManifest)!,
                "Assets");

            Assert.Equal(
                Path.GetFullPath(firstAssets),
                browser.RootPath);

            Assert.Equal(browser.RootPath, browser.CurrentPath);

            Assert.Contains(
                browser.Entries,
                entry => entry.Name == "first.asset");

            Assert.DoesNotContain(
                browser.Entries,
                entry => entry.Name == "editor-only.asset");

            // Выбираем ресурс первого проекта.
            var firstAssetPath = Path.Combine(
                firstAssets,
                "first.asset");

            Assert.True(browser.Select(firstAssetPath));
            Assert.NotNull(browser.SelectedAsset);

            // Переключаемся на второй игровой проект.
            var secondProject = GameProject.Open(secondManifest);

            editor.SetActiveGameProject(secondProject);

            var secondAssets = Path.Combine(
                Path.GetDirectoryName(secondManifest)!,
                "Assets");

            Assert.Equal(
                Path.GetFullPath(secondAssets),
                browser.RootPath);

            Assert.Equal(browser.RootPath, browser.CurrentPath);

            Assert.Empty(browser.Selection.Items);

            Assert.Contains(
                browser.Entries,
                entry => entry.Name == "second.asset");

            Assert.DoesNotContain(
                browser.Entries,
                entry => entry.Name == "first.asset");

            // Возвращаемся к каталогу ресурсов редактора.
            editor.SetActiveGameProject(null);

            Assert.Equal(
                Path.GetFullPath(editorAssets.FullName),
                browser.RootPath);

            Assert.Equal(browser.RootPath, browser.CurrentPath);

            Assert.Contains(
                browser.Entries,
                entry => entry.Name == "editor-only.asset");

            Assert.DoesNotContain(
                browser.Entries,
                entry => entry.Name == "second.asset");
        }
        finally
        {
            temp.Delete(recursive: true);
        }
    }

    private static string CreateProject(
        string parentDirectory,
        string name,
        string assetFileName)
    {
        var projectRoot = Directory.CreateDirectory(
            Path.Combine(parentDirectory, name));

        var assets = Directory.CreateDirectory(
            Path.Combine(projectRoot.FullName, "Assets"));

        File.WriteAllText(
            Path.Combine(projectRoot.FullName, $"{name}.csproj"),
            "<Project />");

        File.WriteAllText(
            Path.Combine(projectRoot.FullName, $"{name}.vibeproject"),
            $$"""
            {
              "formatVersion": 1,
              "name": "{{name}}",
              "startupProject": "{{name}}.csproj"
            }
            """);

        File.WriteAllText(
            Path.Combine(assets.FullName, assetFileName),
            name);

        return Path.Combine(
            projectRoot.FullName,
            $"{name}.vibeproject");
    }
}