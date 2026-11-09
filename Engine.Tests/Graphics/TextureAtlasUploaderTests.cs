using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics;

public sealed class TextureAtlasUploaderTests
{
    [Fact]
    public void Upload_CreatesGpuTextureAndAtlas()
    {
        var manager = new TestTextureManager();
        var buildResult = CreateBuildResult();

        var resource = new TextureAtlasUploader().Upload(
            buildResult,
            manager);

        Assert.True(manager.Exists(resource.Texture));
        Assert.Equal(2, resource.Atlas.Count);

        Assert.Equal(
            buildResult.Texture.Description,
            manager.GetDescription(resource.Texture));
    }

    [Fact]
    public void Dispose_ReleasesGpuTexture()
    {
        var manager = new TestTextureManager();

        var resource = new TextureAtlasUploader().Upload(
            CreateBuildResult(),
            manager);

        var texture = resource.Texture;

        resource.Dispose();

        Assert.False(manager.Exists(texture));
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var manager = new TestTextureManager();

        var resource = new TextureAtlasUploader().Upload(
            CreateBuildResult(),
            manager);

        var texture = resource.Texture;

        resource.Dispose();
        resource.Dispose();

        Assert.False(manager.Exists(texture));
        Assert.Single(manager.DestroyedTextures);
        Assert.Equal(texture, manager.DestroyedTextures[0]);
    }

    [Fact]
    public void Upload_RejectsNullBuildResult()
    {
        var manager = new TestTextureManager();

        Assert.Throws<ArgumentNullException>(
            () => new TextureAtlasUploader().Upload(
                null!,
                manager));
    }

    [Fact]
    public void Upload_RejectsNullTextureManager()
    {
        Assert.Throws<ArgumentNullException>(
            () => new TextureAtlasUploader().Upload(
                CreateBuildResult(),
                null!));
    }

    private static TextureAtlasBuildResult CreateBuildResult()
    {
        var sprites = new[]
        {
            CreateSprite("ship", 4, 4, 10),
            CreateSprite("planet", 2, 2, 20)
        };

        return new TextureAtlasBuilder().Build(sprites);
    }

    private static TextureAtlasSprite CreateSprite(
        string name,
        int width,
        int height,
        byte value)
    {
        var pixels = new byte[width * height * 4];

        Array.Fill(pixels, value);

        return new TextureAtlasSprite(
            name,
            new TextureData(
                width,
                height,
                TextureFormat.Rgba8,
                pixels));
    }

    private sealed class TestTextureManager : ITextureManager
    {
        private uint _nextHandle = 1;

        private readonly Dictionary<TextureHandle, TextureDescription>
            _textures = new();

        public List<TextureHandle> DestroyedTextures { get; } = new();

        public TextureHandle Create(TextureDescription description)
        {
            var handle = new TextureHandle(_nextHandle++);

            _textures.Add(handle, description);

            return handle;
        }

        public TextureHandle Create(TextureData data)
        {
            return Create(data.Description);
        }

        public bool Exists(TextureHandle texture)
        {
            return _textures.ContainsKey(texture);
        }

        public TextureDescription GetDescription(TextureHandle texture)
        {
            return _textures[texture];
        }

        public void Destroy(TextureHandle texture)
        {
            if (_textures.Remove(texture))
                DestroyedTextures.Add(texture);
        }
    }
}