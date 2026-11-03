using Engine.Core.Assets;
using Engine.UI.Controls;

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
        int zIndex = 0,
        bool preserveAspectRatio = true, 
        float sliderValue = 0.0f, 
        float progressBarValue = 0.0f, 
        UiAssetSeparatorOrientation separatorOrientation = UiAssetSeparatorOrientation.Horizontal)
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

        if (type == UiAssetElementType.Slider)
        {
            if (!float.IsFinite(sliderValue) ||
                sliderValue < 0.0f ||
                sliderValue > 1.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sliderValue));
            }
        }
        else if (sliderValue != 0.0f)
        {
            throw new ArgumentException(
                "Slider value is only valid for slider elements.",
                nameof(sliderValue));
        }

        if (type == UiAssetElementType.ProgressBar)
        {
            if (!float.IsFinite(progressBarValue) ||
                progressBarValue < 0.0f ||
                progressBarValue > 1.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(progressBarValue));
            }
        }
        else if (progressBarValue != 0.0f)
        {
            throw new ArgumentException(
                "Progress bar value is only valid for progress bar elements.",
                nameof(progressBarValue));
        }

        if (type == UiAssetElementType.Separator)
        {
            if (!Enum.IsDefined(separatorOrientation))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(separatorOrientation));
            }
        }
        else if (separatorOrientation !=
                 UiAssetSeparatorOrientation.Horizontal)
        {
            throw new ArgumentException(
                "Separator orientation is only valid for separator elements.",
                nameof(separatorOrientation));
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
        PreserveAspectRatio = preserveAspectRatio;
        DropdownOptions =
            options;
        SeparatorOrientation = separatorOrientation;
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
        SliderValue = sliderValue;
        ZIndex =
            zIndex;
        ProgressBarValue = progressBarValue;
    }
    public float SliderValue { get; }
    public bool Visible { get; }
    public float ProgressBarValue { get; }
    public bool Enabled { get; }

    public int ZIndex { get; }

    public Guid Id { get; }

    public Guid ParentId { get; }
    public UiAssetSeparatorOrientation SeparatorOrientation { get; }
    public UiAssetElementType Type { get; }

    public string Name { get; }

    public UiAssetLayout Layout { get; }

    public string Text { get; }

    public AssetPath? Texture { get; }
    public bool PreserveAspectRatio { get; }
    public string? Action { get; }

    public IReadOnlyList<string> DropdownOptions { get; }

    public int SelectedIndex { get; }

    public bool ToggleValue { get; }

    public string Placeholder { get; }

    public int MaxLength { get; }
}