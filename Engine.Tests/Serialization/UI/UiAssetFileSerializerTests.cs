using Engine.Core.Math;
using Engine.Serialization.UI;
using Engine.UI.Assets;
using Engine.UI.Layout;

namespace Engine.Tests.Serialization.UI;

public sealed class UiAssetFileSerializerTests
{
    [Fact]
    public void SaveLoad_RoundTripsUiAsset()
    {
        var path =
            Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid():N}.ui");

        try
        {
            var asset =
                new UiAsset(
                    "MainMenu",
                    new Vector2(
                        1280.0f,
                        720.0f),
                    new[]
                    {
                        new UiAssetElement(
                            Guid.NewGuid(),
                            Guid.Empty,
                            UiAssetElementType.Root,
                            "Root",
                            new UiAssetLayout(
                                UiAnchor.TopLeft,
                                Vector2.Zero,
                                new Vector2(
                                    1280.0f,
                                    720.0f)))
                    });

            UiAssetFileSerializer.Save(
                path,
                asset);

            Assert.True(
                File.Exists(path));

            var restored =
                UiAssetFileSerializer.Load(
                    path);

            Assert.Equal(
                asset.Name,
                restored.Name);

            Assert.Equal(
                asset.CanvasSize,
                restored.CanvasSize);

            Assert.Equal(
                asset.Elements.Count,
                restored.Elements.Count);

            Assert.Equal(
                asset.Elements[0].Id,
                restored.Elements[0].Id);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}