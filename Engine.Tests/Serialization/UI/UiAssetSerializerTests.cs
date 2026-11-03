using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.Serialization.Binary;
using Engine.Serialization.UI;
using Engine.UI.Assets;
using Engine.UI.Layout;

namespace Engine.Tests.Serialization.UI;

public sealed class UiAssetSerializerTests
{
    [Fact]
    public void SerializeDeserialize_PreservesSeparatorOrientation()
    {
        var rootId = Guid.NewGuid();
        var separatorId = Guid.NewGuid();

        var asset = new UiAsset(
            "SeparatorTest",
            new Vector2(1280.0f, 720.0f),
            new[]
            {
            new UiAssetElement(
                rootId,
                Guid.Empty,
                UiAssetElementType.Root,
                "Root",
                new UiAssetLayout(
                    UiAnchor.TopLeft,
                    Vector2.Zero,
                    new Vector2(1280.0f, 720.0f))),

            new UiAssetElement(
                separatorId,
                rootId,
                UiAssetElementType.Separator,
                "VerticalSeparator",
                new UiAssetLayout(
                    UiAnchor.TopLeft,
                    Vector2.Zero,
                    new Vector2(2.0f, 200.0f)),
                separatorOrientation:
                    UiAssetSeparatorOrientation.Vertical)
            });

        var serializer = new UiAssetSerializer();

        var data = BinarySerializer.Serialize(
            asset,
            serializer);

        var restored = BinarySerializer.Deserialize(
            data,
            serializer);

        var separator = Assert.Single(
            restored.Elements.Where(
                element => element.Type == UiAssetElementType.Separator));

        Assert.Equal(
            UiAssetSeparatorOrientation.Vertical,
            separator.SeparatorOrientation);
    }

    [Fact]
    public void SerializeDeserialize_PreservesProgressBarValue()
    {
        var rootId = Guid.NewGuid();
        var progressBarId = Guid.NewGuid();

        var asset = new UiAsset(
            "ProgressBarTest",
            new Vector2(1280.0f, 720.0f),
            new[]
            {
            new UiAssetElement(
                rootId,
                Guid.Empty,
                UiAssetElementType.Root,
                "Root",
                new UiAssetLayout(
                    UiAnchor.TopLeft,
                    Vector2.Zero,
                    new Vector2(1280.0f, 720.0f))),

            new UiAssetElement(
                progressBarId,
                rootId,
                UiAssetElementType.ProgressBar,
                "Loading",
                new UiAssetLayout(
                    UiAnchor.TopLeft,
                    Vector2.Zero,
                    new Vector2(300.0f, 24.0f)),
                progressBarValue: 0.75f)
            });

        var serializer = new UiAssetSerializer();

        var data = BinarySerializer.Serialize(
            asset,
            serializer);

        var restored = BinarySerializer.Deserialize(
            data,
            serializer);

        var progressBar = Assert.Single(
            restored.Elements.Where(
                element => element.Type == UiAssetElementType.ProgressBar));

        Assert.Equal(0.75f, progressBar.ProgressBarValue);
    }

    [Fact]
    public void SerializeDeserialize_PreservesSliderValue()
    {
        var rootId = Guid.NewGuid();
        var sliderId = Guid.NewGuid();

        var asset = new UiAsset(
            "SliderTest",
            new Vector2(1280.0f, 720.0f),
            new[]
            {
            new UiAssetElement(
                rootId,
                Guid.Empty,
                UiAssetElementType.Root,
                "Root",
                new UiAssetLayout(
                    UiAnchor.TopLeft,
                    Vector2.Zero,
                    new Vector2(1280.0f, 720.0f))),

            new UiAssetElement(
                sliderId,
                rootId,
                UiAssetElementType.Slider,
                "Volume",
                new UiAssetLayout(
                    UiAnchor.TopLeft,
                    Vector2.Zero,
                    new Vector2(240.0f, 24.0f)),
                sliderValue: 0.75f)
            });

        var serializer = new UiAssetSerializer();

        var data = BinarySerializer.Serialize(
            asset,
            serializer);

        var restored = BinarySerializer.Deserialize(
            data,
            serializer);

        var slider = Assert.Single(
            restored.Elements.Where(
                element => element.Type == UiAssetElementType.Slider));

        Assert.Equal(0.75f, slider.SliderValue);
    }

    [Fact]
    public void SerializeDeserialize_PreservesAsset()
    {
        var rootId =
            Guid.NewGuid();

        var buttonId =
            Guid.NewGuid();

        var asset =
            new UiAsset(
                "MainMenu",
                new Vector2(
                    1280.0f,
                    720.0f),
                new[]
                {
                    new UiAssetElement(
                        rootId,
                        Guid.Empty,
                        UiAssetElementType.Root,
                        "Root",
                        new UiAssetLayout(
                            UiAnchor.TopLeft,
                            Vector2.Zero,
                            new Vector2(
                                1280.0f,
                                720.0f))),

                    new UiAssetElement(
                        buttonId,
                        rootId,
                        UiAssetElementType.Button,
                        "StartButton",
                        new UiAssetLayout(
                            UiAnchor.Center,
                            new Vector2(
                                0.0f,
                                50.0f),
                            new Vector2(
                                300.0f,
                                60.0f)),
                        text: "Start",
                        texture: new AssetPath(
                            "UI/Buttons/start.png"),
                        action: "StartGame")
                });

        var serializer =
            new UiAssetSerializer();

        var data =
            BinarySerializer.Serialize(
                asset,
                serializer);

        var restored =
            BinarySerializer.Deserialize(
                data,
                serializer);

        Assert.Equal(
            asset.Name,
            restored.Name);

        Assert.Equal(
            asset.CanvasSize,
            restored.CanvasSize);

        Assert.Equal(
            asset.Elements.Count,
            restored.Elements.Count);

        var restoredRoot =
            restored.Elements[0];

        Assert.Equal(
            rootId,
            restoredRoot.Id);

        Assert.Equal(
            Guid.Empty,
            restoredRoot.ParentId);

        var restoredButton =
            restored.Elements[1];

        Assert.Equal(
            buttonId,
            restoredButton.Id);

        Assert.Equal(
            rootId,
            restoredButton.ParentId);

        Assert.Equal(
            UiAssetElementType.Button,
            restoredButton.Type);

        Assert.Equal(
            "StartButton",
            restoredButton.Name);

        Assert.Equal(
            UiAnchor.Center,
            restoredButton.Layout.Anchor);

        Assert.Equal(
            new Vector2(
                0.0f,
                50.0f),
            restoredButton.Layout.Offset);

        Assert.Equal(
            new Vector2(
                300.0f,
                60.0f),
            restoredButton.Layout.Size);

        Assert.Equal(
            "Start",
            restoredButton.Text);

        Assert.Equal(
            new AssetPath(
                "UI/Buttons/start.png"),
            restoredButton.Texture);

        Assert.Equal(
            "StartGame",
            restoredButton.Action);
    }

    [Fact]
    public void Deserialize_UnsupportedVersion_Throws()
    {
        var writer =
            new SerializationWriter();

        writer.WriteInt32(
            UiAsset.CurrentVersion + 1);

        writer.WriteString(
            "FutureUi");

        writer.WriteSingle(
            1280.0f);

        writer.WriteSingle(
            720.0f);

        writer.WriteInt32(
            1);

        Assert.Throws<InvalidDataException>(
            () =>
            {
                var reader =
                    new SerializationReader(
                        writer.WrittenSpan);

                new UiAssetSerializer()
                    .Deserialize(
                        ref reader);
            });
    }

    [Fact]
    public void Deserialize_InvalidElementType_Throws()
    {
        var writer =
            new SerializationWriter();

        writer.WriteInt32(
            UiAsset.CurrentVersion);

        writer.WriteString(
            "InvalidUi");

        writer.WriteSingle(
            1280.0f);

        writer.WriteSingle(
            720.0f);

        writer.WriteInt32(
            1);

        writer.WriteGuid(
            Guid.NewGuid());

        writer.WriteGuid(
            Guid.Empty);

        writer.WriteInt32(
            999);

        Assert.Throws<InvalidDataException>(
            () =>
            {
                var reader =
                    new SerializationReader(
                        writer.WrittenSpan);

                new UiAssetSerializer()
                    .Deserialize(
                        ref reader);
            });
    }
}