using Engine.Core.Math;
using Engine.UI.Layout;

namespace Engine.UI.Assets;

public readonly record struct UiAssetLayout
{
    public UiAssetLayout(
        UiAnchor anchor,
        Vector2 offset,
        Vector2 size)
    {
        if (!float.IsFinite(offset.X) ||
            !float.IsFinite(offset.Y))
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                "UI asset offset must contain finite values.");
        }

        if (!float.IsFinite(size.X) ||
            !float.IsFinite(size.Y) ||
            size.X < 0.0f ||
            size.Y < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                "UI asset size must contain finite non-negative values.");
        }

        Anchor = anchor;
        Offset = offset;
        Size = size;
    }

    public UiAnchor Anchor { get; }

    public Vector2 Offset { get; }

    public Vector2 Size { get; }
}