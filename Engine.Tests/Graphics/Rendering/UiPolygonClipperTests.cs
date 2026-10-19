
using Engine.Core.Math;
using Engine.Graphics;
using Engine.Graphics.Commands;
using Engine.Graphics.Fonts;
using Engine.Graphics.OpenGL.Rendering;
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;
using Engine.UI.Core;
using Engine.UI.Layout;
using Engine.UI.Styling;

namespace Engine.Tests.Graphics.Rendering;

public sealed class UiPolygonClipperTests
{
    [Fact]
    public void ClipPolygon_ClipsQuadToDiamond_InterpolatesUv()
    {
        var region = CreateDiamond();

        var source = new[]
        {
            new UiClipVertex(
                new Vector2(-5.0f, -5.0f),
                new Vector2(0.0f, 0.0f)),

            new UiClipVertex(
                new Vector2(45.0f, -5.0f),
                new Vector2(1.0f, 0.0f)),

            new UiClipVertex(
                new Vector2(45.0f, 45.0f),
                new Vector2(1.0f, 1.0f)),

            new UiClipVertex(
                new Vector2(-5.0f, 45.0f),
                new Vector2(0.0f, 1.0f))
        };

        var clipped =
            UiPolygonClipper.ClipPolygon(
                source,
                region);

        Assert.Equal(4, clipped.Count);

        foreach (var vertex in clipped)
        {
            var distance =
                MathF.Abs(vertex.Position.X - 20.0f) +
                MathF.Abs(vertex.Position.Y - 20.0f);

            Assert.InRange(distance, 19.99f, 20.01f);

            // UV должны соответствовать исходной текстуре.
            var expectedU =
                (vertex.Position.X + 5.0f) / 50.0f;

            var expectedV =
                (vertex.Position.Y + 5.0f) / 50.0f;

            Assert.Equal(
                expectedU,
                vertex.UV.X,
                precision: 3);

            Assert.Equal(
                expectedV,
                vertex.UV.Y,
                precision: 3);
        }
    }

    [Fact]
    public void ClipPolygon_OutsideRegion_ReturnsEmpty()
    {
        var region = CreateDiamond();

        var source = new[]
        {
            new UiClipVertex(
                new Vector2(50.0f, 50.0f),
                Vector2.Zero),

            new UiClipVertex(
                new Vector2(70.0f, 50.0f),
                new Vector2(1.0f, 0.0f)),

            new UiClipVertex(
                new Vector2(70.0f, 70.0f),
                new Vector2(1.0f, 1.0f)),

            new UiClipVertex(
                new Vector2(50.0f, 70.0f),
                new Vector2(0.0f, 1.0f))
        };

        var clipped =
            UiPolygonClipper.ClipPolygon(
                source,
                region);

        Assert.Empty(clipped);
    }

    [Fact]
    public void ClipSegment_CrossingDiamond_ClipsBothEndpoints()
    {
        var region = CreateDiamond();

        var intersects =
            UiPolygonClipper.ClipSegment(
                new Vector2(-10.0f, 20.0f),
                new Vector2(50.0f, 20.0f),
                region,
                out var start,
                out var end);

        Assert.True(intersects);

        Assert.Equal(0.0f, start.X, precision: 2);
        Assert.Equal(20.0f, start.Y, precision: 2);

        Assert.Equal(40.0f, end.X, precision: 2);
        Assert.Equal(20.0f, end.Y, precision: 2);
    }

    [Fact]
    public void ClipSegment_OutsideRegion_ReturnsFalse()
    {
        var region = CreateDiamond();

        var intersects =
            UiPolygonClipper.ClipSegment(
                new Vector2(50.0f, 50.0f),
                new Vector2(70.0f, 70.0f),
                region,
                out _,
                out _);

        Assert.False(intersects);
    }

    [Fact]
    public void UiRenderContext_PassesRotatedClipRegionToCommand()
    {
        var graphics =
            new RecordingGraphicsDevice();

        var context =
            new UiRenderContext(
                graphics,
                new UiTheme());

        context.PushRotation(
            new Vector2(50.0f, 25.0f),
            45.0f);

        context.PushClip(
            new UiRect(
                0.0f,
                0.0f,
                100.0f,
                50.0f));

        context.DrawRectangle(
            new UiRect(
                0.0f,
                0.0f,
                100.0f,
                50.0f),
            new UiColor(255, 255, 255, 255));

        var command =
            Assert.IsType<DrawUiRectangleCommand>(
                Assert.Single(graphics.Commands));

        Assert.NotNull(command.ClipRegion);

        Assert.Equal(
            45.0f,
            command.RotationDegrees,
            precision: 3);

        var bounds =
            command.ClipRegion!.Bounds;

        Assert.Equal(
            106.07f,
            bounds.Width,
            precision: 1);

        Assert.Equal(
            106.07f,
            bounds.Height,
            precision: 1);

        context.PopClip();
        context.PopRotation();
    }

    private static ClipRegion CreateDiamond()
    {
        return ClipRegion.FromVertices(
        [
            new Vector2(20.0f, 0.0f),
            new Vector2(40.0f, 20.0f),
            new Vector2(20.0f, 40.0f),
            new Vector2(0.0f, 20.0f)
        ]);
    }

    private sealed class RecordingGraphicsDevice
        : IGraphicsDevice
    {
        public List<IRenderCommand> Commands { get; } = new();

        public ITextureManager Textures => null!;

        public IFontManager Fonts => null!;

        public IGraphicsBufferManager Buffers => null!;

        public IShaderManager Shaders => null!;

        public IRenderTargetManager RenderTargets => null!;

        public RenderPipeline Pipeline => null!;

        public void BeginFrame()
        {
        }

        public void Submit(IRenderCommand command)
        {
            Commands.Add(command);
        }

        public void EndFrame()
        {
        }
    }
}
