using Engine.Core.Math;
using Engine.Core.Time;
using Engine.Graphics.Cameras;
using Engine.Input;

namespace Game.Sandbox.Camera;

public sealed class CameraController
{
    private readonly Engine.Graphics.Cameras.Camera _camera;
    private readonly IInput _input;

    private readonly InputAction _moveUp;
    private readonly InputAction _moveDown;
    private readonly InputAction _moveLeft;
    private readonly InputAction _moveRight;
    public bool AllowMovement { get; set; } = true;
    private readonly InputAction _zoomIn;
    private readonly InputAction _zoomOut;

    public CameraController(
        Engine.Graphics.Cameras.Camera camera,
        IInput input,
        InputAction moveUp,
        InputAction moveDown,
        InputAction moveLeft,
        InputAction moveRight,
        InputAction zoomIn,
        InputAction zoomOut,
        float moveSpeed,
        float zoomSpeed)
    {
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(input);

        if (moveSpeed <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(moveSpeed));
        }

        if (zoomSpeed <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(zoomSpeed));
        }

        _camera = camera;
        _input = input;

        _moveUp = moveUp;
        _moveDown = moveDown;
        _moveLeft = moveLeft;
        _moveRight = moveRight;

        _zoomIn = zoomIn;
        _zoomOut = zoomOut;

        MoveSpeed = moveSpeed;
        ZoomSpeed = zoomSpeed;
    }

    public float MoveSpeed { get; }

    public float ZoomSpeed { get; }

    public void Update(
        TimeSnapshot time)
    {
        var direction =
            Vector2.Zero;

        if (_input.IsDown(_moveUp))
        {
            direction +=
                new Vector2(0.0f, -1.0f);
        }

        if (_input.IsDown(_moveDown))
        {
            direction +=
                new Vector2(0.0f, 1.0f);
        }

        if (_input.IsDown(_moveLeft))
        {
            direction +=
                new Vector2(-1.0f, 0.0f);
        }

        if (_input.IsDown(_moveRight))
        {
            direction +=
                new Vector2(1.0f, 0.0f);
        }

        if (AllowMovement)
        {
            _camera.Position +=
                direction *
                MoveSpeed *
                (float)time.Delta.TotalSeconds;
        }

        var zoomDirection =
            0.0f;

        if (_input.IsDown(_zoomIn))
        {
            zoomDirection += 1.0f;
        }

        if (_input.IsDown(_zoomOut))
        {
            zoomDirection -= 1.0f;
        }

        if (zoomDirection != 0.0f)
        {
            _camera.Zoom +=
                zoomDirection *
                ZoomSpeed *
                (float)time.Delta.TotalSeconds;
        }

        if (_camera.Zoom < 1.0f)
        {
            _camera.Zoom = 1.0f;
        }
    }
}