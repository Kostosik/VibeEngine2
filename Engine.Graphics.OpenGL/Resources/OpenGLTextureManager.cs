using Engine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL.Resources;

public sealed class OpenGLTextureManager
    : ITextureManager, IDisposable
{
    private readonly GL _gl;
    private bool _disposed;
    private readonly HashSet<uint> _textures = new();
    private readonly Dictionary<uint, TextureDescription> _descriptions = new();
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

        var handle = _gl.GenTexture();

        if (handle == 0)
        {
            throw new InvalidOperationException(
                "OpenGL failed to create a texture.");
        }

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
                    InternalFormat.Rgba8,
                    (uint)description.Width,
                    (uint)description.Height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    null);
            }

            _textures.Add(handle);
            _descriptions.Add(handle, description);

            return new TextureHandle(handle);
        }
        catch
        {
            _textures.Remove(handle);
            _descriptions.Remove(handle);

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

    public TextureHandle Create(TextureData data)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(data);

        Validate(data.Description);

        if (data.Format != TextureFormat.Rgba8)
        {
            throw new NotSupportedException(
                $"Texture format '{data.Format}' is not supported.");
        }

        var expectedSize =
            (long)data.Width * data.Height * 4;

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
                        InternalFormat.Rgba8,
                        (uint)data.Width,
                        (uint)data.Height,
                        0,
                        PixelFormat.Rgba,
                        PixelType.UnsignedByte,
                        ptr);
                }
            }
            if (data.Description.GenerateMipmaps)
            {
                _gl.GenerateMipmap(TextureTarget.Texture2D);
            }
            _textures.Add(handle);
            _descriptions.Add(handle, data.Description);

            return new TextureHandle(handle);
        }
        catch
        {
            _textures.Remove(handle);
            _descriptions.Remove(handle);

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
               _textures.Contains(texture.Value);
    }

    public TextureDescription GetDescription(
        TextureHandle texture)
    {
        ThrowIfDisposed();
        if (!Exists(texture))
        {
            throw new KeyNotFoundException(
                $"Texture '{texture.Value}' does not exist.");
        }

        return _descriptions[texture.Value];
    }

    public void Destroy(TextureHandle texture)
    {
        ThrowIfDisposed();
        if (!Exists(texture))
            return;

        _gl.DeleteTexture(texture.Value);

        _textures.Remove(texture.Value);
        _descriptions.Remove(texture.Value);
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

        foreach (var texture in _textures)
        {
            try
            {
                _gl.DeleteTexture(
                    texture);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        _textures.Clear();
        _descriptions.Clear();
        _disposed = true;

        if (errors.Count > 0)
        {
            throw new AggregateException(
                "One or more OpenGL textures could not be released.",
                errors);
        }
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
        ValidateSampler(
            description.EffectiveSampler,
            description.GenerateMipmaps);
    }
}