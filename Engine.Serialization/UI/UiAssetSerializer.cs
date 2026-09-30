using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.Serialization.Binary;
using Engine.UI.Assets;
using Engine.UI.Layout;

namespace Engine.Serialization.UI;

public sealed class UiAssetSerializer :
    IBinarySerializer<UiAsset>
{
    public void Serialize(
        ref SerializationWriter writer,
        UiAsset value)
    {
        ArgumentNullException.ThrowIfNull(
            value);

        writer.WriteInt32(
            UiAsset.CurrentVersion);

        writer.WriteString(
            value.Name);

        writer.WriteSingle(
            value.CanvasSize.X);

        writer.WriteSingle(
            value.CanvasSize.Y);

        writer.WriteInt32(
            value.Elements.Count);

        foreach (var element in value.Elements)
        {
            writer.WriteGuid(
                element.Id);

            writer.WriteGuid(
                element.ParentId);

            writer.WriteInt32(
                (int)element.Type);

            writer.WriteString(
                element.Name);

            writer.WriteSingle(
                element.Layout.Anchor.X);

            writer.WriteSingle(
                element.Layout.Anchor.Y);

            writer.WriteSingle(
                element.Layout.Offset.X);

            writer.WriteSingle(
                element.Layout.Offset.Y);

            writer.WriteSingle(
                element.Layout.Size.X);

            writer.WriteSingle(
                element.Layout.Size.Y);

            writer.WriteString(
                element.Text);

            writer.WriteBoolean(
                element.Texture.HasValue);

            if (element.Texture.HasValue)
            {
                writer.WriteString(
                    element.Texture.Value.Value);
            }

            writer.WriteString(
                element.Action);

            writer.WriteInt32(
                element.DropdownOptions.Count);

            foreach (var option in
                     element.DropdownOptions)
            {
                writer.WriteString(
                    option);
            }

            writer.WriteInt32(
                element.SelectedIndex);

            writer.WriteBoolean(
                element.ToggleValue);

            writer.WriteString(
                element.Placeholder);

            writer.WriteInt32(
                element.MaxLength);

            writer.WriteBoolean(
    element.Visible);

            writer.WriteBoolean(
                element.Enabled);

            writer.WriteInt32(
                element.ZIndex);
        }
    }

    public UiAsset Deserialize(
        ref SerializationReader reader)
    {
        var version =
            reader.ReadInt32();

        if (version < 1 ||
            version > UiAsset.CurrentVersion)
        {
            throw new InvalidDataException(
                $"UI asset version '{version}' is not supported.");
        }

        var name =
            reader.ReadString()
            ?? throw new InvalidDataException(
                "UI asset name cannot be null.");

        var canvasSize =
            new Vector2(
                reader.ReadSingle(),
                reader.ReadSingle());

        var count =
            reader.ReadInt32();

        if (count < 1 ||
            count >
            reader.Context.MaxCollectionLength)
        {
            throw new InvalidDataException(
                $"UI asset element count '{count}' is invalid.");
        }

        var elements =
            new List<UiAssetElement>(
                count);

        for (var i = 0;
             i < count;
             i++)
        {
            var id =
                reader.ReadGuid();

            var parentId =
                reader.ReadGuid();

            var typeValue =
                reader.ReadInt32();

            if (!Enum.IsDefined(
                    typeof(UiAssetElementType),
                    typeValue))
            {
                throw new InvalidDataException(
                    $"UI asset contains unknown element type '{typeValue}'.");
            }

            var type =
                (UiAssetElementType)typeValue;

            var elementName =
                reader.ReadString()
                ?? throw new InvalidDataException(
                    "UI asset element name cannot be null.");

            var anchor =
                new UiAnchor(
                    reader.ReadSingle(),
                    reader.ReadSingle());

            var layout =
                new UiAssetLayout(
                    anchor,

                    new Vector2(
                        reader.ReadSingle(),
                        reader.ReadSingle()),

                    new Vector2(
                        reader.ReadSingle(),
                        reader.ReadSingle()));

            var text =
                reader.ReadString()
                ?? string.Empty;

            AssetPath? texture = null;

            if (reader.ReadBoolean())
            {
                var textureValue =
                    reader.ReadString()
                    ?? throw new InvalidDataException(
                        "UI asset texture path cannot be null.");

                texture =
                    new AssetPath(
                        textureValue);
            }

            var action =
                reader.ReadString();

            var dropdownOptions =
    Array.Empty<string>();

            var selectedIndex =
                -1;

            var toggleValue =
                false;

            var placeholder =
                string.Empty;

            var maxLength =
                256;

            if (version >= 2)
            {
                var optionCount =
                    reader.ReadInt32();

                if (optionCount < 0 ||
                    optionCount >
                    reader.Context.MaxCollectionLength)
                {
                    throw new InvalidDataException(
                        $"UI dropdown option count '{optionCount}' is invalid.");
                }

                var options =
                    new string[optionCount];

                for (var optionIndex = 0;
                     optionIndex < optionCount;
                     optionIndex++)
                {
                    options[optionIndex] =
                        reader.ReadString()
                        ?? throw new InvalidDataException(
                            "UI dropdown option cannot be null.");
                }

                dropdownOptions =
                    options;

                selectedIndex =
                    reader.ReadInt32();

                toggleValue =
                    reader.ReadBoolean();

                placeholder =
                    reader.ReadString()
                    ?? string.Empty;

                maxLength =
                    reader.ReadInt32();
            }

            var visible =
    true;

            var enabled =
                true;

            var zIndex =
                0;

            if (version >= 3)
            {
                visible =
                    reader.ReadBoolean();

                enabled =
                    reader.ReadBoolean();

                zIndex =
                    reader.ReadInt32();
            }

            elements.Add(
                new UiAssetElement(
                    id,
                    parentId,
                    type,
                    elementName,
                    layout,
                    text,
                    texture,
                    action, dropdownOptions,
                    selectedIndex,
                    toggleValue,
                    placeholder,
                    maxLength,
                    visible,
                    enabled,
                    zIndex));
        }

        return new UiAsset(
            name,
            canvasSize,
            elements);
    }
}