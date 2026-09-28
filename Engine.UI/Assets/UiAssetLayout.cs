using Engine.Core.Math;
using Engine.UI.Layout;

namespace Engine.UI.Assets;

public readonly record struct UiAssetLayout(
    UiAnchor Anchor,
    Vector2 Offset,
    Vector2 Size);