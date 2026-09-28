using Engine.UI.Core;

namespace Engine.UI.Assets;

public sealed class UiAssetInstance
{
    private readonly IReadOnlyDictionary<Guid, UiWidget> _elements;

    internal UiAssetInstance(
        UiRoot root,
        IReadOnlyDictionary<Guid, UiWidget> elements)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(elements);

        Root = root;
        _elements = elements;
    }

    public UiRoot Root { get; }

    public bool TryGet(
        Guid elementId,
        out UiWidget? widget)
    {
        return _elements.TryGetValue(
            elementId,
            out widget);
    }

    public UiWidget Get(
        Guid elementId)
    {
        if (!_elements.TryGetValue(
                elementId,
                out var widget))
        {
            throw new KeyNotFoundException(
                $"Runtime UI element '{elementId}' was not found.");
        }

        return widget;
    }
}