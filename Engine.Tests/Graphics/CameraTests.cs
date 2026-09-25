using Engine.Core.Math;
using Engine.Graphics.Cameras;

namespace Engine.Tests.Graphics;

public sealed class CameraTests
{
    [Fact]
    public void WorldCenterMapsToScreenCenter()
    {
        var camera =
            new Camera(
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
    public void ZoomChangesScreenDistance()
    {
        var camera =
            new Camera(
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
            new Camera(
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