using Engine.Core.Assets;

namespace Engine.UI.Assets;

public sealed class UiAssetElement
{
    public UiAssetElement(
        Guid id,
        Guid parentId,
        UiAssetElementType type,
        string name,
        UiAssetLayout layout,
        string text = "",
        AssetPath? texture = null,
        string? action = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "UI asset element id cannot be empty.",
                nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            name);

        if (type == UiAssetElementType.Root &&
            parentId != Guid.Empty)
        {
            throw new ArgumentException(
                "The root UI asset element cannot have a parent.",
                nameof(parentId));
        }

        if (type != UiAssetElementType.Root &&
            parentId == Guid.Empty)
        {
            throw new ArgumentException(
                "Non-root UI asset elements must have a parent.",
                nameof(parentId));
        }

        Id = id;
        ParentId = parentId;
        Type = type;
        Name = name;
        Layout = layout;
        Text = text;
        Texture = texture;
        Action = action;
    }

    public Guid Id { get; }

    public Guid ParentId { get; }

    public UiAssetElementType Type { get; }

    public string Name { get; }

    public UiAssetLayout Layout { get; }

    public string Text { get; }

    public AssetPath? Texture { get; }

    public string? Action { get; }
}