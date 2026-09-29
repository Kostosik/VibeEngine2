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
        string? action = null,
        IReadOnlyList<string>? dropdownOptions = null,
        int selectedIndex = -1,
        bool toggleValue = false,
        string placeholder = "",
        int maxLength = 256,
        bool visible = true,
        bool enabled = true,
        int zIndex = 0)
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

        var options =
            dropdownOptions?.ToArray()
            ?? Array.Empty<string>();

        for (var i = 0; i < options.Length; i++)
        {
            ArgumentNullException.ThrowIfNull(
                options[i],
                nameof(dropdownOptions));
        }

        if (type == UiAssetElementType.Dropdown)
        {
            if (selectedIndex < -1 ||
                selectedIndex >= options.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(selectedIndex));
            }
        }
        else if (selectedIndex != -1)
        {
            throw new ArgumentException(
                "Selected index is only valid for dropdown elements.",
                nameof(selectedIndex));
        }

        if (type == UiAssetElementType.TextBox &&
            maxLength < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxLength));
        }

        Id = id;
        ParentId = parentId;
        Type = type;
        Name = name;
        Layout = layout;
        Text = text;
        Texture = texture;
        Action = action;

        DropdownOptions =
            options;

        SelectedIndex =
            selectedIndex;

        ToggleValue =
            toggleValue;

        Placeholder =
            placeholder;

        MaxLength =
            maxLength;
        Visible =
    visible;

        Enabled =
            enabled;

        ZIndex =
            zIndex;
    }

    public bool Visible { get; }

    public bool Enabled { get; }

    public int ZIndex { get; }

    public Guid Id { get; }

    public Guid ParentId { get; }

    public UiAssetElementType Type { get; }

    public string Name { get; }

    public UiAssetLayout Layout { get; }

    public string Text { get; }

    public AssetPath? Texture { get; }

    public string? Action { get; }

    public IReadOnlyList<string> DropdownOptions { get; }

    public int SelectedIndex { get; }

    public bool ToggleValue { get; }

    public string Placeholder { get; }

    public int MaxLength { get; }
}