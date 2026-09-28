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