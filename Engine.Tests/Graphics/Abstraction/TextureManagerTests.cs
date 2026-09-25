using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics.Abstraction;

public sealed class TextureManagerTests
{
    [Fact]
    public void TextureManager_CanCreateAndQueryTexture()
    {
        ITextureManager manager =
            new TestTextureManager();

        var description =
            new TextureDescription(
                128,
                64,
                TextureFormat.Rgba8);

        var texture =
            manager.Create(description);

        Assert.True(
            manager.Exists(texture));

        Assert.Equal(
            description,
            manager.GetDescription(texture));
    }

    [Fact]
    public void Destroy_RemovesTexture()
    {
        ITextureManager manager =
            new TestTextureManager();

        var texture =
            manager.Create(
                new TextureDescription(
                    128,
                    64,
                    TextureFormat.Rgba8));

        manager.Destroy(texture);

        Assert.False(
            manager.Exists(texture));

        Assert.Throws<KeyNotFoundException>(
            () => manager.GetDescription(texture));
    }

    private sealed class TestTextureManager
        : ITextureManager
    {
        private uint _nextHandle = 1;

        private readonly Dictionary<
            TextureHandle,
            TextureDescription> _textures = new();

        public TextureHandle Create(
            TextureDescription description)
        {
            var handle =
                new TextureHandle(
                    _nextHandle++);

            _textures.Add(
                handle,
                description);

            return handle;
        }

        public TextureHandle Create(
            TextureData data)
        {
            return Create(
                data.Description);
        }

        public bool Exists(
            TextureHandle texture)
        {
            return _textures.ContainsKey(
                texture);
        }

        public TextureDescription GetDescription(
            TextureHandle texture)
        {
            if (!_textures.TryGetValue(
                    texture,
                    out var description))
            {
                throw new KeyNotFoundException();
            }

            return description;
        }

        public void Destroy(
            TextureHandle texture)
        {
            _textures.Remove(texture);
        }
    }
}