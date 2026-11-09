using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics.Abstraction;

public sealed class TextureManagerTests
{
    [Fact]
    public void TextureDescription_UsesDefaultSamplerWhenNotSpecified()
    {
        var description = new TextureDescription(
            128,
            64,
            TextureFormat.Rgba8);

        Assert.Equal(
            TextureSamplerDescription.Default,
            description.EffectiveSampler);
    }

    [Fact]
    public void TextureDescription_DisablesMipmapsByDefault()
    {
        var description = new TextureDescription(
            128,
            64,
            TextureFormat.Rgba8);

        Assert.False(description.GenerateMipmaps);
    }

    [Fact]
    public void TextureDescription_CanEnableMipmaps()
    {
        var description = new TextureDescription(
            128,
            64,
            TextureFormat.Rgba8)
        {
            GenerateMipmaps = true
        };

        Assert.True(description.GenerateMipmaps);
    }

    [Fact]
    public void TextureSampler_DefaultAnisotropyIsOne()
    {
        var sampler = TextureSamplerDescription.Default;

        Assert.Equal(1.0f, sampler.Anisotropy);
    }

    [Theory]
    [InlineData(1.0f)]
    [InlineData(2.0f)]
    [InlineData(4.0f)]
    [InlineData(8.0f)]
    [InlineData(16.0f)]
    public void TextureSampler_CanConfigureAnisotropy(float anisotropy)
    {
        var sampler = new TextureSamplerDescription(
            TextureFilter.Linear,
            TextureFilter.Linear,
            TextureWrapMode.ClampToEdge,
            TextureWrapMode.ClampToEdge,
            anisotropy);

        Assert.Equal(anisotropy, sampler.Anisotropy);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(-1.0f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void TextureSampler_RejectsInvalidAnisotropy(float anisotropy)
    {
        var sampler = new TextureSamplerDescription(
            TextureFilter.Linear,
            TextureFilter.Linear,
            TextureWrapMode.ClampToEdge,
            TextureWrapMode.ClampToEdge,
            anisotropy);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => ValidateSamplerForTest(sampler));
    }

    [Fact]
    public void TextureFilter_ContainsMipmapFilteringModes()
    {
        Assert.True(Enum.IsDefined(TextureFilter.NearestMipmapNearest));
        Assert.True(Enum.IsDefined(TextureFilter.LinearMipmapNearest));
        Assert.True(Enum.IsDefined(TextureFilter.NearestMipmapLinear));
        Assert.True(Enum.IsDefined(TextureFilter.LinearMipmapLinear));
    }

    [Fact]
    public void TextureManager_PreservesCustomSamplerDescription()
    {
        ITextureManager manager = new TestTextureManager();

        var sampler = new TextureSamplerDescription(
            TextureFilter.Linear,
            TextureFilter.Nearest,
            TextureWrapMode.Repeat,
            TextureWrapMode.MirroredRepeat);

        var description = new TextureDescription(
            128,
            64,
            TextureFormat.Rgba8)
        {
            Sampler = sampler
        };

        var texture = manager.Create(description);

        Assert.Equal(
            sampler,
            manager.GetDescription(texture).EffectiveSampler);
    }

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
    private static void ValidateSamplerForTest(
TextureSamplerDescription sampler)
    {
        if (!float.IsFinite(sampler.Anisotropy) ||
            sampler.Anisotropy < 1.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(sampler));
        }
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