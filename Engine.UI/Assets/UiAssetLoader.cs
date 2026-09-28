using Engine.Graphics.Resources;
using Engine.UI.Actions;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.UI.Assets;

public sealed class UiAssetLoader
{
    private readonly ITextureResourceManager? _textures;
    private readonly UiActionRegistry? _actions;
    public UiAssetLoader(
        ITextureResourceManager? textures = null,
        UiActionRegistry? actions = null)
    {
        _textures = textures;
        _actions = actions;
    }

    public UiAssetInstance Load(
        UiAsset asset)
    {
        ArgumentNullException.ThrowIfNull(
            asset);

        var root =
            new UiRoot
            {
                Width =
                    asset.CanvasSize.X,

                Height =
                    asset.CanvasSize.Y
            };

        var runtimeElements =
            new Dictionary<Guid, UiWidget>();

        runtimeElements.Add(
            asset.Root.Id,
            root);

        foreach (var element in asset.Elements)
        {
            if (element.Type ==
                UiAssetElementType.Root)
            {
                continue;
            }

            if (!runtimeElements.TryAdd(
                    element.Id,
                    CreateWidget(element)))
            {
                throw new InvalidDataException(
                    $"UI asset contains duplicate element id '{element.Id}'.");
            }
        }

        foreach (var element in asset.Elements)
        {
            if (element.Type ==
                UiAssetElementType.Root)
            {
                continue;
            }

            if (!runtimeElements.TryGetValue(
                    element.ParentId,
                    out var parent))
            {
                throw new InvalidDataException(
                    $"UI element '{element.Id}' references parent '{element.ParentId}', which does not exist.");
            }

            var widget =
                runtimeElements[element.Id];

            ApplyLayout(
                widget,
                element);
            BindAction(
    widget,
    element);

            AddChild(
                parent,
                widget);
        }

        return new UiAssetInstance(
            root,
            runtimeElements);
    }

    private UiWidget CreateWidget(
        UiAssetElement element)
    {
        return element.Type switch
        {
            UiAssetElementType.Panel =>
                new UiPanel(),

            UiAssetElementType.Label =>
                new UiLabel(
                    element.Text),

            UiAssetElementType.Button =>
                new UiButton(
                    element.Text),

            UiAssetElementType.Image =>
                CreateImage(element),

            UiAssetElementType.TextBox =>
                new UiTextBox(
                    element.Text),

            UiAssetElementType.Toggle =>
                new UiToggle(
                    element.Text),

            UiAssetElementType.Dropdown =>
                new UiDropdown(),

            UiAssetElementType.ScrollView =>
                new UiScrollView(),

            UiAssetElementType.Root =>
                throw new InvalidDataException(
                    "The root UI element cannot be created as a child."),

            _ =>
                throw new InvalidDataException(
                    $"Unsupported UI asset element type '{element.Type}'.")
        };
    }

    private UiImage CreateImage(
        UiAssetElement element)
    {
        if (!element.Texture.HasValue)
        {
            return new UiImage(
                TextureHandle.Invalid,
                element.Layout.Size);
        }

        if (_textures is null)
        {
            throw new InvalidOperationException(
                $"UI image '{element.Id}' requires a texture resource manager.");
        }

        var texture =
            _textures.Load(
                element.Texture.Value);

        return new UiImage(
            texture,
            element.Layout.Size);
    }

    private static void ApplyLayout(
        UiWidget widget,
        UiAssetElement element)
    {
        widget.LayoutMode =
            UiLayoutMode.Anchor;

        widget.Anchor =
            element.Layout.Anchor;

        widget.Offset =
            element.Layout.Offset;

        widget.Width =
            element.Layout.Size.X;

        widget.Height =
            element.Layout.Size.Y;
    }

    private static void AddChild(
        UiWidget parent,
        UiWidget child)
    {
        if (parent is UiScrollView scrollView)
        {
            if (scrollView.Content is not null)
            {
                throw new InvalidDataException(
                    "A ScrollView can have only one direct content element.");
            }

            scrollView.SetContent(
                child);

            return;
        }

        if (parent is UiContainer container)
        {
            container.AddChild(
                child);

            return;
        }

        throw new InvalidDataException(
            $"UI element '{parent.GetType().Name}' cannot contain children.");
    }

    private void BindAction(
    UiWidget widget,
    UiAssetElement element)
    {
        if (string.IsNullOrWhiteSpace(
                element.Action))
        {
            return;
        }

        if (_actions is null)
        {
            throw new InvalidOperationException(
                $"UI element '{element.Id}' requires action registry for action '{element.Action}'.");
        }

        if (widget is UiButton button)
        {
            var action =
                element.Action;

            button.Clicked +=
                () => _actions.Invoke(action);

            return;
        }

        throw new InvalidDataException(
            $"UI action '{element.Action}' is not supported by element type '{element.Type}'.");
    }
}