
using Engine.Camera;
using Engine.Core.Math;

namespace Engine.Tests.Graphics;

public sealed class Camera3DControllerTests
{
    [Fact]
    public void Move_UsesCameraForward()
    {
        var camera = new Camera3D();
        var controller = new Camera3DController(camera);

        controller.Move(
            new Vector3(0, 0, 1),
            1.0f);

        Assert.Equal(
            new Vector3(0, 0, -5),
            camera.Position);
    }

    [Fact]
    public void Move_UsesCurrentCameraOrientation()
    {
        var camera = new Camera3D();
        camera.LookAt(Vector3.UnitX);

        var controller = new Camera3DController(camera);

        controller.Move(
            new Vector3(0, 0, 1),
            1.0f);

        Assert.Equal(5.0f, camera.Position.X, 4);
        Assert.Equal(0.0f, camera.Position.Z, 4);
    }

    [Fact]
    public void Move_NormalizesDiagonalDirection()
    {
        var camera = new Camera3D();
        var controller = new Camera3DController(camera);

        controller.Move(
            new Vector3(1, 0, 1),
            1.0f);

        Assert.Equal(
            5.0f,
            camera.Position.Length(),
            4);
    }

    [Fact]
    public void Move_RespectsMoveSpeed()
    {
        var camera = new Camera3D();

        var controller = new Camera3DController(camera)
        {
            MoveSpeed = 10.0f
        };

        controller.Move(
            new Vector3(0, 0, 1),
            0.5f);

        Assert.Equal(
            new Vector3(0, 0, -5),
            camera.Position);
    }

    [Fact]
    public void Look_ChangesYaw()
    {
        var camera = new Camera3D();
        var controller = new Camera3DController(camera);

        controller.Look(
            -MathF.PI / 2.0f,
            0);

        Assert.Equal(1.0f, camera.Forward.X, 4);
        Assert.Equal(0.0f, camera.Forward.Z, 4);
    }

    [Fact]
    public void Look_ClampsPitch()
    {
        var camera = new Camera3D();
        var controller = new Camera3DController(camera);

        controller.Look(0, MathF.PI);

        var expectedMaxPitch =
            89.0f * MathF.PI / 180.0f;

        Assert.Equal(
            MathF.Sin(expectedMaxPitch),
            camera.Forward.Y,
            4);
    }

    [Fact]
    public void LookFromMouseDelta_RotatesCamera()
    {
        var camera = new Camera3D();
        var controller = new Camera3DController(camera);

        controller.LookFromMouseDelta(
            new Vector2(100, 0));

        Assert.True(camera.Forward.X > 0);
    }

    [Fact]
    public void Zoom_PositiveDelta_DecreasesFieldOfView()
    {
        var camera = new Camera3D();
        var controller = new Camera3DController(camera);

        var previous = camera.FieldOfView;

        controller.Zoom(2);

        Assert.True(
            camera.FieldOfView < previous);
    }

    [Fact]
    public void Zoom_ClampsFieldOfView()
    {
        var camera = new Camera3D();
        var controller = new Camera3DController(camera);

        controller.Zoom(1000);

        Assert.Equal(
            25.0f * MathF.PI / 180.0f,
            camera.FieldOfView,
            4);

        controller.Zoom(-1000);

        Assert.Equal(
            90.0f * MathF.PI / 180.0f,
            camera.FieldOfView,
            4);
    }

    [Fact]
    public void Move_RejectsInvalidDelta()
    {
        var controller = new Camera3DController(new Camera3D());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            controller.Move(Vector3.UnitX, float.NaN));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            controller.Move(Vector3.UnitX, -1));
    }

    [Fact]
    public void Look_RejectsNonFiniteValues()
    {
        var controller = new Camera3DController(new Camera3D());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            controller.Look(float.NaN, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            controller.Look(0, float.PositiveInfinity));
    }

    [Fact]
    public void MoveSpeed_RejectsNegativeValue()
    {
        var controller = new Camera3DController(new Camera3D());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            controller.MoveSpeed = -1);
    }
}
