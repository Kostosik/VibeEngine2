using Engine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL.Resources;

public sealed class OpenGLTextureManager
    : ITextureManager, IDisposable
{
    private readonly GL _gl;

    private readonly HashSet<uint> _textures = new();
    private readonly Dictionary<uint, TextureDescription> _descriptions = new();

    public OpenGLTextureManager(GL gl)
    {
        _gl = gl;
    }

    public TextureHandle Create(TextureDescription description)
    {
        Validate(description);

        var handle = _gl.GenTexture();

        _gl.BindTexture(
            TextureTarget.Texture2D,
            handle);

        ConfigureTexture();

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
        _gl.BindTexture(
            TextureTarget.Texture2D,
            0);

        _textures.Add(handle);
        _descriptions.Add(handle, description);

        return new TextureHandle(handle);
    }

    public TextureHandle Create(TextureData data)
    {
        Validate(data.Description);

        if (data.Format != TextureFormat.Rgba8)
        {
            throw new NotSupportedException(
                $"Texture format '{data.Format}' is not supported.");
        }

        var expectedSize =
            data.Width *
            data.Height *
            4;

        if (data.Pixels.Length != expectedSize)
        {
            throw new ArgumentException(
                $"Invalid pixel data size. " +
                $"Expected {expectedSize} bytes, " +
                $"but received {data.Pixels.Length} bytes.",
                nameof(data));
        }

        var handle = _gl.GenTexture();

        _gl.BindTexture(
            TextureTarget.Texture2D,
            handle);

        ConfigureTexture();

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

        _gl.BindTexture(
            TextureTarget.Texture2D,
            0);

        _textures.Add(handle);
        _descriptions.Add(handle, data.Description);

        return new TextureHandle(handle);
    }

    public bool Exists(TextureHandle texture)
    {
        return texture.IsValid &&
               _textures.Contains(texture.Value);
    }

    public TextureDescription GetDescription(
        TextureHandle texture)
    {
        if (!Exists(texture))
        {
            throw new KeyNotFoundException(
                $"Texture '{texture.Value}' does not exist.");
        }

        return _descriptions[texture.Value];
    }

    public void Destroy(TextureHandle texture)
    {
        if (!Exists(texture))
            return;

        _gl.DeleteTexture(texture.Value);

        _textures.Remove(texture.Value);
        _descriptions.Remove(texture.Value);
    }

    public void Dispose()
    {
        foreach (var texture in _textures)
        {
            _gl.DeleteTexture(texture);
        }

        _textures.Clear();
        _descriptions.Clear();
    }

    private void ConfigureTexture()
    {
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMinFilter,
            (int)GLEnum.Nearest);

        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMagFilter,
            (int)GLEnum.Nearest);

        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapS,
            (int)GLEnum.ClampToEdge);

        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapT,
            (int)GLEnum.ClampToEdge);
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
    }
}