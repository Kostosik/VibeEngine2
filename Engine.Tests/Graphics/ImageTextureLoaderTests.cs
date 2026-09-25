using Engine.Core.Assets;
using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics;

public sealed class ImageTextureLoaderTests
{
    [Fact]
    public void LoadDecodesPngToRgbaTextureData()
    {
        var png =
            Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

        var source =
            new TestAssetSource(
                png);

        var loader =
            new ImageTextureLoader();

        var texture =
            loader.Load(
                source,
                new AssetPath("test.png"));

        Assert.Equal(
            1,
            texture.Width);

        Assert.Equal(
            1,
            texture.Height);

        Assert.Equal(
            TextureFormat.Rgba8,
            texture.Format);

        Assert.Equal(
            4,
            texture.Pixels.Length);
    }

    private sealed class TestAssetSource :
        IAssetSource
    {
        private readonly byte[] _data;

        public TestAssetSource(
            byte[] data)
        {
            _data = data;
        }

        public ReadOnlyMemory<byte> Load(
            AssetPath path)
        {
            return _data;
        }
    }
}