using Engine.Camera;
using Engine.Core.Math;

namespace Engine.Tests.Graphics;

public sealed class CameraTests
{

    [Fact]
    public void Position_RejectsNonFiniteValues()
    {
        var camera = new Camera2D(new Vector2(1280, 720));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => camera.Position = new Vector2(float.NaN, 0));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => camera.Position = new Vector2(0, float.PositiveInfinity));

        Assert.Equal(Vector2.Zero, camera.Position);
    }

    [Fact]
    public void Zoom_RejectsValuesThatProduceNonFiniteWorldBounds()
    {
        var camera = new Camera2D(new Vector2(1280, 720));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => camera.Zoom = float.Epsilon);

        Assert.Equal(1.0f, camera.Zoom);
    }

    [Fact]
    public void WorldLimit_ClampsCameraPosition()
    {
        var camera = new Camera2D(new Vector2(200, 100));

        camera.SetWorldLimit(
            new Rectangle(0, 0, 1000, 800));

        camera.Position = new Vector2(-100, 900);

        Assert.Equal(
            new Vector2(100, 750),
            camera.Position);
    }

    [Fact]
    public void Shake_ChangesScreenTransformWithoutMovingCameraAndStopsAfterDuration()
    {
        var camera = new Camera2D(new Vector2(1280, 720));

        var worldPosition = new Vector2(100, 50);
        var originalScreenPosition = camera.WorldToScreen(worldPosition);

        camera.StartShake(
            intensity: 12.0f,
            duration: 0.5f,
            seed: 123);

        camera.UpdateShake(0.1f);

        var shakenScreenPosition = camera.WorldToScreen(worldPosition);

        Assert.NotEqual(
            originalScreenPosition,
            shakenScreenPosition);

        // Logical camera position must not change.
        Assert.Equal(Vector2.Zero, camera.Position);

        Assert.True(camera.IsShaking);

        camera.UpdateShake(0.5f);

        Assert.False(camera.IsShaking);
        Assert.Equal(Vector2.Zero, camera.ShakeOffset);

        Assert.Equal(
            originalScreenPosition,
            camera.WorldToScreen(worldPosition));
    }

    [Fact]
    public void Follow_DeadZoneKeepsCameraStillUntilTargetLeavesZone()
    {
        var camera = new Camera2D(new Vector2(1280, 720))
        {
            DeadZoneSize = new Vector2(200, 100)
        };

        // Объект находится внутри зоны.
        camera.Follow(
            new Vector2(50, 0),
            1.0f / 60.0f,
            smoothTime: 0.0f);

        Assert.Equal(Vector2.Zero, camera.Position);

        // Объект выходит за правую границу зоны.
        camera.Follow(
            new Vector2(150, 0),
            1.0f / 60.0f,
            smoothTime: 0.0f);

        Assert.Equal(new Vector2(50, 0), camera.Position);
    }

    [Fact]
    public void WorldLimit_CentersCameraWhenWorldIsSmallerThanViewport()
    {
        var camera = new Camera2D(new Vector2(200, 100));

        camera.SetWorldLimit(
            new Rectangle(0, 0, 100, 50));

        camera.Position = new Vector2(500, 500);

        Assert.Equal(
            new Vector2(50, 25),
            camera.Position);
    }

    [Fact]
    public void Follow_SmoothlyApproachesTargetWithoutOvershooting()
    {
        var camera = new Camera2D(new Vector2(1280, 720));

        var target = new Vector2(100, 50);

        for (var i = 0; i < 60; i++)
            camera.Follow(target, 1.0f / 60.0f);

        Assert.True(camera.Position.X > 99.0f);
        Assert.True(camera.Position.X <= target.X);

        Assert.True(camera.Position.Y > 49.0f);
        Assert.True(camera.Position.Y <= target.Y);
    }


    [Fact]
    public void WorldCenterMapsToScreenCenter()
    {
        var camera =
            new Camera2D(
                new Vector2(
                    1280,
                    720));

        camera.Position =
            new Vector2(
                100,
                50);

        var screen =
            camera.WorldToScreen(
                new Vector2(
                    100,
                    50));

        Assert.Equal(
            new Vector2(
                640,
                360),
            screen);
    }

    [Fact]
    public void ChangingViewportUpdatesWorldBoundsAndScreenCenter()
    {
        var camera = new Camera2D(new Vector2(1280, 720))
        {
            Position = new Vector2(100, 50),
            Zoom = 2.0f
        };

        camera.SetViewportSize(new Vector2(800, 600));

        Assert.Equal(new Vector2(800, 600), camera.ViewportSize);

        Assert.Equal(
            new Rectangle(-100, -100, 400, 300),
            camera.WorldBounds);

        Assert.Equal(
            new Vector2(400, 300),
            camera.WorldToScreen(camera.Position));
    }

    [Fact]
    public void WorldBoundsAndVisibilityFollowCameraPositionAndZoom()
    {
        var camera = new Camera2D(new Vector2(100, 80))
        {
            Position = new Vector2(10, 20),
            Zoom = 2.0f
        };

        Assert.Equal(
            new Rectangle(-15, 0, 50, 40),
            camera.WorldBounds);

        Assert.True(
            camera.IsVisible(new Rectangle(0, 10, 5, 5)));

        Assert.False(
            camera.IsVisible(new Rectangle(36, 10, 5, 5)));

        camera.Position = new Vector2(40, 20);

        Assert.True(
            camera.IsVisible(new Rectangle(36, 10, 5, 5)));
    }

    [Fact]
    public void ZoomRejectsNonPositiveAndNonFiniteValues()
    {
        var camera = new Camera2D(new Vector2(1280, 720));

        var invalidValues = new[]
        {
        0.0f,
        -1.0f,
        float.NaN,
        float.PositiveInfinity,
        float.NegativeInfinity
    };

        foreach (var value in invalidValues)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => camera.Zoom = value);
        }
    }

    [Fact]
    public void SetViewportSizeRejectsInvalidDimensions()
    {
        var camera = new Camera2D(new Vector2(1280, 720));

        var invalidValues = new[]
        {
        0.0f,
        -1.0f,
        float.NaN,
        float.PositiveInfinity,
        float.NegativeInfinity
    };

        foreach (var value in invalidValues)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => camera.SetViewportSize(
                    new Vector2(value, 720)));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => camera.SetViewportSize(
                    new Vector2(1280, value)));
        }

        Assert.Equal(
            new Vector2(1280, 720),
            camera.ViewportSize);
    }

    [Fact]
    public void ZoomChangesScreenDistance()
    {
        var camera =
            new Camera2D(
                new Vector2(
                    1280,
                    720));

        camera.Position =
            new Vector2(
                100,
                50);

        camera.Zoom =
            2.0f;

        var screen =
            camera.WorldToScreen(
                new Vector2(
                    110,
                    50));

        Assert.Equal(
            new Vector2(
                660,
                360),
            screen);
    }

    [Fact]
    public void ScreenToWorldIsInverseOfWorldToScreen()
    {
        var camera =
            new Camera2D(
                new Vector2(
                    1280,
                    720));

        camera.Position =
            new Vector2(
                100,
                50);

        camera.Zoom =
            2.0f;

        var world =
            new Vector2(
                123,
                87);

        var screen =
            camera.WorldToScreen(
                world);

        var result =
            camera.ScreenToWorld(
                screen);

        Assert.Equal(
            world,
            result);
    }
}