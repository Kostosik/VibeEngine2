using Engine.Core.Math;

namespace Engine.UI.Assets;

public sealed class UiAsset
{
    public const int CurrentVersion = 3;

    public UiAsset(
        string name,
        Vector2 canvasSize,
        IReadOnlyList<UiAssetElement> elements)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            name);

        ArgumentNullException.ThrowIfNull(
            elements);

        if (!float.IsFinite(canvasSize.X) ||
            !float.IsFinite(canvasSize.Y) ||
            canvasSize.X <= 0.0f ||
            canvasSize.Y <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(canvasSize),
                "UI asset canvas size must be finite and positive.");
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
        var byId =
            new Dictionary<Guid, UiAssetElement>();

        UiAssetElement? root = null;

        foreach (var element in elements)
        {
            ArgumentNullException.ThrowIfNull(
                element);

            if (!byId.TryAdd(
                    element.Id,
                    element))
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

            if (!byId.ContainsKey(
                    element.ParentId))
            {
                throw new InvalidDataException(
                    $"UI asset element '{element.Id}' references missing parent '{element.ParentId}'.");
            }
        }

        foreach (var element in elements)
        {
            if (element.Type ==
                UiAssetElementType.Root)
            {
                continue;
            }

            var visited =
                new HashSet<Guid>();

            var current =
                element;

            while (current.Type !=
                   UiAssetElementType.Root)
            {
                if (!visited.Add(
                        current.Id))
                {
                    throw new InvalidDataException(
                        $"UI asset contains a hierarchy cycle involving element '{current.Id}'.");
                }

                current =
                    byId[current.ParentId];
            }

            if (!ReferenceEquals(
                    current,
                    root))
            {
                throw new InvalidDataException(
                    $"UI asset element '{element.Id}' is not connected to the root element.");
            }
        }
    }
}