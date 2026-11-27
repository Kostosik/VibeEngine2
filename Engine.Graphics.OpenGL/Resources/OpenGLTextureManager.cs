using Engine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL.Resources;

public sealed class OpenGLTextureManager
    : ITextureManager, IDisposable
{
    private sealed class Entry
    {
        public required uint NativeHandle { get; init; }

        public required TextureDescription Description { get; init; }
    }


    private readonly GL _gl;
    private bool _disposed;
    private readonly Dictionary<TextureHandle, Entry> _textures = new();

    private uint _nextHandle = 1;
    private const uint TextureMaxAnisotropyExt = 0x84FE;
    private const uint MaxTextureMaxAnisotropyExt = 0x84FF;

    public bool SupportsAnisotropicFiltering { get; }

    public float MaxAnisotropy { get; }
    public OpenGLTextureManager(GL gl)
    {
        _gl = gl;

        bool supportsExt =
    _gl.IsExtensionPresent("GL_EXT_texture_filter_anisotropic");

        bool supportsArb =
            _gl.IsExtensionPresent("GL_ARB_texture_filter_anisotropic");

        SupportsAnisotropicFiltering = supportsExt || supportsArb;

        if (SupportsAnisotropicFiltering)
        {
            _gl.GetFloat(
                (GLEnum)MaxTextureMaxAnisotropyExt,
                out float maxAnisotropy);

            MaxAnisotropy = MathF.Max(1.0f, maxAnisotropy);
        }
        else
        {
            MaxAnisotropy = 1.0f;
        }
    }

    public TextureHandle Create(TextureDescription description)
    {
        ThrowIfDisposed();
        if (description.GenerateMipmaps)
        {
            throw new InvalidOperationException(
                "Mipmaps require pixel data. Use Create(TextureData) instead.");
        }
        Validate(description);
        var format = GetOpenGlFormat(description);
        var handle = _gl.GenTexture();

        if (handle == 0)
        {
            throw new InvalidOperationException(
                "OpenGL failed to create a texture.");
        }
        TextureHandle resourceHandle = TextureHandle.Invalid;
        try
        {
            _gl.BindTexture(
                TextureTarget.Texture2D,
                handle);

            ConfigureTexture(description.EffectiveSampler);

            unsafe
            {
                _gl.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    format.InternalFormat,
                    (uint)description.Width,
                    (uint)description.Height,
                    0,
                    PixelFormat.Rgba,
                    format.PixelType,
                    null);
            }

            resourceHandle = CreateHandle();

            _textures.Add(
                resourceHandle,
                new Entry
                {
                    NativeHandle = handle,
                    Description = description
                });

            return resourceHandle;
        }
        catch
        {
            if (resourceHandle.IsValid)
            {
                _textures.Remove(resourceHandle);
            }

            _gl.DeleteTexture(handle);

            throw;
        }
        finally
        {
            _gl.BindTexture(
                TextureTarget.Texture2D,
                0);
        }
    }

    private static (
        InternalFormat InternalFormat,
        PixelType PixelType,
        int BytesPerPixel) GetOpenGlFormat(
            TextureDescription description)
    {
        return (description.Format, description.ColorSpace) switch
        {
            (TextureFormat.Rgba8, TextureColorSpace.Linear) =>
                (InternalFormat.Rgba8, PixelType.UnsignedByte, 4),

            (TextureFormat.Rgba8, TextureColorSpace.Srgb) =>
                (InternalFormat.Srgb8Alpha8, PixelType.UnsignedByte, 4),

            (TextureFormat.Rgba16Float, TextureColorSpace.Linear) =>
                (InternalFormat.Rgba16f, PixelType.HalfFloat, 8),

            _ => throw new ArgumentException(
                "Unsupported texture format and color space combination.",
                nameof(description))
        };
    }

    public TextureHandle Create(TextureData data)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(data);

        Validate(data.Description);
        var format = GetOpenGlFormat(data.Description);
        var expectedSize =
            (long)data.Width * data.Height * format.BytesPerPixel;

        if (data.Pixels.Length != expectedSize)
        {
            throw new ArgumentException(
                $"Invalid pixel data size. " +
                $"Expected {expectedSize} bytes, " +
                $"but received {data.Pixels.Length} bytes.",
                nameof(data));
        }

        var handle = _gl.GenTexture();

        if (handle == 0)
        {
            throw new InvalidOperationException(
                "OpenGL failed to create a texture.");
        }
        TextureHandle resourceHandle = TextureHandle.Invalid;
        try
        {
            _gl.BindTexture(
                TextureTarget.Texture2D,
                handle);

            ConfigureTexture(data.Description.EffectiveSampler);

            var pixels = data.Pixels.Span;

            unsafe
            {
                fixed (byte* ptr = pixels)
                {
                    _gl.TexImage2D(
                        TextureTarget.Texture2D,
                        0,
                        format.InternalFormat,
                        (uint)data.Width,
                        (uint)data.Height,
                        0,
                        PixelFormat.Rgba,
                        format.PixelType,
                        ptr);
                }
            }
            if (data.Description.GenerateMipmaps)
            {
                _gl.GenerateMipmap(TextureTarget.Texture2D);
            }
            resourceHandle = CreateHandle();

            _textures.Add(
                resourceHandle,
                new Entry
                {
                    NativeHandle = handle,
                    Description = data.Description
                });

            return resourceHandle;
        }
        catch
        {
            if (resourceHandle.IsValid)
            {
                _textures.Remove(resourceHandle);
            }

            _gl.DeleteTexture(handle);
            throw;
        }
        finally
        {
            _gl.BindTexture(
                TextureTarget.Texture2D,
                0);
        }
    }

    public bool Exists(TextureHandle texture)
    {
        ThrowIfDisposed();

        return texture.IsValid &&
               _textures.ContainsKey(texture);
    }

    public TextureDescription GetDescription(TextureHandle texture)
    {
        ThrowIfDisposed();

        if (!texture.IsValid ||
            !_textures.TryGetValue(texture, out var entry))
        {
            throw new KeyNotFoundException(
                $"Texture '{texture.Value}' does not exist.");
        }

        return entry.Description;
    }

    public void Destroy(TextureHandle texture)
    {
        ThrowIfDisposed();

        if (!_textures.TryGetValue(texture, out var entry))
            return;

        _gl.DeleteTexture(entry.NativeHandle);

        _textures.Remove(texture);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(
                nameof(OpenGLTextureManager));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        var errors = new List<Exception>();

        foreach (var entry in _textures.Values)
        {
            try
            {
                _gl.DeleteTexture(entry.NativeHandle);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        _textures.Clear();
        _disposed = true;

        if (errors.Count > 0)
        {
            throw new AggregateException(
                "One or more OpenGL textures could not be released.",
                errors);
        }
    }

    internal bool TryGetNativeHandle(
    TextureHandle texture,
    out uint nativeHandle)
    {
        ThrowIfDisposed();

        if (texture.IsValid &&
            _textures.TryGetValue(texture, out var entry))
        {
            nativeHandle = entry.NativeHandle;
            return true;
        }

        nativeHandle = 0;
        return false;
    }

    private TextureHandle CreateHandle()
    {
        if (_nextHandle == 0)
        {
            throw new InvalidOperationException(
                "Texture handle space is exhausted.");
        }

        var value = _nextHandle;

        _nextHandle = value == uint.MaxValue
            ? 0
            : value + 1;

        return new TextureHandle(value);
    }

    internal uint GetNativeHandle(TextureHandle texture)
    {
        if (!TryGetNativeHandle(texture, out var nativeHandle))
        {
            throw new KeyNotFoundException(
                $"Texture '{texture.Value}' does not exist.");
        }

        return nativeHandle;
    }

    private void ConfigureTexture(TextureSamplerDescription sampler)
    {
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMinFilter,
            ToOpenGlFilter(sampler.Minification));

        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMagFilter,
            ToOpenGlFilter(sampler.Magnification));

        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapS,
            ToOpenGlWrapMode(sampler.WrapS));

        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapT,
            ToOpenGlWrapMode(sampler.WrapT));

        if (sampler.Anisotropy > 1.0f)
        {
            if (!SupportsAnisotropicFiltering)
            {
                throw new NotSupportedException(
                    "Anisotropic filtering is not supported by this OpenGL device.");
            }

            float anisotropy = MathF.Min(
                sampler.Anisotropy,
                MaxAnisotropy);

            _gl.TexParameter(
                TextureTarget.Texture2D,
                (TextureParameterName)TextureMaxAnisotropyExt,
                anisotropy);
        }
    }

    private static int ToOpenGlFilter(TextureFilter filter)
    {
        return filter switch
        {
            TextureFilter.Nearest =>
                (int)GLEnum.Nearest,

            TextureFilter.Linear =>
                (int)GLEnum.Linear,

            TextureFilter.NearestMipmapNearest =>
                (int)GLEnum.NearestMipmapNearest,

            TextureFilter.LinearMipmapNearest =>
                (int)GLEnum.LinearMipmapNearest,

            TextureFilter.NearestMipmapLinear =>
                (int)GLEnum.NearestMipmapLinear,

            TextureFilter.LinearMipmapLinear =>
                (int)GLEnum.LinearMipmapLinear,

            _ => throw new ArgumentOutOfRangeException(nameof(filter))
        };
    }

    private static int ToOpenGlWrapMode(
        Engine.Graphics.Resources.TextureWrapMode mode)
    {
        return mode switch
        {
            Engine.Graphics.Resources.TextureWrapMode.ClampToEdge =>
                (int)GLEnum.ClampToEdge,

            Engine.Graphics.Resources.TextureWrapMode.Repeat =>
                (int)GLEnum.Repeat,

            Engine.Graphics.Resources.TextureWrapMode.MirroredRepeat =>
                (int)GLEnum.MirroredRepeat,

            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }

    private static void ValidateSampler(
        TextureSamplerDescription sampler,
        bool generateMipmaps)
    {
        if (!Enum.IsDefined(sampler.Minification))
            throw new ArgumentOutOfRangeException(nameof(sampler));

        if (!float.IsFinite(sampler.Anisotropy) ||
    sampler.Anisotropy < 1.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sampler),
                "Anisotropy must be a finite value greater than or equal to 1.");
        }

        if (sampler.Magnification is not TextureFilter.Nearest
            and not TextureFilter.Linear)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sampler),
                "Magnification supports only Nearest and Linear filters.");
        }

        if (!Enum.IsDefined(sampler.WrapS))
            throw new ArgumentOutOfRangeException(nameof(sampler));

        if (!Enum.IsDefined(sampler.WrapT))
            throw new ArgumentOutOfRangeException(nameof(sampler));

        var usesMipmaps = sampler.Minification is
            TextureFilter.NearestMipmapNearest or
            TextureFilter.LinearMipmapNearest or
            TextureFilter.NearestMipmapLinear or
            TextureFilter.LinearMipmapLinear;

        if (usesMipmaps && !generateMipmaps)
        {
            throw new ArgumentException(
                "Mipmap filtering requires GenerateMipmaps to be enabled.",
                nameof(sampler));
        }
    }

    private static void Validate(
        TextureDescription description)
    {
        if (description.Width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(description.Width));
        }

        if (description.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(description.Height));
        }
        if (!Enum.IsDefined(description.Format))
        {
            throw new ArgumentOutOfRangeException(
                nameof(description.Format),
                description.Format,
                "Unsupported texture format.");
        }

        if (!Enum.IsDefined(description.ColorSpace))
        {
            throw new ArgumentOutOfRangeException(
                nameof(description.ColorSpace),
                description.ColorSpace,
                "Unsupported texture color space.");
        }

        if (description.ColorSpace == TextureColorSpace.Srgb &&
            description.Format != TextureFormat.Rgba8)
        {
            throw new ArgumentException(
                "sRGB color space is supported only for Rgba8 textures.",
                nameof(description));
        }

        ValidateSampler(
            description.EffectiveSampler,
            description.GenerateMipmaps);
    }
}