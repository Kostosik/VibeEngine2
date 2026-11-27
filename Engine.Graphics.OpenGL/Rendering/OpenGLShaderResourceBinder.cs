using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;
using Engine.Graphics.OpenGL.Resources;
using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL.Rendering;

/// <summary>
/// OpenGL implementation of named shader resource binding.
/// Manages texture units and caches sampler objects.
/// </summary>
internal sealed class OpenGLShaderResourceBinder :
    IShaderResourceBinder,
    IDisposable
{
    private const uint TextureMaxAnisotropyExt = 0x84FE;
    private const uint MaxTextureMaxAnisotropyExt = 0x84FF;
    private readonly OpenGLTextureManager _textures;
    private readonly GL _gl;
    private readonly int _maxTextureUnits;

    private readonly Dictionary<string, int> _textureUnits =
        new(StringComparer.Ordinal);

    private readonly HashSet<string> _configuredSamplers =
        new(StringComparer.Ordinal);

    private readonly List<int> _activeUnits = [];

    private readonly Dictionary<TextureSamplerDescription, uint> _samplers = [];

    private readonly bool _supportsAnisotropy;
    private readonly float _maxAnisotropy;

    private OpenGLShaderProgram? _shader;
    private int _nextTextureUnit;
    private bool _disposed;

    public OpenGLShaderResourceBinder(GL gl, OpenGLTextureManager textures)
    {
        ArgumentNullException.ThrowIfNull(gl);
        ArgumentNullException.ThrowIfNull(textures);
        _gl = gl;

        _gl.GetInteger(
            GLEnum.MaxTextureImageUnits,
            out _maxTextureUnits);

        if (_maxTextureUnits <= 0)
        {
            throw new NotSupportedException(
                "OpenGL does not expose fragment texture units.");
        }

        _supportsAnisotropy =
            _gl.IsExtensionPresent("GL_EXT_texture_filter_anisotropic") ||
            _gl.IsExtensionPresent("GL_ARB_texture_filter_anisotropic");

        if (_supportsAnisotropy)
        {
            _gl.GetFloat(
                (GLEnum)MaxTextureMaxAnisotropyExt,
                out float maxAnisotropy);

            _maxAnisotropy = MathF.Max(1.0f, maxAnisotropy);
        }
        else
        {
            _maxAnisotropy = 1.0f;
        }
        _textures = textures;
    }

    public void Begin(OpenGLShaderProgram shader)
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(shader);

        if (_shader is not null)
        {
            throw new InvalidOperationException(
                "Shader resource binding is already active.");
        }

        _ = shader.Handle;

        _shader = shader;
        _nextTextureUnit = 0;
    }

    public void SetTexture(string name, TextureHandle texture)
    {
        EnsureActive();

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!texture.IsValid)
        {
            throw new ArgumentException(
                "Texture handle is invalid.",
                nameof(texture));
        }

        if (!_textures.TryGetNativeHandle(
        texture,
        out var nativeTexture))
        {
            throw new KeyNotFoundException(
                $"Texture '{texture.Value}' does not exist.");
        }

        if (_textureUnits.ContainsKey(name))
        {
            throw new InvalidOperationException(
                $"Texture '{name}' is already bound.");
        }

        if (_nextTextureUnit >= _maxTextureUnits)
        {
            throw new NotSupportedException(
                $"Fullscreen pass exceeds the available " +
                $"{_maxTextureUnits} fragment texture units.");
        }

        var location = _shader!.GetUniformLocation(name);

        if (location < 0)
        {
            throw new InvalidOperationException(
                $"Shader texture uniform '{name}' was not found.");
        }

        var unit = _nextTextureUnit++;

        _textureUnits.Add(name, unit);
        _activeUnits.Add(unit);

        _gl.ActiveTexture(
            (TextureUnit)((int)TextureUnit.Texture0 + unit));

        _gl.BindTexture(
            TextureTarget.Texture2D,
            nativeTexture);

        _gl.Uniform1(location, unit);
    }

    public void SetSampler(
        string name,
        TextureSamplerDescription sampler)
    {
        EnsureActive();

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!_textureUnits.TryGetValue(name, out var unit))
        {
            throw new InvalidOperationException(
                $"Texture '{name}' must be bound before its sampler.");
        }

        if (!_configuredSamplers.Add(name))
        {
            throw new InvalidOperationException(
                $"Sampler for '{name}' is already configured.");
        }

        var samplerHandle = GetOrCreateSampler(sampler);

        _gl.BindSampler(
            (uint)unit,
            samplerHandle);
    }

    private uint GetOrCreateSampler(
        TextureSamplerDescription description)
    {
        ValidateSampler(description);

        if (_samplers.TryGetValue(description, out var existing))
        {
            return existing;
        }

        var sampler = _gl.GenSampler();

        if (sampler == 0)
        {
            throw new InvalidOperationException(
                "OpenGL failed to create a sampler object.");
        }

        try
        {
            _gl.SamplerParameter(
                sampler,
                GLEnum.TextureMinFilter,
                ToOpenGlFilter(description.Minification));

            _gl.SamplerParameter(
                sampler,
                GLEnum.TextureMagFilter,
                ToOpenGlFilter(description.Magnification));

            _gl.SamplerParameter(
                sampler,
                GLEnum.TextureWrapS,
                ToOpenGlWrapMode(description.WrapS));

            _gl.SamplerParameter(
                sampler,
                GLEnum.TextureWrapT,
                ToOpenGlWrapMode(description.WrapT));

            if (description.Anisotropy > 1.0f)
            {
                _gl.SamplerParameter(
                    sampler,
                    SamplerParameterF.MaxAnisotropy,
                    MathF.Min(
                        description.Anisotropy,
                        _maxAnisotropy));
            }

            _samplers.Add(description, sampler);

            return sampler;
        }
        catch
        {
            _gl.DeleteSampler(sampler);
            throw;
        }
    }

    private void ValidateSampler(
        TextureSamplerDescription sampler)
    {
        if (!Enum.IsDefined(sampler.Minification) ||
            !Enum.IsDefined(sampler.Magnification) ||
            !Enum.IsDefined(sampler.WrapS) ||
            !Enum.IsDefined(sampler.WrapT))
        {
            throw new ArgumentOutOfRangeException(
                nameof(sampler),
                "Sampler contains unsupported parameters.");
        }

        if (sampler.Magnification is not TextureFilter.Nearest
            and not TextureFilter.Linear)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sampler),
                "Magnification supports only Nearest and Linear.");
        }

        if (!float.IsFinite(sampler.Anisotropy) ||
            sampler.Anisotropy < 1.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sampler),
                "Anisotropy must be finite and at least 1.");
        }

        if (sampler.Anisotropy > 1.0f &&
            !_supportsAnisotropy)
        {
            throw new NotSupportedException(
                "Anisotropic filtering is not supported.");
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

    public void End()
    {
        if (_shader is null)
            return;

        try
        {
            foreach (var unit in _activeUnits)
            {
                _gl.BindSampler((uint)unit, 0);

                _gl.ActiveTexture(
                    (TextureUnit)((int)TextureUnit.Texture0 + unit));

                _gl.BindTexture(
                    TextureTarget.Texture2D,
                    0);
            }
        }
        finally
        {
            _gl.ActiveTexture(TextureUnit.Texture0);

            _textureUnits.Clear();
            _configuredSamplers.Clear();
            _activeUnits.Clear();

            _nextTextureUnit = 0;
            _shader = null;
        }
    }

    private void EnsureActive()
    {
        ThrowIfDisposed();

        if (_shader is null)
        {
            throw new InvalidOperationException(
                "Shader resource binding has not been started.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        var errors = new List<Exception>();

        try
        {
            End();
        }
        catch (Exception exception)
        {
            errors.Add(exception);
        }

        foreach (var sampler in _samplers.Values)
        {
            try
            {
                _gl.DeleteSampler(sampler);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        _samplers.Clear();
        _disposed = true;

        if (errors.Count > 0)
        {
            throw new AggregateException(
                "OpenGL sampler resources could not be fully released.",
                errors);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}