using Engine.Graphics.Fonts;
using Engine.Graphics.OpenGL.Fonts;
using Engine.Graphics.Resources;
using StbTrueTypeSharp;

namespace Engine.Tests.Graphics.Fonts;

public sealed class OpenGLFontManagerTests
{
    private const int LatinExtendedFirst = 0x0100;
    private const int LatinExtendedCount = 0x0100;

    [Fact]
    public void Create_AdditionalCodepointRange_LoadsSupportedGlyph()
    {
        var fontData = LoadTestFont();

        var codepoint = FindSupportedCodepoint(
            fontData,
            LatinExtendedFirst,
            LatinExtendedCount);

        var textures = new TestTextureManager();

        using var fonts = new OpenGLFontManager(textures);

        var font = fonts.Create(
            fontData,
            new FontDescription(18)
            {
                AdditionalCodepointRanges =
                [
                    new FontCodepointRange(
                        LatinExtendedFirst,
                        LatinExtendedCount)
                ]
            });

        var actualGlyph = fonts.GetGlyph(
            font,
            codepoint);

        var fallbackGlyph = fonts.GetGlyph(
            font,
            '?');

        Assert.True(actualGlyph.Texture.IsValid);

        // Дополнительный диапазон должен использовать
        // загруженный глиф, а не fallback из латиницы.
        Assert.NotEqual(
            fallbackGlyph.Texture,
            actualGlyph.Texture);
    }

    [Fact]
    public void GetGlyph_MissingCodepointInRequestedRange_ReturnsFallback()
    {
        var fontData = LoadTestFont();

        const int missingCodepoint = 0x10FFFF;

        var textures = new TestTextureManager();

        using var fonts = new OpenGLFontManager(textures);

        var font = fonts.Create(
            fontData,
            new FontDescription(18)
            {
                AdditionalCodepointRanges =
                [
                    new FontCodepointRange(
                        missingCodepoint,
                        1)
                ]
            });

        var fallbackGlyph = fonts.GetGlyph(
            font,
            '?');

        var actualGlyph = fonts.GetGlyph(
            font,
            missingCodepoint);

        Assert.Equal(
            fallbackGlyph,
            actualGlyph);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0x110000, 1)]
    [InlineData(0, 0)]
    [InlineData(0x10FFFF, 2)]
    public void FontCodepointRange_Validate_RejectsInvalidRange(
        int firstCodepoint,
        int codepointCount)
    {
        var range = new FontCodepointRange(
            firstCodepoint,
            codepointCount);

        Assert.Throws<ArgumentOutOfRangeException>(
            range.Validate);
    }

    private static int FindSupportedCodepoint(
        byte[] fontData,
        int firstCodepoint,
        int codepointCount)
    {
        using var fontInfo = StbTrueType.CreateFont(
            fontData,
            0);

        if (fontInfo is null)
        {
            throw new InvalidOperationException(
                "The test font could not be loaded.");
        }

        var endExclusive =
            firstCodepoint + codepointCount;

        for (var codepoint = firstCodepoint;
             codepoint < endExclusive;
             codepoint++)
        {
            if (StbTrueType.stbtt_FindGlyphIndex(
                    fontInfo,
                    codepoint) != 0)
            {
                return codepoint;
            }
        }

        throw new InvalidOperationException(
            "The test font contains no glyphs in " +
            "the requested Latin Extended range.");
    }

    private static byte[] LoadTestFont()
    {
        var directory = new DirectoryInfo(
            AppContext.BaseDirectory);

        while (directory is not null)
        {
            var path = Path.Combine(
                directory.FullName,
                "Game.Sandbox",
                "Assets",
                "Fonts",
                "ARIAL.TTF");

            if (File.Exists(path))
            {
                return File.ReadAllBytes(path);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "Could not find Game.Sandbox/Assets/Fonts/ARIAL.TTF.");
    }

    private sealed class TestTextureManager : ITextureManager
    {
        private uint _nextHandle = 1;

        private readonly Dictionary<
            TextureHandle,
            TextureDescription> _textures = new();

        public TextureHandle Create(
            TextureDescription description)
        {
            var handle = new TextureHandle(
                _nextHandle++);

            _textures.Add(
                handle,
                description);

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

        public void Destroy(TextureHandle texture)
        {
            _textures.Remove(texture);
        }
    }
}