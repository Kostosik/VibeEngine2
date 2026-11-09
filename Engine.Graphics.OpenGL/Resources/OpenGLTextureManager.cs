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

    public OpenGLTextureManager(GL gl)
    {
        _gl = gl;
    }

    public TextureHandle Create(TextureDescription description)
    {
        ThrowIfDisposed();
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
    }

    private static int ToOpenGlFilter(TextureFilter filter)
    {
        return filter switch
        {
            TextureFilter.Nearest => (int)GLEnum.Nearest,
            TextureFilter.Linear => (int)GLEnum.Linear,
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

    private static void ValidateSampler(TextureSamplerDescription sampler)
    {
        if (!Enum.IsDefined(sampler.Minification))
            throw new ArgumentOutOfRangeException(nameof(sampler));

        if (!Enum.IsDefined(sampler.Magnification))
            throw new ArgumentOutOfRangeException(nameof(sampler));

        if (!Enum.IsDefined(sampler.WrapS))
            throw new ArgumentOutOfRangeException(nameof(sampler));

        if (!Enum.IsDefined(sampler.WrapT))
            throw new ArgumentOutOfRangeException(nameof(sampler));
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
        ValidateSampler(description.EffectiveSampler);
    }
}