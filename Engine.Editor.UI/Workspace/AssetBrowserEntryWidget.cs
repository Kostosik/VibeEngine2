
using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.Input;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

internal sealed class AssetBrowserEntryWidget : UiContainer
{
    private readonly UiImage? _icon;
    private readonly UiLabel _label;
    private readonly bool _isGridMode;

    public AssetBrowserEntryWidget(
        string name,
        TextureHandle texture,
        bool isGridMode,
        bool isSelected)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _isGridMode = isGridMode;
        IsSelected = isSelected;

        if (texture.IsValid)
        {
            var iconSize = isGridMode
                ? new Vector2(32.0f, 32.0f)
                : new Vector2(18.0f, 18.0f);

            _icon = new UiImage(texture, iconSize)
            {
                Width = iconSize.X,
                Height = iconSize.Y,
                IsHitTestVisible = false
            };

            AddChild(_icon);
        }

        _label = new UiLabel(name)
        {
            FontSize = isGridMode ? 11.0f : 13.0f,
            Color = new UiColor(225, 225, 225, 255),
            IsHitTestVisible = false
        };

        AddChild(_label);

        Height = isGridMode ? 72.0f : 28.0f;
        Width = isGridMode ? 116.0f : null;

        HorizontalAlignment = UiHorizontalAlignment.Stretch;
        VerticalAlignment = UiVerticalAlignment.Top;

        Cursor = Engine.Input.Cursors.CursorShape.Hand;
    }

    public bool IsSelected { get; set; }

    public event Action? Clicked;

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        return new Vector2(
            Width ?? availableSize.X,
            Height ?? 28.0f);
    }

    protected override void ArrangeCore(UiRect finalRect)
    {
        if (_isGridMode)
        {
            if (_icon is not null)
            {
                _icon.Arrange(new UiRect(
                    finalRect.X + (finalRect.Width - 32.0f) / 2.0f,
                    finalRect.Y + 6.0f,
                    32.0f,
                    32.0f));
            }

            _label.Arrange(new UiRect(
                finalRect.X + 4.0f,
                finalRect.Y + 42.0f,
                MathF.Max(0.0f, finalRect.Width - 8.0f),
                MathF.Max(0.0f, finalRect.Height - 44.0f)));

            return;
        }

        if (_icon is not null)
        {
            _icon.Arrange(new UiRect(
                finalRect.X + 5.0f,
                finalRect.Y + (finalRect.Height - 18.0f) / 2.0f,
                18.0f,
                18.0f));
        }

        var textLeft = finalRect.X + (_icon is null ? 8.0f : 29.0f);

        _label.Arrange(new UiRect(
            textLeft,
            finalRect.Y,
            MathF.Max(0.0f, finalRect.Right - textLeft - 4.0f),
            finalRect.Height));
    }

    protected override void OnRender(UiRenderContext context)
    {
        if (IsSelected)
        {
            context.DrawRectangle(
                Bounds,
                new UiColor(55, 85, 120, 255),
                filled: true);
        }
        else if (IsHovered)
        {
            context.DrawRectangle(
                Bounds,
                context.Theme.ButtonHovered,
                filled: true);
        }
        else
        {
            context.DrawRectangle(
                Bounds,
                new UiColor(42, 45, 51, 255),
                filled: true);
        }
    }

    protected override void OnPointerDown(UiPointerEvent pointer)
    {
        if (!Enabled || pointer.Button != InputMouseButton.Left)
        {
            return;
        }

        IsPressed = true;
        pointer.Handled = true;
    }

    protected override void OnPointerUp(UiPointerEvent pointer)
    {
        if (pointer.Button != InputMouseButton.Left || !IsPressed)
        {
            return;
        }

        IsPressed = false;

        if (ContainsPoint(pointer.Position))
        {
            Clicked?.Invoke();
        }

        pointer.Handled = true;
    }
}
