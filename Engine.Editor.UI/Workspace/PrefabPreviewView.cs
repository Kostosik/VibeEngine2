using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.ECS.Prefabs;
using Engine.Editor;
using Engine.Editor.Documents;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.Graphics2D.Sprites;
using Engine.Transform;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

public sealed class PrefabPreviewView : UiPanel
{
    private const float MinZoom = 0.1f;
    private const float MaxZoom = 64.0f;
    private const float ZoomStep = 1.15f;

    private readonly PrefabDocument _document;
    private readonly EditorContext? _editor;
    private readonly ViewportProjectTextureCache _textureCache;
    private readonly UiButton _fitButton;

    private readonly List<PreviewItem> _items = new();

    private PrefabAsset? _cachedAsset;
    private string? _transformWarning;

    private Vector2 _cameraCenter;
    private float _zoom = 1.0f;
    private bool _needsFit = true;

    private bool _isPanning;
    private Vector2 _lastPanPosition;

    public PrefabPreviewView(
        PrefabDocument document,
        EditorContext? editor,
        ITextureManager? textureManager)
    {
        ArgumentNullException.ThrowIfNull(document);

        _document = document;
        _editor = editor;

        _textureCache = new ViewportProjectTextureCache(
            textureManager);

        Background = new UiColor(20, 22, 26, 255);
        ClipChildren = true;

        HorizontalAlignment = UiHorizontalAlignment.Stretch;
        VerticalAlignment = UiVerticalAlignment.Stretch;

        _fitButton = new UiButton("Fit")
        {
            Width = 52.0f,
            Height = 27.0f,
            Margin = new UiThickness(8.0f),
            HorizontalAlignment = UiHorizontalAlignment.Right,
            VerticalAlignment = UiVerticalAlignment.Top,
            FontSize = 12.0f
        };

        _fitButton.Clicked += FitToContent;

        AddChild(_fitButton);
    }

    public PrefabEntityId SelectedEntityId { get; set; }

    protected override void OnRender(UiRenderContext context)
    {
        base.OnRender(context);

        if (Bounds.Width <= 0.0f || Bounds.Height <= 0.0f)
        {
            return;
        }

        EnsureSnapshot();
        EnsureTextureRoot();

        var content = GetContentBounds();

        if (_needsFit &&
            content.Width > 0.0f &&
            content.Height > 0.0f)
        {
            FitToContent();
        }

        context.PushClip(Bounds);

        try
        {
            context.DrawText(
                $"Prefab Preview • {_items.Count} Entities • {_zoom * 100.0f:0}%",
                new Vector2(Bounds.X + 8.0f, Bounds.Y + 8.0f),
                12.0f,
                new UiColor(215, 220, 230, 255));

            DrawGrid(context, content);

            foreach (var item in _items.OrderBy(
                         item => item.Sprite?.Layer ?? 0))
            {
                DrawItem(context, content, item);
            }

            if (_transformWarning is not null)
            {
                context.DrawText(
                    _transformWarning,
                    new Vector2(Bounds.X + 8.0f, Bounds.Bottom - 18.0f),
                    10.0f,
                    new UiColor(230, 155, 115, 255));
            }
            else if (_items.Count == 0)
            {
                context.DrawText(
                    "No renderable entities",
                    new Vector2(Bounds.X + 8.0f, Bounds.Y + 42.0f),
                    12.0f,
                    new UiColor(155, 160, 170, 255));
            }
        }
        finally
        {
            context.PopClip();
        }
    }

    private void EnsureSnapshot()
    {
        var asset = _document.Asset;

        if (ReferenceEquals(asset, _cachedAsset))
        {
            return;
        }

        _cachedAsset = asset;
        _items.Clear();
        _transformWarning = null;

        var resolved = new Dictionary<
            PrefabEntityId,
            FixedTransform2D>();

        foreach (var entity in asset.Entities)
        {
            if (!TryResolveWorldTransform(
                    asset,
                    entity,
                    resolved,
                    new HashSet<PrefabEntityId>(),
                    out var transform,
                    out var error))
            {
                _transformWarning ??= error;
                continue;
            }

            SpriteComponent? sprite = null;

            if (TryGetComponent<SpriteComponent>(
                    entity,
                    out var spriteComponent) &&
                !string.IsNullOrWhiteSpace(spriteComponent.TexturePath) &&
                float.IsFinite(spriteComponent.Width) &&
                float.IsFinite(spriteComponent.Height) &&
                spriteComponent.Width > 0.0f &&
                spriteComponent.Height > 0.0f)
            {
                sprite = spriteComponent;
            }

            _items.Add(
                new PreviewItem(
                    entity.Id,
                    transform,
                    sprite));
        }

        _needsFit = true;
    }

    private static bool TryResolveWorldTransform(
        PrefabAsset asset,
        PrefabEntity entity,
        Dictionary<PrefabEntityId, FixedTransform2D> resolved,
        HashSet<PrefabEntityId> resolving,
        out FixedTransform2D transform,
        out string? error)
    {
        transform = default;
        error = null;

        if (resolved.TryGetValue(entity.Id, out transform))
        {
            return true;
        }

        if (!resolving.Add(entity.Id))
        {
            error = "Preview: transform hierarchy contains a cycle.";
            return false;
        }

        try
        {
            if (TryGetComponent<TransformParent2D>(
                    entity,
                    out var parentComponent))
            {
                if (!TryGetComponent<LocalTransform2D>(
                        entity,
                        out var localTransform))
                {
                    error =
                        $"Preview: Entity {entity.Id.Value} has a parent but no LocalTransform2D.";

                    return false;
                }

                var parentReference = parentComponent.Parent;

                if (!parentReference.IsValid ||
                    parentReference.Generation != 0)
                {
                    error =
                        $"Preview: Entity {entity.Id.Value} has an invalid parent reference.";

                    return false;
                }

                var parentId = new PrefabEntityId(
                    parentReference.Index);

                var parent = asset.Entities.FirstOrDefault(
                    item => item.Id == parentId);

                if (parent is null)
                {
                    error =
                        $"Preview: parent Entity {parentId.Value} was not found.";

                    return false;
                }

                if (!TryResolveWorldTransform(
                        asset,
                        parent,
                        resolved,
                        resolving,
                        out var parentTransform,
                        out error))
                {
                    return false;
                }

                transform = FixedTransformOperations2D.Combine(
                    parentTransform,
                    localTransform.Value);
            }
            else if (TryGetComponent<LocalTransform2D>(
                         entity,
                         out var localTransform))
            {
                transform = localTransform.Value;
            }
            else if (TryGetComponent<WorldTransform2D>(
                         entity,
                         out var worldTransform))
            {
                transform = new FixedTransform2D(
                    worldTransform.Position)
                {
                    Rotation = worldTransform.Rotation,
                    Scale = worldTransform.Scale
                };
            }
            else
            {
                return false;
            }

            resolved[entity.Id] = transform;
            return true;
        }
        finally
        {
            resolving.Remove(entity.Id);
        }
    }

    private static bool TryGetComponent<T>(
        PrefabEntity entity,
        out T value)
        where T : struct
    {
        var component = entity.Components
            .OfType<PrefabComponent<T>>()
            .FirstOrDefault();

        if (component is null)
        {
            value = default;
            return false;
        }

        value = component.Component;
        return true;
    }

    private void EnsureTextureRoot()
    {
        var assetsRoot =
            _editor?.ActiveGameProject?.AssetsDirectoryPath;

        if (string.IsNullOrWhiteSpace(assetsRoot))
        {
            if (_textureCache.HasAssetsRoot)
            {
                _textureCache.ReleaseResources();
            }

            return;
        }

        _textureCache.SetAssetsRoot(assetsRoot);
    }

    private void DrawItem(
        UiRenderContext context,
        UiRect viewport,
        PreviewItem item)
    {
        var transform = item.Transform;

        var worldPosition = new Vector2(
            transform.Position.X.ToFloat(),
            transform.Position.Y.ToFloat());

        var center = WorldToScreen(
            worldPosition,
            viewport);

        if (item.Sprite is not SpriteComponent sprite)
        {
            DrawEntityMarker(
                context,
                center,
                item.Id == SelectedEntityId);

            return;
        }

        var scaleX = transform.Scale.X.ToFloat();
        var scaleY = transform.Scale.Y.ToFloat();

        var width =
            sprite.Width * MathF.Abs(scaleX) * _zoom;

        var height =
            sprite.Height * MathF.Abs(scaleY) * _zoom;

        if (!float.IsFinite(width) ||
            !float.IsFinite(height) ||
            width < 1.0f ||
            height < 1.0f)
        {
            return;
        }

        var destination = new UiRect(
            center.X - width * 0.5f,
            center.Y - height * 0.5f,
            width,
            height);

        var rotationDegrees =
            -transform.Rotation.ToFloat() *
            (180.0f / MathF.PI);

        context.PushRotation(
            center,
            rotationDegrees);

        try
        {
            var hasTexture = _textureCache.TryLoad(
                sprite.TexturePath,
                out var texture);

            if (hasTexture)
            {
                var uv = new Rectangle(
                    scaleX < 0.0f ? 1.0f : 0.0f,
                    scaleY < 0.0f ? 1.0f : 0.0f,
                    scaleX < 0.0f ? -1.0f : 1.0f,
                    scaleY < 0.0f ? -1.0f : 1.0f);

                context.DrawTexture(
                    texture,
                    destination,
                    uv,
                    layer: sprite.Layer);
            }
            else
            {
                context.DrawRectangle(
                    destination,
                    new UiColor(190, 75, 75, 220),
                    filled: false,
                    layer: sprite.Layer);
            }

            if (item.Id == SelectedEntityId)
            {
                context.DrawRectangle(
                    new UiRect(
                        destination.X - 2.0f,
                        destination.Y - 2.0f,
                        destination.Width + 4.0f,
                        destination.Height + 4.0f),
                    new UiColor(255, 220, 80, 255),
                    filled: false,
                    layer: sprite.Layer + 1);
            }
        }
        finally
        {
            context.PopRotation();
        }
    }

    private static void DrawEntityMarker(
        UiRenderContext context,
        Vector2 center,
        bool selected)
    {
        var color = selected
            ? new UiColor(255, 220, 80, 255)
            : new UiColor(90, 160, 230, 255);

        context.DrawRectangle(
            new UiRect(
                center.X - 5.0f,
                center.Y - 5.0f,
                10.0f,
                10.0f),
            color,
            filled: true);

        context.DrawRectangle(
            new UiRect(
                center.X - 8.0f,
                center.Y - 8.0f,
                16.0f,
                16.0f),
            color,
            filled: false);
    }

    private void DrawGrid(
        UiRenderContext context,
        UiRect viewport)
    {
        if (viewport.Width <= 0.0f ||
            viewport.Height <= 0.0f ||
            _zoom <= 0.0f)
        {
            return;
        }

        var topLeft = ScreenToWorld(
            new Vector2(viewport.X, viewport.Y),
            viewport);

        var bottomRight = ScreenToWorld(
            new Vector2(viewport.Right, viewport.Bottom),
            viewport);

        var spacing = CalculateGridSpacing(_zoom);

        var startX = MathF.Floor(
            MathF.Min(topLeft.X, bottomRight.X) / spacing) * spacing;

        var endX = MathF.Max(topLeft.X, bottomRight.X);

        var startY = MathF.Floor(
            MathF.Min(topLeft.Y, bottomRight.Y) / spacing) * spacing;

        var endY = MathF.Max(topLeft.Y, bottomRight.Y);

        for (var x = startX; x <= endX; x += spacing)
        {
            var screen = WorldToScreen(
                new Vector2(x, 0.0f),
                viewport);

            var isAxis = MathF.Abs(x) < 0.0001f;

            context.DrawRectangle(
                new UiRect(
                    screen.X - (isAxis ? 1.0f : 0.5f),
                    viewport.Y,
                    isAxis ? 2.0f : 1.0f,
                    viewport.Height),
                isAxis
                    ? new UiColor(75, 80, 90, 255)
                    : new UiColor(38, 42, 48, 255));
        }

        for (var y = startY; y <= endY; y += spacing)
        {
            var screen = WorldToScreen(
                new Vector2(0.0f, y),
                viewport);

            var isAxis = MathF.Abs(y) < 0.0001f;

            context.DrawRectangle(
                new UiRect(
                    viewport.X,
                    screen.Y - (isAxis ? 1.0f : 0.5f),
                    viewport.Width,
                    isAxis ? 2.0f : 1.0f),
                isAxis
                    ? new UiColor(75, 80, 90, 255)
                    : new UiColor(38, 42, 48, 255));
        }
    }

    private static float CalculateGridSpacing(float zoom)
    {
        var target = 32.0f / zoom;

        var exponent = MathF.Floor(
            MathF.Log10(target));

        var magnitude = MathF.Pow(
            10.0f,
            exponent);

        var normalized = target / magnitude;

        var step = normalized <= 1.0f
            ? 1.0f
            : normalized <= 2.0f
                ? 2.0f
                : normalized <= 5.0f
                    ? 5.0f
                    : 10.0f;

        return step * magnitude;
    }

    private void FitToContent()
    {
        if (_items.Count == 0)
        {
            _cameraCenter = Vector2.Zero;
            _zoom = 1.0f;
            _needsFit = false;
            return;
        }

        var minX = float.PositiveInfinity;
        var minY = float.PositiveInfinity;
        var maxX = float.NegativeInfinity;
        var maxY = float.NegativeInfinity;

        foreach (var item in _items)
        {
            var transform = item.Transform;

            var x = transform.Position.X.ToFloat();
            var y = transform.Position.Y.ToFloat();

            var halfWidth = 5.0f;
            var halfHeight = 5.0f;

            if (item.Sprite is SpriteComponent sprite)
            {
                var width =
                    sprite.Width *
                    MathF.Abs(transform.Scale.X.ToFloat());

                var height =
                    sprite.Height *
                    MathF.Abs(transform.Scale.Y.ToFloat());

                var rotation = transform.Rotation.ToFloat();

                var cosine = MathF.Abs(MathF.Cos(rotation));
                var sine = MathF.Abs(MathF.Sin(rotation));

                halfWidth =
                    (cosine * width + sine * height) * 0.5f;

                halfHeight =
                    (sine * width + cosine * height) * 0.5f;
            }

            minX = MathF.Min(minX, x - halfWidth);
            maxX = MathF.Max(maxX, x + halfWidth);
            minY = MathF.Min(minY, y - halfHeight);
            maxY = MathF.Max(maxY, y + halfHeight);
        }

        _cameraCenter = new Vector2(
            (minX + maxX) * 0.5f,
            (minY + maxY) * 0.5f);

        var viewport = GetContentBounds();

        var widthWorld = MathF.Max(1.0f, maxX - minX);
        var heightWorld = MathF.Max(1.0f, maxY - minY);

        var availableWidth = MathF.Max(
            1.0f,
            viewport.Width - 40.0f);

        var availableHeight = MathF.Max(
            1.0f,
            viewport.Height - 40.0f);

        _zoom = Math.Clamp(
            MathF.Min(
                availableWidth / widthWorld,
                availableHeight / heightWorld),
            MinZoom,
            MaxZoom);

        _needsFit = false;
    }

    private Vector2 WorldToScreen(
        Vector2 world,
        UiRect viewport)
    {
        var centerX = viewport.X + viewport.Width * 0.5f;
        var centerY = viewport.Y + viewport.Height * 0.5f;

        return new Vector2(
            centerX + (world.X - _cameraCenter.X) * _zoom,
            centerY - (world.Y - _cameraCenter.Y) * _zoom);
    }

    private Vector2 ScreenToWorld(
        Vector2 screen,
        UiRect viewport)
    {
        var centerX = viewport.X + viewport.Width * 0.5f;
        var centerY = viewport.Y + viewport.Height * 0.5f;

        return new Vector2(
            _cameraCenter.X + (screen.X - centerX) / _zoom,
            _cameraCenter.Y - (screen.Y - centerY) / _zoom);
    }

    private UiRect GetContentBounds()
    {
        return new UiRect(
            Bounds.X + 8.0f,
            Bounds.Y + 34.0f,
            MathF.Max(0.0f, Bounds.Width - 16.0f),
            MathF.Max(0.0f, Bounds.Height - 44.0f));
    }

    protected override void OnPointerWheel(UiPointerEvent pointer)
    {
        if (!ContainsPointer(pointer.Position) ||
            MathF.Abs(pointer.ScrollDelta) < 0.0001f)
        {
            return;
        }

        var viewport = GetContentBounds();

        var newZoom = Math.Clamp(
            _zoom * MathF.Pow(ZoomStep, pointer.ScrollDelta),
            MinZoom,
            MaxZoom);

        if (MathF.Abs(newZoom - _zoom) < 0.000001f)
        {
            return;
        }

        var worldBefore = ScreenToWorld(
            pointer.Position,
            viewport);

        _zoom = newZoom;

        var viewportCenter = new Vector2(
            viewport.X + viewport.Width * 0.5f,
            viewport.Y + viewport.Height * 0.5f);

        _cameraCenter = new Vector2(
            worldBefore.X -
                (pointer.Position.X - viewportCenter.X) / _zoom,

            worldBefore.Y +
                (pointer.Position.Y - viewportCenter.Y) / _zoom);

        pointer.Handled = true;
    }

    protected override void OnPointerDown(UiPointerEvent pointer)
    {
        if (pointer.Button != Engine.Input.InputMouseButton.Middle ||
            !ContainsPointer(pointer.Position))
        {
            return;
        }

        _isPanning = true;
        _lastPanPosition = pointer.Position;

        pointer.RequestCapture();
        pointer.Handled = true;
    }

    protected override void OnPointerMove(UiPointerEvent pointer)
    {
        base.OnPointerMove(pointer);

        if (!_isPanning ||
            pointer.Button != Engine.Input.InputMouseButton.Middle)
        {
            return;
        }

        var delta = pointer.Position - _lastPanPosition;

        _cameraCenter = new Vector2(
            _cameraCenter.X - delta.X / _zoom,
            _cameraCenter.Y + delta.Y / _zoom);

        _lastPanPosition = pointer.Position;
        pointer.Handled = true;
    }

    protected override void OnPointerUp(UiPointerEvent pointer)
    {
        base.OnPointerUp(pointer);

        if (!_isPanning ||
            pointer.Button != Engine.Input.InputMouseButton.Middle)
        {
            return;
        }

        _isPanning = false;
        pointer.Handled = true;
    }

    private bool ContainsPointer(Vector2 position)
    {
        return position.X >= Bounds.X &&
               position.Y >= Bounds.Y &&
               position.X <= Bounds.Right &&
               position.Y <= Bounds.Bottom;
    }

    public void ReleaseResources()
    {
        _textureCache.ReleaseResources();
        _cachedAsset = null;
        _needsFit = true;
    }

    private readonly record struct PreviewItem(
        PrefabEntityId Id,
        FixedTransform2D Transform,
        SpriteComponent? Sprite);
}