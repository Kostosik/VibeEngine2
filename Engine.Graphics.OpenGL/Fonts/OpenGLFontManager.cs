using Engine.Core.Math;
using Engine.Graphics.Fonts;
using Engine.Graphics.Resources;
using StbTrueTypeSharp;
using static StbTrueTypeSharp.StbTrueType;

namespace Engine.Graphics.OpenGL.Fonts;

internal sealed class OpenGLFontManager :
    IFontManager,
    IDisposable
{
    private const int LatinFirstCodepoint = 32;
    private const int LatinCodepointCount = 224;
    private bool _disposed;
    private const int CyrillicFirstCodepoint = 0x0400;
    private const int CyrillicCodepointCount = 256;

    private readonly ITextureManager _textures;

    private readonly Dictionary<
        FontHandle,
        OpenGLFont> _fonts = new();

    private uint _nextId = 1;

    public OpenGLFontManager(
        ITextureManager textures)
    {
        ArgumentNullException.ThrowIfNull(
            textures);

        _textures = textures;

        DefaultFont =
            FontHandle.Invalid;
    }

    public FontHandle DefaultFont { get; }


    public FontHandle Create(
        ReadOnlyMemory<byte> data,
        FontDescription description)
    {
        ThrowIfDisposed();

        if (data.Length == 0)
        {
            throw new ArgumentException(
                "Font data cannot be empty.",
                nameof(data));
        }

        description.Validate();

        var fontData =
            data.ToArray();

        using var fontInfo =
            StbTrueType.CreateFont(
                fontData,
                0);

        if (fontInfo is null)
        {
            throw new ArgumentException(
                "Font data is not a valid TrueType/OpenType font.",
                nameof(data));
        }

        var glyphs =
            new Dictionary<int, FontGlyph>();

        var textures =
            new List<TextureHandle>();

        try
        {
            BakeRange(
                fontData,
                fontInfo,
                description,
                LatinFirstCodepoint,
                LatinCodepointCount,
                glyphs,
                textures);

            BakeRange(
                fontData,
                fontInfo,
                description,
                CyrillicFirstCodepoint,
                CyrillicCodepointCount,
                glyphs,
                textures);

            // U+2026 — Unicode-символ многоточия.
            const int ellipsisCodepoint = 0x2026;

            var ellipsisAlreadyIncluded =
                description.AdditionalCodepointRanges is { } ranges &&
                ranges.Any(range =>
                    ellipsisCodepoint >= range.FirstCodepoint &&
                    (long)ellipsisCodepoint <
                    (long)range.FirstCodepoint +
                    range.CodepointCount);

            if (!ellipsisAlreadyIncluded)
            {
                // Для одного дополнительного глифа не нужен
                // полноразмерный атлас основного шрифта.
                var ellipsisDescription =
                    description with
                    {
                        AtlasWidth = System.Math.Min(
                            description.AtlasWidth,
                            128),

                        AtlasHeight = System.Math.Min(
                            description.AtlasHeight,
                            128)
                    };

                BakeRange(
                    fontData,
                    fontInfo,
                    ellipsisDescription,
                    ellipsisCodepoint,
                    1,
                    glyphs,
                    textures);
            }

            if (description.AdditionalCodepointRanges
                is { } additionalRanges)
            {
                foreach (var range in additionalRanges)
                {
                    BakeRange(
                        fontData,
                        fontInfo,
                        description,
                        range.FirstCodepoint,
                        range.CodepointCount,
                        glyphs,
                        textures);
                }
            }
        }
        catch
        {
            foreach (var texture in textures)
            {
                _textures.Destroy(texture);
            }

            throw;
        }

        var handle =
            new FontHandle(
                _nextId++);

        var metrics =
            new FontMetrics(
                description.PixelHeight * 0.8f,
                -description.PixelHeight * 0.2f,
                0.0f,
                description.PixelHeight);

        var font =
            new OpenGLFont(
                handle,
                description,
                metrics,
                glyphs,
                textures);

        _fonts.Add(
            handle,
            font);

        return handle;
    }


    public bool Exists(
        FontHandle font)
    {
        ThrowIfDisposed();
        return font.IsValid &&
               _fonts.ContainsKey(font);
    }

    public FontGlyph GetGlyph(
        FontHandle font,
        int codepoint)
    {
        ThrowIfDisposed();
        if (!Exists(font))
        {
            throw new KeyNotFoundException(
                $"Font '{font.Id}' does not exist.");
        }

        var resource =
            _fonts[font];

        if (resource.Glyphs.TryGetValue(
                codepoint,
                out var glyph))
        {
            return glyph;
        }

        if (resource.Glyphs.TryGetValue(
                '?',
                out var fallback))
        {
            return fallback;
        }

        return default;
    }

    public float GetKerning(
        FontHandle font,
        int leftCodepoint,
        int rightCodepoint)
    {
        ThrowIfDisposed();
        if (!Exists(font))
        {
            throw new KeyNotFoundException(
                $"Font '{font.Id}' does not exist.");
        }

        return 0.0f;
    }

    public FontMetrics GetMetrics(
        FontHandle font)
    {
        ThrowIfDisposed();
        if (!Exists(font))
        {
            throw new KeyNotFoundException(
                $"Font '{font.Id}' does not exist.");
        }

        return _fonts[font].Metrics;
    }

    public void Destroy(
        FontHandle font)
    {
        ThrowIfDisposed();

        if (!_fonts.TryGetValue(
                font,
                out var resource))
        {
            return;
        }

        var errors = new List<Exception>();

        foreach (var texture in resource.Textures)
        {
            try
            {
                _textures.Destroy(
                    texture);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        try
        {
            resource.Dispose();
        }
        catch (Exception exception)
        {
            errors.Add(exception);
        }

        if (errors.Count > 0)
        {
            throw new AggregateException(
                $"Font '{font.Id}' could not be fully released.",
                errors);
        }

        _fonts.Remove(
            font);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        var errors = new List<Exception>();

        foreach (var font in _fonts.Values)
        {
            foreach (var texture in font.Textures)
            {
                try
                {
                    _textures.Destroy(texture);
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }

            try
            {
                font.Dispose();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        _fonts.Clear();
        _disposed = true;

        if (errors.Count > 0)
        {
            throw new AggregateException(
                "One or more OpenGL font resources could not be released.",
                errors);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }

    private void BakeRange(
        byte[] fontData,
        stbtt_fontinfo fontInfo,
        FontDescription description,
        int firstCodepoint,
        int codepointCount,
        Dictionary<int, FontGlyph> glyphs,
        List<TextureHandle> textures)
    {
        var width =
            description.AtlasWidth;

        var height =
            description.AtlasHeight;

        while (true)
        {
            var bitmap =
                new byte[
                    width *
                    height];

            var bakedChars =
                new StbTrueType.stbtt_bakedchar[
                    codepointCount];

            var result =
                StbTrueType.stbtt_BakeFontBitmap(
                    fontData,
                    0,
                    description.PixelHeight,
                    bitmap,
                    width,
                    height,
                    firstCodepoint,
                    codepointCount,
                    bakedChars);

            if (result)
            {
                CreateAtlas(
                    fontInfo,
                    bitmap,
                    width,
                    height,
                    firstCodepoint,
                    bakedChars,
                    glyphs,
                    textures);

                return;
            }

            if (width >= 4096 &&
                height >= 4096)
            {
                throw new InvalidOperationException(
                    $"Font atlas is too small for " +
                    $"range U+{firstCodepoint:X4}..U+" +
                    $"{firstCodepoint + codepointCount - 1:X4}.");
            }

            if (width <= height)
            {
                width *= 2;
            }
            else
            {
                height *= 2;
            }
        }
    }

    public FontTextMetrics MeasureText(
    FontHandle font,
    string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (!Exists(font))
        {
            throw new KeyNotFoundException(
                $"Font '{font.Id}' does not exist.");
        }

        var metrics =
            GetMetrics(font);

        if (text.Length == 0)
        {
            return new FontTextMetrics(
                0.0f,
                metrics.LineHeight,
                metrics.Ascent,
                metrics.Descent,
                metrics.LineHeight);
        }

        var maximumWidth = 0.0f;
        var currentWidth = 0.0f;
        var lineCount = 1;
        var previousCodepoint = -1;

        foreach (var rune in text.EnumerateRunes())
        {
            if (rune.Value == '\n')
            {
                maximumWidth =
                    MathF.Max(
                        maximumWidth,
                        currentWidth);

                currentWidth = 0.0f;
                lineCount++;
                previousCodepoint = -1;

                continue;
            }

            if (rune.Value == '\r')
            {
                continue;
            }

            var codepoint =
                rune.Value;

            if (previousCodepoint >= 0)
            {
                currentWidth +=
                    GetKerning(
                        font,
                        previousCodepoint,
                        codepoint);
            }

            var glyph =
                GetGlyph(
                    font,
                    codepoint);

            currentWidth +=
                glyph.Advance;

            previousCodepoint =
                codepoint;
        }

        maximumWidth =
            MathF.Max(
                maximumWidth,
                currentWidth);

        return new FontTextMetrics(
            maximumWidth,
            metrics.LineHeight * lineCount,
            metrics.Ascent,
            metrics.Descent,
            metrics.LineHeight);
    }

    private void CreateAtlas(
        stbtt_fontinfo fontInfo,
        byte[] bitmap,
        int width,
        int height,
        int firstCodepoint,
        stbtt_bakedchar[] bakedChars,
        Dictionary<int, FontGlyph> glyphs,
        List<TextureHandle> textures)
    {
        var hasSupportedGlyph = false;

        for (var i = 0; i < bakedChars.Length; i++)
        {
            var codepoint = firstCodepoint + i;

            if (stbtt_FindGlyphIndex(fontInfo, codepoint) != 0)
            {
                hasSupportedGlyph = true;
                break;
            }
        }

        if (!hasSupportedGlyph)
        {
            return;
        }

        var rgba =
            ConvertToRgba(
                bitmap);

        var texture =
            _textures.Create(
                new TextureData(
                    width,
                    height,
                    TextureFormat.Rgba8,
                    rgba));

        textures.Add(
            texture);

        for (var i = 0;
             i < bakedChars.Length;
             i++)
        {
            var baked =
                bakedChars[i];

            var codepoint =
                firstCodepoint + i;

            if (stbtt_FindGlyphIndex(fontInfo, codepoint) == 0)
            {
                continue;
            }

            var glyphWidth =
                baked.x1 -
                baked.x0;

            var glyphHeight =
                baked.y1 -
                baked.y0;

            var uv =
                new Rectangle(
                    baked.x0 / (float)width,
                    baked.y0 / (float)height,
                    glyphWidth / (float)width,
                    glyphHeight / (float)height);

            glyphs[codepoint] =
                new FontGlyph(
                    texture,
                    uv,
                    new Vector2(
                        glyphWidth,
                        glyphHeight),
                    new Vector2(
                        baked.xoff,
                        baked.yoff),
                    baked.xadvance);
        }
    }

    private static byte[] ConvertToRgba(
        byte[] bitmap)
    {
        var rgba =
            new byte[
                bitmap.Length * 4];

        for (var i = 0;
             i < bitmap.Length;
             i++)
        {
            var alpha =
                bitmap[i];

            var offset =
                i * 4;

            rgba[offset] = 255;
            rgba[offset + 1] = 255;
            rgba[offset + 2] = 255;
            rgba[offset + 3] = alpha;
        }

        return rgba;
    }
}