using Engine.Camera;
using Engine.Core.Math;

namespace Engine.Tests.Graphics;

public sealed class CameraTests
{
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