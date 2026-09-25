using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics.Abstraction;

public sealed class TextureDataTests
{
    [Fact]
    public void Constructor_CreatesData()
    {
        var pixels =
            new byte[4 * 2 * 2];

        var data =
            new TextureData(
                2,
                2,
                TextureFormat.Rgba8,
                pixels);

        Assert.Equal(2, data.Width);
        Assert.Equal(2, data.Height);
        Assert.Equal(
            TextureFormat.Rgba8,
            data.Format);

        Assert.Equal(
            pixels,
            data.Pixels.ToArray());
    }

    [Fact]
    public void Description_ReturnsCorrectDescription()
    {
        var data =
            new TextureData(
                4,
                8,
                TextureFormat.Rgba8,
                new byte[4 * 8 * 4]);

        var description =
            data.Description;

        Assert.Equal(4, description.Width);
        Assert.Equal(8, description.Height);
        Assert.Equal(
            TextureFormat.Rgba8,
            description.Format);
    }

    [Fact]
    public void Constructor_RejectsZeroWidth()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new TextureData(
                    0,
                    2,
                    TextureFormat.Rgba8,
                    new byte[16]));
    }

    [Fact]
    public void Constructor_RejectsZeroHeight()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new TextureData(
                    2,
                    0,
                    TextureFormat.Rgba8,
                    new byte[16]));
    }
}