using Engine.Core.Math;

namespace Engine.UI.Assets;

public sealed class UiAsset
{
    public const int CurrentVersion = 1;

    public UiAsset(
        string name,
        Vector2 canvasSize,
        IReadOnlyList<UiAssetElement> elements)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            name);

        ArgumentNullException.ThrowIfNull(
            elements);

        if (canvasSize.X <= 0.0f ||
            canvasSize.Y <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(canvasSize),
                "UI asset canvas size must be positive.");
        }

        if (elements.Count == 0)
        {
            throw new ArgumentException(
                "A UI asset must contain at least a root element.",
                nameof(elements));
        }

        ValidateElements(
            elements);

        Name = name;
        CanvasSize = canvasSize;
        Elements = elements.ToArray();
    }

    public string Name { get; }

    public Vector2 CanvasSize { get; }

    public IReadOnlyList<UiAssetElement> Elements { get; }

    public UiAssetElement Root =>
        Elements.First(
            static element =>
                element.Type ==
                UiAssetElementType.Root);

    private static void ValidateElements(
        IReadOnlyList<UiAssetElement> elements)
    {
        var ids =
            new HashSet<Guid>();

        UiAssetElement? root = null;

        foreach (var element in elements)
        {
            if (!ids.Add(element.Id))
            {
                throw new InvalidDataException(
                    $"UI asset contains duplicate element id '{element.Id}'.");
            }

            if (element.Type ==
                UiAssetElementType.Root)
            {
                if (root is not null)
                {
                    throw new InvalidDataException(
                        "UI asset contains multiple root elements.");
                }

                root = element;
            }
        }

        if (root is null)
        {
            throw new InvalidDataException(
                "UI asset does not contain a root element.");
        }

        foreach (var element in elements)
        {
            if (element.Type ==
                UiAssetElementType.Root)
            {
                continue;
            }

            if (!ids.Contains(
                    element.ParentId))
            {
                throw new InvalidDataException(
                    $"UI asset element '{element.Id}' references missing parent '{element.ParentId}'.");
            }
        }
    }
}