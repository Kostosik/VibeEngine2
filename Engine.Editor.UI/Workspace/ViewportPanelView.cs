using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Editor.Commands;
using Engine.Editor.Viewport.Gizmos;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.Graphics2D.Sprites;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;
using Engine.Worlds.Spatial;

namespace Engine.Editor.UI.Workspace;

public sealed class ViewportPanelView :
    UiPanel
{
    private enum TransformTool
    {
        Move,
        Rotate,
        Scale
    }

    private readonly ITextureManager? _projectTextureManager;

    private readonly ImageTextureLoader _projectImageLoader = new();

    private readonly Dictionary<string, TextureHandle> _projectTextures =
        new(
            OperatingSystem.IsWindows()
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal);

    private readonly HashSet<string> _unavailableProjectTextures =
        new(
            OperatingSystem.IsWindows()
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal);

    private string? _projectAssetsRoot;

    private TransformTool _activeTool = TransformTool.Move;
    private bool _translationUsesLocalTransform;
    private LocalTransform2D _translationStartLocalTransform;
    private readonly UiPanel _toolBar;
    private readonly UiStackPanel _toolButtons;

    private readonly UiButton _moveToolButton;
    private readonly UiButton _rotateToolButton;
    private readonly UiButton _scaleToolButton;

    private int _debugRenderCounter;

    private readonly EditorTranslationGizmo _translationGizmo =
    new();

    private bool _isRotating;
    private bool _isScaling;

    private EntityId _transformEntity;
    private LocalTransform2D _transformStartValue;
    private EditorGizmoAxis _transformAxis;

    private Vector2 _transformStartMousePosition;
    private Vector2 _transformCenterScreen;

    private float _rotationStartMouseAngle;

    private bool _isTranslating;
    private Editor.Documents.EditorDocument? _interactionDocument;
    private EditorGizmoAxis _translationAxis;
    private EntityId _translationEntity;
    private Vector2 _translationStartWorld;
    private WorldPosition _translationStartPosition;
    private bool _translationUsesWorldTransform;
    private WorldTransform2D _translationStartWorldTransform;
    private const float GridMinScreenSpacing = 12.0f;
    private const float GridMaxScreenSpacing = 48.0f;
    private const float GridLineThickness = 1.0f;
    private const float AxisLineThickness = 2.0f;

    private const float MinZoom = 0.1f;
    private const float MaxZoom = 64.0f;
    private const float ZoomStep = 1.15f;
    private bool _isPanning;
    private Vector2 _lastPanPosition;
    private const float EntityMarkerWorldSize = 10.0f;
    private const float SelectionOutlineWorldSize = 10.5f;

    private Vector2 _translationStartMouseWorld;

    public ViewportPanelView(
        EditorContext editor,
        ITextureManager? projectTextureManager = null)
    {
        ArgumentNullException.ThrowIfNull(editor);

        Editor = editor;
        _projectTextureManager = projectTextureManager;

        Background =
            new UiColor(
                18,
                18,
                18,
                255);

        _toolButtons = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 4.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        _moveToolButton = CreateToolButton("Move");
        _rotateToolButton = CreateToolButton("Rotate");
        _scaleToolButton = CreateToolButton("Scale");

        _moveToolButton.Clicked +=
            () => SetActiveTool(TransformTool.Move);

        _rotateToolButton.Clicked +=
            () => SetActiveTool(TransformTool.Rotate);

        _scaleToolButton.Clicked +=
            () => SetActiveTool(TransformTool.Scale);

        _toolButtons.AddChild(_moveToolButton);
        _toolButtons.AddChild(_rotateToolButton);
        _toolButtons.AddChild(_scaleToolButton);

        _toolBar = new UiPanel
        {
            Width = 274.0f,
            Height = 38.0f,
            Margin = new UiThickness(8.0f),
            Padding = new UiThickness(4.0f),
            Background = new UiColor(32, 32, 32, 230),
            HorizontalAlignment = UiHorizontalAlignment.Left,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        _toolBar.AddChild(_toolButtons);
        AddChild(_toolBar);

        RefreshToolButtons();
    }

    private static UiButton CreateToolButton(string text)
    {
        return new UiButton(text)
        {
            Width = 86.0f,
            Height = 30.0f,
            Focusable = false
        };
    }

    private void SetActiveTool(TransformTool tool)
    {
        if (_activeTool == tool)
        {
            return;
        }

        _activeTool = tool;
        RefreshToolButtons();
    }

    private void RefreshToolButtons()
    {
        _moveToolButton.Text =
            _activeTool == TransformTool.Move
                ? "> Move"
                : "Move";

        _rotateToolButton.Text =
            _activeTool == TransformTool.Rotate
                ? "> Rotate"
                : "Rotate";

        _scaleToolButton.Text =
            _activeTool == TransformTool.Scale
                ? "> Scale"
                : "Scale";
    }

    protected override void OnPointerWheel(
    UiPointerEvent pointer)
    {
        base.OnPointerWheel(
            pointer);

        var document =
            Editor.ActiveDocument;

        if (document is null ||
            MathF.Abs(pointer.ScrollDelta) < 0.0001f)
        {
            return;
        }

        var localPosition =
            new Vector2(
                pointer.Position.X -
                Bounds.X,

                pointer.Position.Y -
                Bounds.Y);

        if (!ContainsLocalPosition(
                localPosition))
        {
            return;
        }

        var worldBefore =
            document.Viewport.Transform.ScreenToWorld(
                localPosition);

        var factor =
            MathF.Pow(
                ZoomStep,
                pointer.ScrollDelta);

        var zoom =
            Math.Clamp(
                document.Viewport.State.Zoom * factor,
                MinZoom,
                MaxZoom);

        if (MathF.Abs(
                zoom -
                document.Viewport.State.Zoom) <
            0.000001f)
        {
            return;
        }

        document.Viewport.State.SetZoom(
            zoom);

        var worldAfter =
            document.Viewport.Transform.ScreenToWorld(
                localPosition);

        document.Viewport.State.Pan(
            worldBefore -
            worldAfter);

        pointer.Handled = true;
    }

    public EditorContext Editor { get; }

    public void Refresh()
    {
        var document =
            Editor.ActiveDocument;

        if (document is null)
        {
            return;
        }

        if (Bounds.Width <= 0.0f ||
            Bounds.Height <= 0.0f)
        {
            return;
        }

        document.Viewport.State.SetSize(
            new Vector2(
                Bounds.Width,
                Bounds.Height));
    }

    protected override void OnRender(
     UiRenderContext context)
    {
        base.OnRender(
            context);

        var document =
            Editor.ActiveDocument;


        if (document is null)
        {
            context.DrawText(
                "No document",
                new Vector2(
                    Bounds.X + 12.0f,
                    Bounds.Y + 12.0f),
                13.0f,
                new UiColor(
                    150,
                    150,
                    150,
                    255));

            return;
        }

        if (Bounds.Width <= 0.0f ||
            Bounds.Height <= 0.0f)
        {
            return;
        }

        document.Viewport.State.SetSize(
            new Vector2(
                Bounds.Width,
                Bounds.Height));

        context.PushClip(
            Bounds);

        try
        {
            DrawGrid(context, document);
            DrawSprites(context, document);
            DrawEntities(context, document);
            DrawSelection(context, document);

            DrawActiveGizmo(context, document);

            DrawInfo(
                context,
                document);
        }
        finally
        {
            context.PopClip();
        }
    }

    private void DrawActiveGizmo(
    UiRenderContext context,
    Engine.Editor.Documents.EditorDocument document)
    {
        switch (_activeTool)
        {
            case TransformTool.Move:
                DrawTranslationGizmo(context, document);
                break;

            case TransformTool.Rotate:
                DrawRotationGizmo(context, document);
                break;

            case TransformTool.Scale:
                DrawScaleGizmo(context, document);
                break;
        }
    }

    private void DrawSprites(
    UiRenderContext context,
    Engine.Editor.Documents.EditorDocument document)
    {
        var project = Editor.ActiveGameProject;

        if (project is null || _projectTextureManager is null)
        {
            if (_projectAssetsRoot is not null)
            {
                ReleaseProjectTextures();
                _projectAssetsRoot = null;
            }

            return;
        }

        var assetsRoot = project.AssetsDirectoryPath;

        EnsureProjectAssetsRoot(assetsRoot);

        var zoom = document.Viewport.State.Zoom;

        var sprites = document.World.EcsWorld
            .Query<SpriteComponent, WorldTransform2D>();

        foreach (var item in sprites)
        {
            var sprite = item.First;
            var transform = item.Second;

            if (string.IsNullOrWhiteSpace(sprite.TexturePath))
            {
                continue;
            }

            if (!float.IsFinite(sprite.Width) ||
                !float.IsFinite(sprite.Height) ||
                sprite.Width <= 0.0f ||
                sprite.Height <= 0.0f)
            {
                continue;
            }

            var scaleX = MathF.Abs(transform.Scale.X.ToFloat());
            var scaleY = MathF.Abs(transform.Scale.Y.ToFloat());

            var width = sprite.Width * scaleX;
            var height = sprite.Height * scaleY;

            if (!float.IsFinite(width) ||
                !float.IsFinite(height) ||
                width <= 0.0f ||
                height <= 0.0f)
            {
                continue;
            }

            var screenWidth = width * zoom;
            var screenHeight = height * zoom;

            var localScreen = document.Viewport.Transform.WorldToScreen(
                new Vector2(
                    transform.Position.X.ToFloat(),
                    transform.Position.Y.ToFloat()));

            var center = new Vector2(
                Bounds.X + localScreen.X,
                Bounds.Y + localScreen.Y);

            var destination = new UiRect(
                center.X - screenWidth * 0.5f,
                center.Y - screenHeight * 0.5f,
                screenWidth,
                screenHeight);

            if (!TryLoadProjectTexture(
                    sprite.TexturePath,
                    assetsRoot,
                    out var texture))
            {
                DrawSpriteTexturePlaceholder(
                    context,
                    destination,
                    sprite.Layer);

                continue;
            }

            context.DrawTexture(
                texture,
                destination,
                new Rectangle(0.0f, 0.0f, 1.0f, 1.0f),
                layer: sprite.Layer);
        }
    }

    private bool TryLoadProjectTexture(
        string texturePath,
        string assetsRoot,
        out TextureHandle texture)
    {
        texture = default;

        if (_unavailableProjectTextures.Contains(texturePath))
        {
            return false;
        }

        string? fullPath = null;

        try
        {
            fullPath = ResolveProjectTexturePath(
                assetsRoot,
                texturePath);

            if (_projectTextures.TryGetValue(
                    fullPath,
                    out texture) &&
                _projectTextureManager!.Exists(texture))
            {
                return true;
            }

            var imageData = _projectImageLoader.Load(
                File.ReadAllBytes(fullPath));

            texture = _projectTextureManager!.Create(imageData);

            if (!texture.IsValid ||
                !_projectTextureManager.Exists(texture))
            {
                throw new InvalidOperationException(
                    "Graphics backend failed to create the sprite texture.");
            }

            _projectTextures[fullPath] = texture;

            return true;
        }
        catch (Exception exception)
        {
            _unavailableProjectTextures.Add(texturePath);

            System.Diagnostics.Debug.WriteLine(
                $"[Viewport] Failed to load sprite texture " +
                $"'{texturePath}': {exception}");

            texture = default;

            return false;
        }
    }

    private static string ResolveProjectTexturePath(
        string assetsRoot,
        string texturePath)
    {
        if (Path.IsPathRooted(texturePath))
        {
            throw new InvalidDataException(
                "Sprite texture paths must be relative to the project's Assets directory.");
        }

        var fullRoot = Path.GetFullPath(assetsRoot);

        var relativeAssetPath = new AssetPath(texturePath).Value;

        if (string.IsNullOrWhiteSpace(relativeAssetPath))
        {
            throw new InvalidDataException(
                "Sprite texture path is empty.");
        }

        var fullPath = Path.GetFullPath(
            Path.Combine(fullRoot, relativeAssetPath));

        var relativePath = Path.GetRelativePath(
            fullRoot,
            fullPath);

        if (Path.IsPathRooted(relativePath) ||
            relativePath.Equals("..", StringComparison.Ordinal) ||
            relativePath.StartsWith(
                $"..{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal) ||
            relativePath.StartsWith(
                $"..{Path.AltDirectorySeparatorChar}",
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "Sprite texture path cannot point outside the project's Assets directory.");
        }

        return fullPath;
    }

    private void EnsureProjectAssetsRoot(string rootPath)
    {
        var fullRoot = Path.GetFullPath(rootPath);

        if (_projectAssetsRoot is not null &&
            string.Equals(
                _projectAssetsRoot,
                fullRoot,
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
        {
            return;
        }

        ReleaseProjectTextures();

        _projectAssetsRoot = fullRoot;
    }

    private void ReleaseProjectTextures()
    {
        if (_projectTextureManager is not null)
        {
            foreach (var texture in _projectTextures.Values)
            {
                if (_projectTextureManager.Exists(texture))
                {
                    _projectTextureManager.Destroy(texture);
                }
            }
        }

        _projectTextures.Clear();
        _unavailableProjectTextures.Clear();
    }

    public void ReleaseResources()
    {
        ReleaseProjectTextures();
        _projectAssetsRoot = null;
    }

    private static void DrawSpriteTexturePlaceholder(
        UiRenderContext context,
        UiRect rect,
        int layer)
    {
        if (rect.Width < 2.0f || rect.Height < 2.0f)
        {
            return;
        }

        context.DrawRectangle(
            rect,
            new UiColor(190, 65, 65, 220),
            filled: false,
            layer: layer);

        if (rect.Width >= 80.0f && rect.Height >= 20.0f)
        {
            context.DrawText(
                "Texture unavailable",
                new Vector2(rect.X + 4.0f, rect.Y + 4.0f),
                10.0f,
                new UiColor(235, 150, 150, 255),
                layer: layer);
        }
    }

    private bool TryGetSelectedScreenPosition(
    Engine.Editor.Documents.EditorDocument document,
    out Vector2 screen)
    {
        screen = default;

        if (!document.Inspector.TryGetSelectedEntity(
                document.EntitySelection,
                out var entity) ||
            !document.World.SpatialEntities.Contains(entity))
        {
            return false;
        }

        var position =
            document.World.SpatialEntities.GetPosition(entity);

        var localScreen =
            document.Viewport.Transform.WorldToScreen(
                new Vector2(position.X, position.Y));

        screen = new Vector2(
            Bounds.X + localScreen.X,
            Bounds.Y + localScreen.Y);

        return true;
    }

    private void DrawRotationGizmo(
    UiRenderContext context,
    Engine.Editor.Documents.EditorDocument document)
    {
        if (!TryGetSelectedScreenPosition(document, out var center))
        {
            return;
        }

        const int segments = 32;
        const float radius = 32.0f;
        const float segmentSize = 5.0f;

        var color = new UiColor(235, 185, 75, 255);

        for (var i = 0; i < segments; i++)
        {
            var angle = MathF.Tau * i / segments;

            var x = center.X + MathF.Cos(angle) * radius;
            var y = center.Y + MathF.Sin(angle) * radius;

            context.DrawRectangle(
                new UiRect(
                    x - segmentSize * 0.5f,
                    y - segmentSize * 0.5f,
                    segmentSize,
                    segmentSize),
                color,
                filled: true,
                layer: 3);
        }
    }

    private void DrawScaleGizmo(
    UiRenderContext context,
    Engine.Editor.Documents.EditorDocument document)
    {
        if (!TryGetSelectedScreenPosition(document, out var center))
        {
            return;
        }

        const float arm = 48.0f;
        const float thickness = 3.0f;
        const float handleSize = 10.0f;

        var xColor = new UiColor(220, 80, 80, 255);
        var yColor = new UiColor(80, 220, 120, 255);

        context.DrawRectangle(
            new UiRect(
                center.X,
                center.Y - thickness * 0.5f,
                arm,
                thickness),
            xColor,
            filled: true,
            layer: 3);

        context.DrawRectangle(
            new UiRect(
                center.X - thickness * 0.5f,
                center.Y - arm,
                thickness,
                arm),
            yColor,
            filled: true,
            layer: 3);

        context.DrawRectangle(
            new UiRect(
                center.X + arm - handleSize * 0.5f,
                center.Y - handleSize * 0.5f,
                handleSize,
                handleSize),
            xColor,
            filled: true,
            layer: 3);

        context.DrawRectangle(
            new UiRect(
                center.X - handleSize * 0.5f,
                center.Y - arm - handleSize * 0.5f,
                handleSize,
                handleSize),
            yColor,
            filled: true,
            layer: 3);
    }

    private void DrawTranslationGizmo(
    UiRenderContext context,
    Editor.Documents.EditorDocument document)
    {
        if (!document.Inspector.TryGetSelectedEntity(
                document.EntitySelection,
                out var entity))
        {
            return;
        }

        if (!document.World.SpatialEntities.Contains(
                entity))
        {
            return;
        }

        var position =
            document.World.SpatialEntities.GetPosition(
                entity);

        var localScreen =
            document.Viewport.Transform.WorldToScreen(
                new Vector2(
                    position.X,
                    position.Y));

        var screen =
            new Vector2(
                Bounds.X + localScreen.X,
                Bounds.Y + localScreen.Y);

        var arm =
            _translationGizmo.ArmLength;

        const float thickness = 3.0f;
        const float arrowSize = 8.0f;

        var xColor =
            new UiColor(
                220,
                80,
                80,
                255);

        var yColor =
            new UiColor(
                80,
                220,
                120,
                255);

        context.DrawRectangle(
            new UiRect(
                screen.X,
                screen.Y - thickness * 0.5f,
                arm,
                thickness),
            xColor,
            filled: true,
            layer: 3);

        context.DrawRectangle(
            new UiRect(
                screen.X - thickness * 0.5f,
                screen.Y - arm,
                thickness,
                arm),
            yColor,
            filled: true,
            layer: 3);

        var xTip = screen.X + arm;
        var yTip = screen.Y - arm;

        // X: ровный треугольный наконечник вправо.
        context.DrawRectangle(
            new UiRect(xTip - 8.0f, screen.Y - 7.0f, 2.0f, 2.0f),
            xColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(xTip - 8.0f, screen.Y - 5.0f, 4.0f, 2.0f),
            xColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(xTip - 8.0f, screen.Y - 3.0f, 6.0f, 2.0f),
            xColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(xTip - 8.0f, screen.Y - 1.0f, 8.0f, 2.0f),
            xColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(xTip - 8.0f, screen.Y + 1.0f, 6.0f, 2.0f),
            xColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(xTip - 8.0f, screen.Y + 3.0f, 4.0f, 2.0f),
            xColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(xTip - 8.0f, screen.Y + 5.0f, 2.0f, 2.0f),
            xColor, filled: true, layer: 3);

        // Y: ровный треугольный наконечник вверх.
        context.DrawRectangle(
            new UiRect(screen.X - 7.0f, yTip, 2.0f, 2.0f),
            yColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(screen.X - 5.0f, yTip, 2.0f, 4.0f),
            yColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(screen.X - 3.0f, yTip, 2.0f, 6.0f),
            yColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(screen.X - 1.0f, yTip, 2.0f, 8.0f),
            yColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(screen.X + 1.0f, yTip, 2.0f, 6.0f),
            yColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(screen.X + 3.0f, yTip, 2.0f, 4.0f),
            yColor, filled: true, layer: 3);

        context.DrawRectangle(
            new UiRect(screen.X + 5.0f, yTip, 2.0f, 2.0f),
            yColor, filled: true, layer: 3);
    }

    private void DrawGrid(
    UiRenderContext context,
    Editor.Documents.EditorDocument document)
    {
        var viewport =
            document.Viewport;

        var zoom =
            viewport.State.Zoom;

        if (zoom <= 0.0f)
        {
            return;
        }

        var spacing =
            CalculateGridSpacing(
                zoom);

        var topLeft =
            viewport.Transform.ScreenToWorld(
                Vector2.Zero);

        var bottomRight =
            viewport.Transform.ScreenToWorld(
                new Vector2(
                    Bounds.Width,
                    Bounds.Height));

        var minX =
            MathF.Min(
                topLeft.X,
                bottomRight.X);

        var maxX =
            MathF.Max(
                topLeft.X,
                bottomRight.X);

        var minY =
            MathF.Min(
                topLeft.Y,
                bottomRight.Y);

        var maxY =
            MathF.Max(
                topLeft.Y,
                bottomRight.Y);

        var startX =
            MathF.Floor(
                minX / spacing) *
            spacing;

        var startY =
            MathF.Floor(
                minY / spacing) *
            spacing;

        var gridColor =
            new UiColor(
                35,
                35,
                35,
                255);

        var axisColor =
            new UiColor(
                70,
                70,
                70,
                255);

        for (
            var worldX = startX;
            worldX <= maxX;
            worldX += spacing)
        {
            var localScreen =
                viewport.Transform.WorldToScreen(
                    new Vector2(
                        worldX,
                        0.0f));

            var screenX =
                Bounds.X +
                localScreen.X;

            var thickness =
                MathF.Abs(worldX) <
                0.0001f
                    ? AxisLineThickness
                    : GridLineThickness;

            var color =
                MathF.Abs(worldX) <
                0.0001f
                    ? axisColor
                    : gridColor;

            context.DrawRectangle(
                new UiRect(
                    screenX -
                    thickness * 0.5f,

                    Bounds.Y,

                    thickness,

                    Bounds.Height),

                color,
                filled: true,
                layer: -10);
        }

        for (
            var worldY = startY;
            worldY <= maxY;
            worldY += spacing)
        {
            var localScreen =
                viewport.Transform.WorldToScreen(
                    new Vector2(
                        0.0f,
                        worldY));

            var screenY =
                Bounds.Y +
                localScreen.Y;

            var thickness =
                MathF.Abs(worldY) <
                0.0001f
                    ? AxisLineThickness
                    : GridLineThickness;

            var color =
                MathF.Abs(worldY) <
                0.0001f
                    ? axisColor
                    : gridColor;

            context.DrawRectangle(
                new UiRect(
                    Bounds.X,

                    screenY -
                    thickness * 0.5f,

                    Bounds.Width,

                    thickness),

                color,
                filled: true,
                layer: -10);
        }
    }

    private static float CalculateGridSpacing(
    float zoom)
    {
        var targetWorldSpacing =
            GridMinScreenSpacing /
            zoom;

        if (targetWorldSpacing <= 0.0f)
        {
            return 1.0f;
        }

        var exponent =
            MathF.Floor(
                MathF.Log10(
                    targetWorldSpacing));

        var magnitude =
            MathF.Pow(
                10.0f,
                exponent);

        var normalized =
            targetWorldSpacing /
            magnitude;

        var step =
            normalized <= 1.0f
                ? 1.0f
                : normalized <= 2.0f
                    ? 2.0f
                    : normalized <= 5.0f
                        ? 5.0f
                        : 10.0f;

        return step * magnitude;
    }

    protected override void OnPointerDown(
    UiPointerEvent pointer)
    {
        if (pointer.Button ==
     Engine.Input.InputMouseButton.Middle)
        {
            _interactionDocument =
                Editor.ActiveDocument;

            _isPanning = true;
            _lastPanPosition =
                pointer.Position;

            pointer.RequestCapture();
            pointer.Handled = true;

            return;
        }

        var document =
            Editor.ActiveDocument;

        if (document is null)
        {
            return;
        }

        var localPosition =
            new Vector2(
                pointer.Position.X - Bounds.X,
                pointer.Position.Y - Bounds.Y);

        if (!ContainsLocalPosition(
                localPosition))
        {
            return;
        }

        if (document.Inspector.TryGetSelectedEntity(
                document.EntitySelection,
                out var selectedEntity) &&
            document.World.SpatialEntities.Contains(
                selectedEntity))
        {
            var selectedPosition =
                document.World.SpatialEntities.GetPosition(
                    selectedEntity);

            var selectedScreen =
                document.Viewport.Transform.WorldToScreen(
                    new Vector2(
                        selectedPosition.X,
                        selectedPosition.Y));

            if (_activeTool == TransformTool.Move)
            {
                var axis =
                    _translationGizmo.HitTest(
                        localPosition,
                        selectedScreen);

                if (axis != EditorGizmoAxis.None)
                {
                    var ecs =
                        document.World.EcsWorld;

                    var usesLocalTransform =
                        ecs.Has<LocalTransform2D>(
                            selectedEntity);

                    var usesWorldTransform =
                        !usesLocalTransform &&
                        ecs.Has<WorldTransform2D>(
                            selectedEntity);

                    // Spatial-only сущности не перемещаем через gizmo.
                    if (!usesLocalTransform &&
                        !usesWorldTransform)
                    {
                        pointer.Handled = true;
                        return;
                    }

                    _isTranslating = true;
                    _interactionDocument = document;
                    _translationAxis = axis;
                    _translationEntity = selectedEntity;

                    _translationStartPosition =
                        selectedPosition;

                    _translationUsesLocalTransform =
                        usesLocalTransform;

                    _translationUsesWorldTransform =
                        usesWorldTransform;

                    if (_translationUsesLocalTransform)
                    {
                        _translationStartLocalTransform =
                            ecs.Get<LocalTransform2D>(
                                selectedEntity);
                    }
                    else
                    {
                        _translationStartWorldTransform =
                            ecs.Get<WorldTransform2D>(
                                selectedEntity);
                    }

                    _translationStartWorld =
                        document.Viewport.Transform.ScreenToWorld(
                            localPosition);

                    pointer.RequestCapture();
                    pointer.Handled = true;
                    return;
                }
            }
            else if (document.World.EcsWorld.Has<LocalTransform2D>(
                         selectedEntity))
            {
                var hitRotation =
                    _activeTool == TransformTool.Rotate &&
                    IsNearRotationRing(
                        localPosition,
                        selectedScreen);

                var scaleAxis =
                    _activeTool == TransformTool.Scale
                        ? HitTestScaleHandle(
                            localPosition,
                            selectedScreen)
                        : EditorGizmoAxis.None;

                if (hitRotation ||
                    scaleAxis != EditorGizmoAxis.None)
                {
                    _interactionDocument = document;
                    _transformEntity = selectedEntity;

                    _transformStartValue =
                        document.World.EcsWorld.Get<LocalTransform2D>(
                            selectedEntity);

                    _transformStartMousePosition =
                        localPosition;

                    _transformCenterScreen =
                        selectedScreen;

                    _isRotating = hitRotation;
                    _isScaling = !hitRotation;
                    _transformAxis = scaleAxis;

                    _rotationStartMouseAngle =
                        MathF.Atan2(
                            localPosition.Y - selectedScreen.Y,
                            localPosition.X - selectedScreen.X);

                    pointer.RequestCapture();
                    pointer.Handled = true;
                    return;
                }
            }
        }

        var worldPosition =
            document.Viewport.Transform.ScreenToWorld(
                localPosition);

        var entity =
            FindEntityAt(
                document,
                worldPosition);

        if (entity.HasValue)
        {
            document.EntitySelection.Set(
                entity.Value);
        }
        else
        {
            document.EntitySelection.Clear();
        }

        pointer.Handled = true;
    }

    protected override void OnPointerMove(
    UiPointerEvent pointer)
    {
        base.OnPointerMove(
            pointer);

        if ((_isTranslating ||
             _isRotating ||
             _isScaling ||
             _isPanning) &&
            !ReferenceEquals(
                _interactionDocument,
                Editor.ActiveDocument))
        {
            _isTranslating = false;
            _isPanning = false;
            _isRotating = false;
            _isScaling = false;

            _translationUsesWorldTransform = false;
            _translationUsesLocalTransform = false;

            _transformAxis = EditorGizmoAxis.None;
            _transformEntity = EntityId.Invalid;

            _translationAxis =
                EditorGizmoAxis.None;

            _translationEntity =
                EntityId.Invalid;

            _interactionDocument =
                null;

            return;
        }

        if (_isTranslating &&
            _translationAxis !=
            EditorGizmoAxis.None)
        {
            var document =
                _interactionDocument;

            if (document is null ||
                !document.World.EcsWorld.Exists(
                    _translationEntity))
            {
                return;
            }

            var localPosition =
                new Vector2(
                    pointer.Position.X -
                    Bounds.X,
                    pointer.Position.Y -
                    Bounds.Y);

            var worldPosition =
                document.Viewport.Transform.ScreenToWorld(
                    localPosition);

            var deltaPos =
                worldPosition -
                _translationStartWorld;

            var x =
                _translationStartPosition.X;

            var y =
                _translationStartPosition.Y;

            if (_translationAxis ==
                EditorGizmoAxis.X)
            {
                x =
                    (int)MathF.Round(
                        _translationStartPosition.X +
                        deltaPos.X,
                        MidpointRounding.AwayFromZero);
            }
            else if (_translationAxis ==
                     EditorGizmoAxis.Y)
            {
                y =
                    (int)MathF.Round(
                        _translationStartPosition.Y +
                        deltaPos.Y,
                        MidpointRounding.AwayFromZero);
            }

            if (_translationUsesLocalTransform &&
                document.World.EcsWorld.Has<LocalTransform2D>(
                    _translationEntity))
            {
                var worldDelta = new FixedVector2(
                    Fixed32.FromInt(
                        x - _translationStartPosition.X),
                    Fixed32.FromInt(
                        y - _translationStartPosition.Y));

                if (TryConvertWorldDeltaToLocal(
                        document,
                        _translationEntity,
                        worldDelta,
                        out var localDelta))
                {
                    ref var transform =
                        ref document.World.EcsWorld.Get<LocalTransform2D>(
                            _translationEntity);

                    transform.Position =
                        _translationStartLocalTransform.Position +
                        localDelta;
                }
            }
            else if (_translationUsesWorldTransform &&
         document.World.EcsWorld.Has<WorldTransform2D>(
             _translationEntity))
            {
                var worldDelta =
                    new FixedVector2(
                        Fixed32.FromInt(
                            x - _translationStartPosition.X),
                        Fixed32.FromInt(
                            y - _translationStartPosition.Y));

                ref var transform =
                    ref document.World.EcsWorld.Get<WorldTransform2D>(
                        _translationEntity);

                transform.Position =
                    _translationStartWorldTransform.Position +
                    worldDelta;
            }
            document.RefreshTransforms();
            pointer.Handled = true;

            return;
        }

        if ((_isRotating || _isScaling) &&
    _interactionDocument is { } transformDocument)
        {
            if (!transformDocument.World.EcsWorld.Exists(
                    _transformEntity) ||
                !transformDocument.World.EcsWorld.Has<LocalTransform2D>(
                    _transformEntity))
            {
                return;
            }

            var currentMousePosition =
                new Vector2(
                    pointer.Position.X - Bounds.X,
                    pointer.Position.Y - Bounds.Y);

            var value =
                _transformStartValue;

            if (_isRotating)
            {
                var currentAngle =
                    MathF.Atan2(
                        currentMousePosition.Y -
                            _transformCenterScreen.Y,
                        currentMousePosition.X -
                            _transformCenterScreen.X);

                var deltaAngle =
                    NormalizeAngle(
                        currentAngle -
                        _rotationStartMouseAngle);

                // В экранных координатах ось Y направлена вниз,
                // поэтому знак угла инвертируется.
                value.Rotation =
                    _transformStartValue.Rotation +
                    Fixed32.FromFloat(-deltaAngle);
            }
            else if (_isScaling)
            {
                var deltaX =
                    (currentMousePosition.X -
                     _transformStartMousePosition.X) /
                    _translationGizmo.ArmLength;

                var deltaY =
                    (_transformStartMousePosition.Y -
                     currentMousePosition.Y) /
                    _translationGizmo.ArmLength;

                var startScale =
                    _transformStartValue.Scale;

                var scaleX =
                    startScale.X.ToFloat();

                var scaleY =
                    startScale.Y.ToFloat();

                if (_transformAxis == EditorGizmoAxis.X)
                {
                    scaleX =
                        ClampScale(
                            scaleX + deltaX,
                            startScale.X.ToFloat());
                }
                else if (_transformAxis == EditorGizmoAxis.Y)
                {
                    scaleY =
                        ClampScale(
                            scaleY + deltaY,
                            startScale.Y.ToFloat());
                }

                value.Scale =
                    new FixedVector2(
                        Fixed32.FromFloat(scaleX),
                        Fixed32.FromFloat(scaleY));
            }

            transformDocument.World.EcsWorld
                .Get<LocalTransform2D>(_transformEntity) = value;
            transformDocument.RefreshTransforms();
            pointer.Handled = true;
            return;
        }

        if (!_isPanning ||
            pointer.Button !=
            Engine.Input.InputMouseButton.Middle)
        {
            return;
        }

        var delta =
            new Vector2(
                pointer.Position.X -
                _lastPanPosition.X,
                pointer.Position.Y -
                _lastPanPosition.Y);

        _lastPanPosition =
            pointer.Position;

        var documentForPan =
            _interactionDocument;

        if (documentForPan is null)
        {
            return;
        }

        var zoom =
            documentForPan.Viewport.State.Zoom;

        if (zoom <= 0.0f)
        {
            return;
        }

        documentForPan.Viewport.State.Pan(
            new Vector2(
                -delta.X / zoom,
                delta.Y / zoom));

        pointer.Handled = true;
    }

    private static bool TryConvertWorldDeltaToLocal(
    Editor.Documents.EditorDocument document,
    EntityId entity,
    FixedVector2 worldDelta,
    out FixedVector2 localDelta)
    {
        var ecs = document.World.EcsWorld;

        localDelta = worldDelta;

        if (!ecs.Has<TransformParent2D>(entity))
        {
            return true;
        }

        var parentEntity =
            ecs.Get<TransformParent2D>(entity).Parent;

        if (!ecs.Exists(parentEntity) ||
            !ecs.Has<WorldTransform2D>(parentEntity))
        {
            return false;
        }

        var parentTransform =
            ecs.Get<WorldTransform2D>(parentEntity);

        if (parentTransform.Scale.X == Fixed32.Zero ||
            parentTransform.Scale.Y == Fixed32.Zero)
        {
            return false;
        }

        var cosine =
            Fixed32.Cos(parentTransform.Rotation);

        var sine =
            Fixed32.Sin(parentTransform.Rotation);

        // Обратное вращение относительно родителя.
        var unrotatedX =
            worldDelta.X * cosine +
            worldDelta.Y * sine;

        var unrotatedY =
            worldDelta.Y * cosine -
            worldDelta.X * sine;

        // Учитываем масштаб родителя.
        localDelta = new FixedVector2(
            unrotatedX / parentTransform.Scale.X,
            unrotatedY / parentTransform.Scale.Y);

        return true;
    }

    protected override void OnPointerUp(
    UiPointerEvent pointer)
    {
        base.OnPointerUp(
            pointer);

        if ((_isRotating || _isScaling) &&
    pointer.Button ==
        Engine.Input.InputMouseButton.Left)
        {
            var document =
                _interactionDocument;

            if (document is not null &&
                document.World.EcsWorld.Exists(
                    _transformEntity) &&
                document.World.EcsWorld.Has<LocalTransform2D>(
                    _transformEntity))
            {
                var finalValue =
                    document.World.EcsWorld.Get<LocalTransform2D>(
                        _transformEntity);

                if (!AreTransformValuesEqual(
                        _transformStartValue,
                        finalValue))
                {
                    document.Execute(
                        new SetLocalTransform2DCommand(
                            document,
                            document.GetEntityReference(
                                _transformEntity),
                            _transformStartValue,
                            finalValue));
                }
            }

            _isRotating = false;
            _isScaling = false;

            _transformAxis =
                EditorGizmoAxis.None;

            _transformEntity =
                EntityId.Invalid;

            _interactionDocument = null;

            pointer.ReleaseCapture();
            pointer.Handled = true;
            return;
        }

        if (_isTranslating &&
            pointer.Button ==
            Engine.Input.InputMouseButton.Left)
        {
            var document =
                _interactionDocument;

            if (document is not null &&
    document.World.EcsWorld.Exists(
        _translationEntity))
            {
                var reference =
                    document.GetEntityReference(
                        _translationEntity);

                if (_translationUsesLocalTransform &&
                    document.World.EcsWorld.Has<LocalTransform2D>(
                        _translationEntity))
                {
                    var finalTransform =
                        document.World.EcsWorld.Get<LocalTransform2D>(
                            _translationEntity);

                    if (finalTransform.Position !=
                        _translationStartLocalTransform.Position)
                    {
                        document.Execute(
                            new SetLocalTransform2DCommand(
                                document,
                                reference,
                                _translationStartLocalTransform,
                                finalTransform));
                    }
                }
                else if (_translationUsesWorldTransform &&
                         document.World.EcsWorld.Has<WorldTransform2D>(
                             _translationEntity))
                {
                    var finalTransform =
                        document.World.EcsWorld.Get<WorldTransform2D>(
                            _translationEntity);

                    if (finalTransform.Position !=
                        _translationStartWorldTransform.Position)
                    {
                        document.Execute(
                            new SetWorldTransform2DCommand(
                                document.World,
                                reference,
                                _translationStartWorldTransform,
                                finalTransform));
                    }
                }
            }
            _translationUsesLocalTransform = false;
            _isTranslating = false;
            _translationUsesWorldTransform = false;

            _translationAxis =
                EditorGizmoAxis.None;

            _translationEntity =
                EntityId.Invalid;

            _interactionDocument =
                null;

            pointer.ReleaseCapture();
            pointer.Handled = true;

            return;
        }

        if (pointer.Button ==
            Engine.Input.InputMouseButton.Middle)
        {
            _isPanning = false;

            _interactionDocument =
                null;

            pointer.ReleaseCapture();
            pointer.Handled = true;

            return;
        }
    }

    private void DrawEntities(
    UiRenderContext context,
    Editor.Documents.EditorDocument document)
    {
        foreach (var entity in
                 document.World
                     .EcsWorld
                     .Inspector
                     .GetEntities())
        {
            if (!document.World.SpatialEntities.Contains(
                    entity))
            {
                continue;
            }

            var position =
                document.World.SpatialEntities.GetPosition(
                    entity);

            var localScreen =
                document.Viewport.Transform.WorldToScreen(
                    new Vector2(
                        position.X,
                        position.Y));

            var screen =
                new Vector2(
                    Bounds.X +
                    localScreen.X,

                    Bounds.Y +
                    localScreen.Y);

            var zoom =
                document.Viewport.State.Zoom;

            var color =
                new UiColor(
                    90,
                    160,
                    230,
                    255);

            if (document.World.EcsWorld.Has<LocalTransform2D>(
                    entity))
            {
                var transform =
                    document.World.EcsWorld.Get<LocalTransform2D>(
                        entity);

                DrawTransformedMarker(
                    context,
                    screen,
                    zoom,
                    EntityMarkerWorldSize,
                    transform,
                    color,
                    showDirection: true,
                    layer: 0);
            }
            else
            {
                var rect =
                    CreateScreenRect(
                        screen,
                        EntityMarkerWorldSize * zoom);

                context.DrawRectangle(
                    rect,
                    color,
                    filled: true);
            }
        }
    }

    private bool IsNearRotationRing(
    Vector2 position,
    Vector2 center)
    {
        const float radius = 32.0f;
        const float tolerance = 8.0f;

        var dx = position.X - center.X;
        var dy = position.Y - center.Y;

        var distance =
            MathF.Sqrt(dx * dx + dy * dy);

        return MathF.Abs(distance - radius) <= tolerance;
    }

    private EditorGizmoAxis HitTestScaleHandle(
        Vector2 position,
        Vector2 center)
    {
        var arm =
            _translationGizmo.ArmLength;

        var xHandle =
            new Vector2(
                center.X + arm,
                center.Y);

        var yHandle =
            new Vector2(
                center.X,
                center.Y - arm);

        var xDistance =
            DistanceSquared(position, xHandle);

        var yDistance =
            DistanceSquared(position, yHandle);

        const float hitRadius = 10.0f;
        var hitRadiusSquared = hitRadius * hitRadius;

        var hitX = xDistance <= hitRadiusSquared;
        var hitY = yDistance <= hitRadiusSquared;

        if (!hitX && !hitY)
            return EditorGizmoAxis.None;

        if (hitX && hitY)
        {
            return xDistance <= yDistance
                ? EditorGizmoAxis.X
                : EditorGizmoAxis.Y;
        }

        return hitX
            ? EditorGizmoAxis.X
            : EditorGizmoAxis.Y;
    }

    private static float DistanceSquared(
        Vector2 left,
        Vector2 right)
    {
        var dx = left.X - right.X;
        var dy = left.Y - right.Y;

        return dx * dx + dy * dy;
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > MathF.PI)
            angle -= 2.0f * MathF.PI;

        while (angle < -MathF.PI)
            angle += 2.0f * MathF.PI;

        return angle;
    }

    private static float ClampScale(
        float value,
        float originalValue)
    {
        const float minimumMagnitude = 0.01f;

        if (MathF.Abs(value) >= minimumMagnitude)
            return value;

        return originalValue < 0.0f
            ? -minimumMagnitude
            : minimumMagnitude;
    }

    private static bool AreTransformValuesEqual(
        LocalTransform2D left,
        LocalTransform2D right)
    {
        return left.Position == right.Position &&
               left.Rotation == right.Rotation &&
               left.Scale == right.Scale;
    }

    private void DrawSelection(
    UiRenderContext context,
    Editor.Documents.EditorDocument document)
    {
        foreach (var entity in
                 document.EntitySelection.Items)
        {
            if (!document.World.EcsWorld.Exists(
                    entity))
            {
                continue;
            }

            if (!document.World.SpatialEntities.Contains(
                    entity))
            {
                continue;
            }

            var position =
                document.World.SpatialEntities.GetPosition(
                    entity);

            var localScreen =
                document.Viewport.Transform.WorldToScreen(
                    new Vector2(
                        position.X,
                        position.Y));

            var screen =
                new Vector2(
                    Bounds.X +
                    localScreen.X,

                    Bounds.Y +
                    localScreen.Y);

            var zoom =
                document.Viewport.State.Zoom;

            var selectionColor =
                new UiColor(
                    255,
                    220,
                    80,
                    255);

            if (document.World.EcsWorld.Has<LocalTransform2D>(
                    entity))
            {
                var transform =
                    document.World.EcsWorld.Get<LocalTransform2D>(
                        entity);

                DrawTransformedMarker(
                    context,
                    screen,
                    zoom,
                    SelectionOutlineWorldSize,
                    transform,
                    selectionColor,
                    showDirection: false,
                    layer: 1);
            }
            else
            {
                var rect =
                    CreateScreenRect(
                        screen,
                        SelectionOutlineWorldSize * zoom);

                context.DrawRectangle(
                    rect,
                    selectionColor,
                    filled: false,
                    layer: 1);
            }
        }
    }

    private static void DrawTransformedMarker(
    UiRenderContext context,
    Vector2 center,
    float zoom,
    float sizeWorld,
    LocalTransform2D transform,
    UiColor color,
    bool showDirection,
    int layer)
    {
        var halfWidth =
            sizeWorld * 0.5f;

        var halfHeight =
            sizeWorld * 0.32f;

        var corners = new[]
        {
        TransformLocalPoint(
            center, zoom, transform,
            -halfWidth, -halfHeight),

        TransformLocalPoint(
            center, zoom, transform,
            halfWidth, -halfHeight),

        TransformLocalPoint(
            center, zoom, transform,
            halfWidth, halfHeight),

        TransformLocalPoint(
            center, zoom, transform,
            -halfWidth, halfHeight)
    };

        DrawClosedOutline(
            context,
            corners,
            color,
            zoom,
            layer);

        if (!showDirection)
        {
            return;
        }

        // Указатель локальной оси X помогает видеть
        // направление даже после поворота на 180 градусов.
        var directionEnd =
            TransformLocalPoint(
                center,
                zoom,
                transform,
                halfWidth + 2.0f,
                0.0f);

        DrawScreenLine(
            context,
            center,
            directionEnd,
            new UiColor(210, 230, 255, 255),
            zoom,
            layer + 1);

        var centerSize =
            Math.Clamp(
                2.5f * zoom,
                2.0f,
                5.0f);

        context.DrawRectangle(
            CreateScreenRect(
                center,
                centerSize),
            color,
            filled: true,
            layer: layer + 1);
    }

    private static Vector2 TransformLocalPoint(
        Vector2 center,
        float zoom,
        LocalTransform2D transform,
        float localX,
        float localY)
    {
        var scaleX =
            transform.Scale.X.ToFloat();

        var scaleY =
            transform.Scale.Y.ToFloat();

        var rotation =
            transform.Rotation.ToFloat();

        var scaledX =
            localX * scaleX;

        var scaledY =
            localY * scaleY;

        var cosine =
            MathF.Cos(rotation);

        var sine =
            MathF.Sin(rotation);

        var rotatedX =
            scaledX * cosine -
            scaledY * sine;

        var rotatedY =
            scaledX * sine +
            scaledY * cosine;

        // В viewport экранная ось Y направлена вниз,
        // поэтому экранное смещение по Y инвертируется.
        return new Vector2(
            center.X + rotatedX * zoom,
            center.Y - rotatedY * zoom);
    }

    private static void DrawClosedOutline(
        UiRenderContext context,
        IReadOnlyList<Vector2> points,
        UiColor color,
        float zoom,
        int layer)
    {
        for (var i = 0; i < points.Count; i++)
        {
            var next =
                (i + 1) % points.Count;

            DrawScreenLine(
                context,
                points[i],
                points[next],
                color,
                zoom,
                layer);
        }
    }

    private static void DrawScreenLine(
        UiRenderContext context,
        Vector2 start,
        Vector2 end,
        UiColor color,
        float zoom,
        int layer)
    {
        var delta =
            end - start;

        var length =
            MathF.Sqrt(
                delta.X * delta.X +
                delta.Y * delta.Y);

        var thickness =
            Math.Clamp(
                zoom * 1.25f,
                1.25f,
                4.0f);

        var sampleSpacing =
            MathF.Max(
                thickness * 1.25f,
                1.5f);

        var steps =
            Math.Clamp(
                (int)MathF.Ceiling(
                    length / sampleSpacing),
                1,
                256);

        for (var i = 0; i <= steps; i++)
        {
            var point =
                start +
                delta * (i / (float)steps);

            context.DrawRectangle(
                new UiRect(
                    point.X - thickness * 0.5f,
                    point.Y - thickness * 0.5f,
                    thickness,
                    thickness),
                color,
                filled: true,
                layer: layer);
        }
    }

    private void DrawInfo(
        UiRenderContext context,
        Editor.Documents.EditorDocument document)
    {
        var selectedCount =
            document.EntitySelection.Count;

        var text =
            $"Entities: " +
            $"{document.World.EcsWorld.EntityCount}  " +
            $"Selected: " +
            $"{selectedCount}  " +
            $"Zoom: " +
            $"{document.Viewport.State.Zoom:0.00}";

        context.DrawText(
            text,
            new Vector2(
                Bounds.X + 8.0f,
                Bounds.Bottom - 22.0f),
            12.0f,
            new UiColor(
                170,
                170,
                170,
                255));
    }

    private EntityId? FindEntityAt(
        Editor.Documents.EditorDocument document,
        Vector2 worldPosition)
    {
        var bestEntity =
            EntityId.Invalid;

        var bestDistanceSquared =
            float.MaxValue;

        var hitRadius =
            EntityMarkerWorldSize *
            0.5f;

        var hitRadiusSquared =
            hitRadius *
            hitRadius;

        foreach (var entity in
                 document.World
                     .EcsWorld
                     .Inspector
                     .GetEntities())
        {
            if (!document.World.SpatialEntities.Contains(
                    entity))
            {
                continue;
            }

            var position =
                document.World.SpatialEntities.GetPosition(
                    entity);

            var dx =
                position.X -
                worldPosition.X;

            var dy =
                position.Y -
                worldPosition.Y;

            var distanceSquared =
                dx * dx +
                dy * dy;

            if (distanceSquared >
                hitRadiusSquared)
            {
                continue;
            }

            if (distanceSquared >=
                bestDistanceSquared)
            {
                continue;
            }

            bestDistanceSquared =
                distanceSquared;

            bestEntity =
                entity;
        }

        return bestEntity.IsValid
            ? bestEntity
            : null;
    }

    private static UiRect CreateScreenRect(
        Vector2 center,
        float size)
    {
        var half =
            size * 0.5f;

        return new UiRect(
            center.X - half,
            center.Y - half,
            size,
            size);
    }

    private bool ContainsLocalPosition(
        Vector2 position)
    {
        return position.X >= 0.0f &&
               position.Y >= 0.0f &&
               position.X <= Bounds.Width &&
               position.Y <= Bounds.Height;
    }

    public void DebugDump()
    {
        var document =
            Editor.ActiveDocument;

        Console.WriteLine();
        Console.WriteLine(
            "========== VIEWPORT DEBUG ==========");

        Console.WriteLine(
            $"Viewport Bounds: " +
            $"X={Bounds.X}, " +
            $"Y={Bounds.Y}, " +
            $"W={Bounds.Width}, " +
            $"H={Bounds.Height}");

        if (document is null)
        {
            Console.WriteLine(
                "Document: NULL");

            Console.WriteLine(
                "====================================");

            return;
        }

        Console.WriteLine(
            $"World entities: " +
            $"{document.World.EcsWorld.EntityCount}");

        Console.WriteLine(
            $"Spatial entities: " +
            $"{document.World.SpatialIndex.EntityCount}");

        Console.WriteLine(
            $"Selected entities: " +
            $"{document.EntitySelection.Count}");

        Console.WriteLine(
            $"Viewport size: " +
            $"{document.Viewport.State.Size.X} x " +
            $"{document.Viewport.State.Size.Y}");

        Console.WriteLine(
            $"Viewport center: " +
            $"{document.Viewport.State.Center}");

        Console.WriteLine(
            $"Viewport zoom: " +
            $"{document.Viewport.State.Zoom}");

        foreach (var entity in
                 document.World
                     .EcsWorld
                     .Inspector
                     .GetEntities())
        {
            Console.WriteLine(
                $"Entity: {entity}");

            var hasPosition =
                document.World.SpatialEntities.Contains(
                    entity);

            Console.WriteLine(
                $"  Has WorldPosition: {hasPosition}");

            if (hasPosition)
            {
                Console.WriteLine(
                    $"  WorldPosition: " +
                    $"{document.World.SpatialEntities.GetPosition(entity)}");
            }

            Console.WriteLine(
                $"  Selected: " +
                $"{document.EntitySelection.Contains(entity)}");
        }

        Console.WriteLine(
            "====================================");
    }
}