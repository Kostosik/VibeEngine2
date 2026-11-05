using Engine.Core.Math;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

internal sealed class PrefabWorkspaceLayout : UiPanel
{
    private const float HierarchyWidth = 235.0f;
    private const float InspectorWidth = 320.0f;
    private const float Spacing = 6.0f;

    private readonly UiPanel _hierarchy;
    private readonly UiPanel _inspector;
    private readonly UiPanel _preview;

    public PrefabWorkspaceLayout(
        UiPanel hierarchy,
        UiPanel inspector,
        UiPanel preview)
    {
        ArgumentNullException.ThrowIfNull(hierarchy);
        ArgumentNullException.ThrowIfNull(inspector);
        ArgumentNullException.ThrowIfNull(preview);

        _hierarchy = hierarchy;
        _inspector = inspector;
        _preview = preview;

        HorizontalAlignment = UiHorizontalAlignment.Stretch;
        VerticalAlignment = UiVerticalAlignment.Stretch;

        AddChild(_hierarchy);
        AddChild(_inspector);
        AddChild(_preview);
    }

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        var contentWidth = MathF.Max(
            0.0f,
            availableSize.X - Padding.Left - Padding.Right);

        var contentHeight = MathF.Max(
            0.0f,
            availableSize.Y - Padding.Top - Padding.Bottom);

        var widths = CalculateWidths(contentWidth);

        _hierarchy.Measure(
            context,
            new Vector2(widths.Hierarchy, contentHeight));

        _inspector.Measure(
            context,
            new Vector2(widths.Inspector, contentHeight));

        _preview.Measure(
            context,
            new Vector2(widths.Preview, contentHeight));

        return new Vector2(
            contentWidth + Padding.Left + Padding.Right,
            contentHeight + Padding.Top + Padding.Bottom);
    }

    protected override void ArrangeCore(UiRect finalRect)
    {
        var content = finalRect.Deflate(Padding);
        var widths = CalculateWidths(content.Width);

        var x = content.X;

        _hierarchy.Arrange(
            new UiRect(
                x,
                content.Y,
                widths.Hierarchy,
                content.Height));

        x += widths.Hierarchy + Spacing;

        _inspector.Arrange(
            new UiRect(
                x,
                content.Y,
                widths.Inspector,
                content.Height));

        x += widths.Inspector + Spacing;

        _preview.Arrange(
            new UiRect(
                x,
                content.Y,
                widths.Preview,
                content.Height));
    }

    private static (
        float Hierarchy,
        float Inspector,
        float Preview) CalculateWidths(float availableWidth)
    {
        var hierarchy = MathF.Min(
            HierarchyWidth,
            availableWidth * 0.25f);

        var inspector = MathF.Min(
            InspectorWidth,
            availableWidth * 0.30f);

        var preview = MathF.Max(
            0.0f,
            availableWidth -
            hierarchy -
            inspector -
            Spacing * 2.0f);

        return (hierarchy, inspector, preview);
    }
}