using Engine.Editor.Assets;

namespace Engine.Tests.Editor;

public sealed class EditorAssetBrowserViewStateTests
{
    [Fact]
    public void GetVisibleEntries_FiltersByNameIgnoringCase()
    {
        var state = new EditorAssetBrowserViewState();

        var entries = new[]
        {
            new EditorAssetEntry(
                "Assets/Texture.png",
                "Texture.png",
                false),

            new EditorAssetEntry(
                "Assets/Textures",
                "Textures",
                true),

            new EditorAssetEntry(
                "Assets/Fonts",
                "Fonts",
                true),

            new EditorAssetEntry(
                "Assets/other.png",
                "other.png",
                false)
        };

        state.SetSearchText("TEXT");

        var result = state.GetVisibleEntries(entries);

        Assert.Equal(
            new[] { "Textures", "Texture.png" },
            result.Select(entry => entry.Name));
    }

    [Fact]
    public void GetVisibleEntries_WhenKindFilterIsSet_KeepsDirectoriesAndMatchingFiles()
    {
        var state = new EditorAssetBrowserViewState();

        var entries = new[]
        {
        new EditorAssetEntry(
            "Assets/Textures",
            "Textures",
            true),

        new EditorAssetEntry(
            "Assets/ship.png",
            "ship.png",
            false),

        new EditorAssetEntry(
            "Assets/music.ogg",
            "music.ogg",
            false),

        new EditorAssetEntry(
            "Assets/icon.jpg",
            "icon.jpg",
            false)
    };

        state.SetKindFilter(EditorAssetKind.Image);

        var result = state.GetVisibleEntries(entries);

        Assert.Equal(
            new[] { "Textures", "icon.jpg", "ship.png" },
            result.Select(entry => entry.Name));
    }

    [Fact]
    public void GetVisibleEntries_KeepsDirectoriesFirstWhenDescending()
    {
        var state = new EditorAssetBrowserViewState();

        state.ToggleSortDirection();

        var entries = new[]
        {
            new EditorAssetEntry("Assets/a.txt", "a.txt", false),
            new EditorAssetEntry("Assets/z.txt", "z.txt", false),
            new EditorAssetEntry("Assets/aFolder", "aFolder", true),
            new EditorAssetEntry("Assets/zFolder", "zFolder", true)
        };

        var result = state.GetVisibleEntries(entries);

        Assert.Equal(
            new[] { "zFolder", "aFolder", "z.txt", "a.txt" },
            result.Select(entry => entry.Name));
    }
}