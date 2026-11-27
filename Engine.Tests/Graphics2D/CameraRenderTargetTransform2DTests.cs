using Engine.Camera;
using Engine.Core.Math;
using Engine.Graphics2D.Rendering;
using Xunit;

namespace Engine.Tests.Graphics2D;

public sealed class CameraRenderTargetTransform2DTests
{
    [Fact]
    public void WorldToTarget_UsesIndependentAxisScale()
    {
        var camera = new Camera2D(
            new Vector2(800, 600))
        {
            Position = new Vector2(100, 50),
            Zoom = 2.0f
        };

        var transform = new CameraRenderTargetTransform2D(
            camera,
            1600,
            900);

        var worldPosition = new Vector2(110, 60);

        Assert.Equal(
            new Vector2(840, 420),
            transform.WorldToTarget(worldPosition));

        Assert.Equal(
            new Vector2(840, 480),
            transform.WorldToTargetBottomLeft(worldPosition));
    }

    [Fact]
    public void WorldSizeToTarget_AppliesZoomAndIndependentAxisScale()
    {
        var camera = new Camera2D(
            new Vector2(800, 600))
        {
            Zoom = 2.0f
        };

        var transform = new CameraRenderTargetTransform2D(
            camera,
            1600,
            900);

        var result = transform.WorldSizeToTarget(
            new Vector2(20, 20));

        Assert.Equal(
            new Vector2(80, 60),
            result);
    }

    [Fact]
    public void CameraScreenToTarget_PreservesCoordinatesWhenSizesMatch()
    {
        var camera = new Camera2D(
            new Vector2(800, 600));

        var transform = new CameraRenderTargetTransform2D(
            camera,
            800,
            600);

        var position = new Vector2(123, 456);

        Assert.Equal(
            position,
            transform.CameraScreenToTarget(position));
    }

    [Fact]
    public void Scale_TracksCameraViewportResize()
    {
        var camera = new Camera2D(
            new Vector2(800, 600));

        var transform = new CameraRenderTargetTransform2D(
            camera,
            1600,
            900);

        Assert.Equal(
            new Vector2(2, 1.5f),
            transform.Scale);

        camera.SetViewportSize(
            new Vector2(400, 300));

        Assert.Equal(
            new Vector2(4, 3),
            transform.Scale);
    }

    [Fact]
    public void Quad_GetVertex_UsesSharedTriangleAndUvOrder()
    {
        var quad = new CameraRenderTargetQuad2D(
            new Vector2(10, 20),
            new Vector2(30, 20),
            new Vector2(30, 60),
            new Vector2(10, 60));

        var uv = new Rectangle(
            0.1f, 0.2f, 0.3f, 0.4f);

        var expected = new[]
        {
        (new Vector2(10, 20), new Vector2(0.1f, 0.2f)),
        (new Vector2(30, 20), new Vector2(0.4f, 0.2f)),
        (new Vector2(30, 60), new Vector2(0.4f, 0.6f)),
        (new Vector2(10, 20), new Vector2(0.1f, 0.2f)),
        (new Vector2(30, 60), new Vector2(0.4f, 0.6f)),
        (new Vector2(10, 60), new Vector2(0.1f, 0.6f))
    };

        for (var i = 0; i < CameraRenderTargetQuad2D.VertexCount; i++)
        {
            var actual = quad.GetVertex(i, uv);

            Assert.Equal(expected[i].Item1, actual.Position);
            Assert.Equal(expected[i].Item2, actual.UV);
        }
    }

    [Fact]
    public void CameraScreenQuadToTarget_RotatesBeforeNonUniformScaling()
    {
        var camera = new Camera2D(
            new Vector2(800, 600));

        var transform = new CameraRenderTargetTransform2D(
            camera,
            1600,
            900);

        var quad = transform.CameraScreenQuadToTarget(
            new Vector2(100, 100),
            new Vector2(20, 40),
            90.0f);

        Assert.Equal(
            new Vector2(260, 165),
            quad.TopLeft);

        Assert.Equal(
            new Vector2(260, 195),
            quad.TopRight);

        Assert.Equal(
            new Vector2(180, 195),
            quad.BottomRight);

        Assert.Equal(
            new Vector2(180, 165),
            quad.BottomLeft);
    }

    [Theory]
    [InlineData(0, 600)]
    [InlineData(800, 0)]
    [InlineData(-1, 600)]
    [InlineData(800, -1)]
    public void Constructor_RejectsInvalidTargetSize(
        int width,
        int height)
    {
        var camera = new Camera2D(
            new Vector2(800, 600));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CameraRenderTargetTransform2D(
                camera,
                width,
                height));
    }
}