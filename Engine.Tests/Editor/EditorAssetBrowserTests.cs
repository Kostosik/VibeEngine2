using Engine.Editor.Assets;

namespace Engine.Tests.Editor;

public sealed class EditorAssetBrowserTests
{
    [Fact]
    public void NavigateBackAndForward_RestoresVisitedDirectories()
    {
        var root = Directory.CreateTempSubdirectory();

        try
        {
            var first = Directory.CreateDirectory(
                Path.Combine(root.FullName, "First"));

            var second = Directory.CreateDirectory(
                Path.Combine(first.FullName, "Second"));

            var browser = new EditorAssetBrowser(
                new FileSystemAssetSource(),
                root.FullName);

            Assert.False(browser.CanNavigateBack);
            Assert.False(browser.CanNavigateForward);

            Assert.True(browser.NavigateTo(first.FullName));
            Assert.True(browser.NavigateTo(second.FullName));

            Assert.True(browser.CanNavigateBack);
            Assert.False(browser.CanNavigateForward);

            Assert.True(browser.NavigateBack());
            Assert.Equal(first.FullName, browser.CurrentPath);
            Assert.True(browser.CanNavigateForward);

            Assert.True(browser.NavigateForward());
            Assert.Equal(second.FullName, browser.CurrentPath);
            Assert.False(browser.CanNavigateForward);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public void Select_WhenDirectoryIsSelected_DoesNotReturnItAsAsset()
    {
        var root = Directory.CreateTempSubdirectory();

        try
        {
            var directory = Directory.CreateDirectory(
                Path.Combine(root.FullName, "Textures"));

            var browser = new EditorAssetBrowser(
                new FileSystemAssetSource(),
                root.FullName);

            Assert.True(browser.Select(directory.FullName));

            Assert.Equal(
                directory.FullName,
                browser.SelectedEntry?.Path);

            Assert.True(browser.SelectedEntry!.IsDirectory);
            Assert.Null(browser.SelectedAsset);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public void Select_WhenEntryIsDirectory_SelectsItWithoutTreatingItAsFile()
    {
        var root = Directory.CreateTempSubdirectory();

        try
        {
            var directory = Directory.CreateDirectory(
                Path.Combine(root.FullName, "Textures"));

            var browser = new EditorAssetBrowser(
                new FileSystemAssetSource(),
                root.FullName);

            Assert.True(browser.Select(directory.FullName));

            Assert.Equal(
                directory.FullName,
                browser.SelectedEntry?.Path);

            Assert.True(browser.SelectedEntry!.IsDirectory);
            Assert.Null(browser.SelectedAsset);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public void NavigateTo_AfterGoingBack_ClearsForwardHistory()
    {
        var root = Directory.CreateTempSubdirectory();

        try
        {
            var first = Directory.CreateDirectory(
                Path.Combine(root.FullName, "First"));

            var second = Directory.CreateDirectory(
                Path.Combine(first.FullName, "Second"));

            var alternative = Directory.CreateDirectory(
                Path.Combine(first.FullName, "Alternative"));

            var browser = new EditorAssetBrowser(
                new FileSystemAssetSource(),
                root.FullName);

            Assert.True(browser.NavigateTo(first.FullName));
            Assert.True(browser.NavigateTo(second.FullName));
            Assert.True(browser.NavigateBack());

            Assert.True(browser.NavigateTo(alternative.FullName));

            Assert.Equal(alternative.FullName, browser.CurrentPath);
            Assert.False(browser.CanNavigateForward);
            Assert.False(browser.NavigateForward());
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public void Refresh_WhenEntriesChange_RaisesChanged()
    {
        var root =
            Directory.CreateTempSubdirectory();

        try
        {
            var source =
                new MutableAssetSource();

            var browser =
                new EditorAssetBrowser(
                    source,
                    root.FullName);

            var changes =
                0;

            browser.Changed +=
                () => changes++;

            source.Entries =
                new[]
                {
                    new EditorAssetEntry(
                        Path.Combine(
                            root.FullName,
                            "test.asset"),
                        "test.asset",
                        false)
                };

            browser.Refresh();

            Assert.Equal(
                1,
                changes);

            Assert.Single(
                browser.Entries);
        }
        finally
        {
            root.Delete(
                recursive: true);
        }
    }

    [Fact]
    public void Refresh_WhenSelectedAssetDisappears_RaisesChangedAndClearsSelection()
    {
        var root =
            Directory.CreateTempSubdirectory();

        try
        {
            var assetPath =
                Path.Combine(
                    root.FullName,
                    "test.asset");

            var source =
                new MutableAssetSource
                {
                    Entries =
                    new[]
                    {
                        new EditorAssetEntry(
                            assetPath,
                            "test.asset",
                            false)
                    }
                };

            var browser =
                new EditorAssetBrowser(
                    source,
                    root.FullName);

            Assert.True(
                browser.Select(
                    assetPath));

            var changes =
                0;

            browser.Changed +=
                () => changes++;

            source.Entries =
                Array.Empty<EditorAssetEntry>();

            browser.Refresh();

            Assert.Equal(
                1,
                changes);

            Assert.Empty(
                browser.Selection.Items);

            Assert.Null(
                browser.SelectedAsset);
        }
        finally
        {
            root.Delete(
                recursive: true);
        }
    }

    [Fact]
    public void NavigateTo_WhenEntriesDoNotChange_StillRaisesChangedOnce()
    {
        var root =
            Directory.CreateTempSubdirectory();

        var child =
            Directory.CreateDirectory(
                Path.Combine(
                    root.FullName,
                    "Folder"));

        try
        {
            var source =
                new MutableAssetSource
                {
                    Entries =
                    new[]
                    {
                        new EditorAssetEntry(
                            Path.Combine(
                                child.FullName,
                                "test.asset"),
                            "test.asset",
                            false)
                    }
                };

            var browser =
                new EditorAssetBrowser(
                    source,
                    root.FullName);

            var changes =
                0;

            browser.Changed +=
                () => changes++;

            Assert.True(
                browser.NavigateTo(
                    child.FullName));

            Assert.Equal(
                1,
                changes);

            Assert.Equal(
                child.FullName,
                browser.CurrentPath);
        }
        finally
        {
            root.Delete(
                recursive: true);
        }
    }

    private sealed class MutableAssetSource :
        IEditorAssetSource
    {
        public IReadOnlyList<EditorAssetEntry> Entries { get; set; } =
            Array.Empty<EditorAssetEntry>();

        public IReadOnlyList<EditorAssetEntry> GetEntries(
            string path)
        {
            return Entries;
        }
    }
}