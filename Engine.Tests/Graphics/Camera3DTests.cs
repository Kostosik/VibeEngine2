
using Engine.Camera;
using Engine.Core.Math;

namespace Engine.Tests.Graphics;

public sealed class Camera3DTests
{
    [Fact]
    public void DefaultCamera_HasIdentityOrientation()
    {
        var camera = new Camera3D();

        Assert.Equal(Vector3.Zero, camera.Position);
        Assert.Equal(Quaternion.Identity, camera.Rotation);

        Assert.Equal(-Vector3.UnitZ, camera.Forward);
        Assert.Equal(Vector3.UnitY, camera.Up);
        Assert.Equal(Vector3.UnitX, camera.Right);
    }

    [Fact]
    public void ViewMatrix_DefaultCamera_IsIdentity()
    {
        var camera = new Camera3D();

        Assert.Equal(
            Matrix4x4.Identity,
            camera.ViewMatrix);
    }

    [Fact]
    public void ViewMatrix_TransformsCameraPositionToOrigin()
    {
        var camera = new Camera3D
        {
            Position = new Vector3(1, 2, 5)
        };

        var result = camera.ViewMatrix.TransformPoint(
            camera.Position);

        Assert.Equal(Vector3.Zero, result);
    }

    [Fact]
    public void ViewMatrix_TransformsWorldOriginCorrectly()
    {
        var camera = new Camera3D
        {
            Position = new Vector3(0, 0, 5)
        };

        var result = camera.ViewMatrix.TransformPoint(
            Vector3.Zero);

        Assert.Equal(new Vector3(0, 0, -5), result);
    }

    [Fact]
    public void LookAt_ChangesCameraForwardDirection()
    {
        var camera = new Camera3D();

        camera.LookAt(Vector3.UnitX);

        Assert.Equal(1.0f, camera.Forward.X, 5);
        Assert.Equal(0.0f, camera.Forward.Y, 5);
        Assert.Equal(0.0f, camera.Forward.Z, 5);
    }

    [Fact]
    public void LookAt_UsesCustomUpDirection()
    {
        var camera = new Camera3D();

        camera.LookAt(
            Vector3.UnitX,
            Vector3.UnitZ);

        Assert.Equal(
            Vector3.UnitZ,
            camera.Up);
    }

    [Fact]
    public void Rotation_NormalizesAssignedQuaternion()
    {
        var camera = new Camera3D();

        camera.Rotation = new Quaternion(0, 2, 0, 2);

        Assert.Equal(
            Quaternion.CreateFromAxisAngle(
                Vector3.UnitY,
                MathF.PI / 2.0f),
            camera.Rotation);
    }

    [Fact]
    public void ProjectionMatrix_UsesCameraSettings()
    {
        var camera = new Camera3D
        {
            FieldOfView = MathF.PI / 2.0f,
            AspectRatio = 16.0f / 9.0f,
            NearPlane = 0.5f,
            FarPlane = 500.0f
        };

        var expected =
            Matrix4x4.CreatePerspectiveFieldOfView(
                MathF.PI / 2.0f,
                16.0f / 9.0f,
                0.5f,
                500.0f);

        Assert.Equal(expected, camera.ProjectionMatrix);
    }

    [Fact]
    public void Position_RejectsNonFiniteValues()
    {
        var camera = new Camera3D();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            camera.Position = new Vector3(float.NaN, 0, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            camera.Position = new Vector3(0, float.PositiveInfinity, 0));

        Assert.Equal(Vector3.Zero, camera.Position);
    }

    [Fact]
    public void Rotation_RejectsInvalidQuaternion()
    {
        var camera = new Camera3D();

        Assert.Throws<ArgumentException>(() =>
            camera.Rotation = new Quaternion(0, 0, 0, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            camera.Rotation = new Quaternion(float.NaN, 0, 0, 1));

        Assert.Equal(Quaternion.Identity, camera.Rotation);
    }

    [Fact]
    public void LookAt_RejectsInvalidDirections()
    {
        var camera = new Camera3D();

        Assert.Throws<ArgumentException>(() =>
            camera.LookAt(Vector3.Zero));

        Assert.Throws<ArgumentException>(() =>
            camera.LookAt(
                Vector3.UnitX,
                Vector3.UnitX));

        Assert.Throws<ArgumentException>(() =>
            camera.LookAt(
                Vector3.UnitX,
                Vector3.Zero));
    }


    [Fact]
    public void SetViewportSize_UpdatesAspectRatio()
    {
        var camera = new Camera3D();

        camera.SetViewportSize(1920, 1080);

        Assert.Equal(16.0f / 9.0f, camera.AspectRatio, 5);

        camera.SetViewportSize(800, 600);

        Assert.Equal(4.0f / 3.0f, camera.AspectRatio, 5);
    }

    [Fact]
    public void SetViewportSize_RejectsInvalidDimensions()
    {
        var camera = new Camera3D();

        var originalAspectRatio = camera.AspectRatio;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            camera.SetViewportSize(0, 600));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            camera.SetViewportSize(800, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            camera.SetViewportSize(-800, 600));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            camera.SetViewportSize(800, -600));

        Assert.Equal(originalAspectRatio, camera.AspectRatio);
    }


    [Fact]
    public void ProjectionSettings_RejectInvalidValues()
    {
        var camera = new Camera3D();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            camera.FieldOfView = 0);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            camera.FieldOfView = MathF.PI);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            camera.AspectRatio = 0);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            camera.NearPlane = 1000);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            camera.FarPlane = 0.1f);
    }
}
