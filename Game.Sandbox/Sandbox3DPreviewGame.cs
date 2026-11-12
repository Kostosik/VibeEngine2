
using Engine.Camera;
using Engine.Core.Math;
using Engine.Core.Time;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.Graphics3D;
using Engine.Graphics3D.Commands;
using Engine.Graphics3D.Rendering;
using Engine.Graphics3D.Resources;
using Engine.Hosting.Desktop;
using Engine.Input;
using Engine.Runtime;

namespace Game.Sandbox;

internal sealed class Sandbox3DPreviewGame : EngineGame
{
    private readonly Sandbox3DPreviewControls _controls;

    private Camera3DController? _cameraController;

    private Vector2 _lastPointerPosition;
    private bool _hasLastPointerPosition;

    private const string VertexShader = """
        #version 330 core

        layout(location = 0) in vec3 aPosition;
        layout(location = 1) in vec3 aNormal;
        layout(location = 2) in vec2 aTexCoord;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;

        void main()
        {
            gl_Position =
                uProjection *
                uView *
                uModel *
                vec4(aPosition, 1.0);
        }
        """;

    private const string FragmentShader = """
        #version 330 core

        out vec4 FragColor;

        void main()
        {
            FragColor = vec4(0.25, 0.65, 1.0, 1.0);
        }
        """;

    private readonly DesktopGameHostContext _context;

    private IMeshManager? _meshes;
    private Camera3D? _camera;

    private MeshHandle _triangleMesh;
    private MeshHandle _cubeMesh;
    private ShaderHandle _shader;

    private double _elapsed;

    public Sandbox3DPreviewGame(
        DesktopGameHostContext context,
        Sandbox3DPreviewControls controls)
        : base(
            (context ?? throw new ArgumentNullException(nameof(context)))
            .Runtime)
    {
        ArgumentNullException.ThrowIfNull(controls);

        _context = context;
        _controls = controls;
    }

    protected override void OnInitialize()
    {
        var graphics = Runtime.Services.Graphics;

        if (graphics is not IMeshManagerProvider meshProvider)
        {
            throw new NotSupportedException(
                "The active graphics backend does not provide 3D mesh management.");
        }

        _meshes = meshProvider.Meshes;

        _camera = new Camera3D
        {
            Position = new Vector3(0, 0, 7)
        };



        _camera.LookAt(Vector3.Zero);
        _camera.SetViewportSize(
            _context.WindowWidth,
            _context.WindowHeight);

        _cameraController = new Camera3DController(_camera)
        {
            MoveSpeed = 4.5f,
            LookSensitivity = 0.0025f,
            ZoomSensitivity = 0.05f
        };

        graphics.Pipeline.Clear();
        graphics.Pipeline.PostProcessing.Clear();
        graphics.Pipeline.AddPass(RenderPass3D.World);

        var shaderDescription = new ShaderDescription(
            VertexShader,
            FragmentShader);

        _shader = graphics.Shaders.Create(shaderDescription);

        _triangleMesh = _meshes.Create(
            MeshPrimitives.CreateTriangle(1.8f));

        _cubeMesh = _meshes.Create(
            MeshPrimitives.CreateCube(1.6f));

        _context.Resized += OnResized;
    }

    protected override void OnUpdate(TimeSnapshot time)
    {
        var deltaSeconds = (float)Math.Clamp(
            time.Delta.TotalSeconds,
            0.0,
            0.1);

        _elapsed += time.Delta.TotalSeconds;

        var input = Runtime.Services.Input;

        var movement = new Vector3(
            input.GetValue(_controls.Right) -
            input.GetValue(_controls.Left),

            input.GetValue(_controls.Up) -
            input.GetValue(_controls.Down),

            input.GetValue(_controls.Forward) -
            input.GetValue(_controls.Backward));

        _cameraController?.Move(
            movement,
            deltaSeconds);

        var pointer = _context.PointerInput;

        if (pointer is null)
        {
            _hasLastPointerPosition = false;
            return;
        }

        if (pointer.IsDown(InputMouseButton.Right))
        {
            var currentPosition = pointer.Position;

            if (_hasLastPointerPosition)
            {
                var delta = currentPosition - _lastPointerPosition;

                _cameraController?.LookFromMouseDelta(delta);
            }

            _lastPointerPosition = currentPosition;
            _hasLastPointerPosition = true;
        }
        else
        {
            _hasLastPointerPosition = false;
        }

        _cameraController?.Zoom(pointer.ScrollDelta);
    }

    protected override void OnRender(double interpolationAlpha)
    {
        var graphics = Runtime.Services.Graphics;
        var camera = _camera
            ?? throw new InvalidOperationException(
                "3D preview camera is not initialized.");

        var view = camera.ViewMatrix;
        var projection = camera.ProjectionMatrix;

        // Статичный треугольник слева.
        var triangleModel = Matrix4x4.CreateTranslation(
            new Vector3(-1.7f, 0, 0));

        graphics.Submit(
            new DrawMeshCommand(
                _triangleMesh,
                _shader,
                triangleModel,
                view,
                projection));

        // Куб справа вращается вокруг двух осей.
        var rotation =
            Quaternion.CreateFromAxisAngle(
                Vector3.UnitY,
                (float)_elapsed) *
            Quaternion.CreateFromAxisAngle(
                Vector3.UnitX,
                (float)(_elapsed * 0.65));

        var cubeModel =
            Matrix4x4.CreateRotation(rotation) *
            Matrix4x4.CreateTranslation(
                new Vector3(1.4f, 0, 0));

        graphics.Submit(
            new DrawMeshCommand(
                _cubeMesh,
                _shader,
                cubeModel,
                view,
                projection));
    }

    protected override void OnShutdown()
    {
        _context.Resized -= OnResized;

        var errors = new List<Exception>();

        void TryCleanup(Action cleanup)
        {
            try
            {
                cleanup();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        var meshes = _meshes;

        if (meshes is not null)
        {
            if (_triangleMesh.IsValid)
            {
                TryCleanup(() => meshes.Destroy(_triangleMesh));
                _triangleMesh = MeshHandle.Invalid;
            }

            if (_cubeMesh.IsValid)
            {
                TryCleanup(() => meshes.Destroy(_cubeMesh));
                _cubeMesh = MeshHandle.Invalid;
            }
        }

        if (_shader.IsValid)
        {
            TryCleanup(() =>
                Runtime.Services.Graphics.Shaders.Destroy(_shader));

            _shader = ShaderHandle.Invalid;
        }

        if (errors.Count > 0)
        {
            throw new AggregateException(
                "3D preview resources could not be fully released.",
                errors);
        }
    }

    private void OnResized(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        _camera?.SetViewportSize(width, height);
    }
}
