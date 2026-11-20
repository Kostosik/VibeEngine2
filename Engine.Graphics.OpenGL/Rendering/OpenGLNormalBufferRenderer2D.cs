using Engine.Camera;
using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.Graphics.OpenGL.Resources;
using Engine.Graphics2D.Commands;
using Engine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL.Rendering;

internal sealed class OpenGLNormalBufferRenderer2D : IDisposable
{
    private readonly GL _gl;
    private readonly Camera2D _camera;
    private readonly OpenGLTextureManager _textures;
    private readonly OpenGLShaderProgram _shader;

    private readonly uint _vao;
    private readonly uint _vbo;

    private readonly float[] _vertices = new float[6 * 4];

    private readonly int _resolutionLocation;
    private readonly int _albedoLocation;
    private readonly int _normalMapLocation;
    private readonly int _hasNormalMapLocation;
    private readonly int _normalStrengthLocation;
    private readonly int _normalTransform0Location;
    private readonly int _normalTransform1Location;

    private float _width;
    private float _height;

    private bool _frameActive;
    private bool _disposed;

    public OpenGLNormalBufferRenderer2D(
        GL gl,
        int width,
        int height,
        Camera2D camera,
        OpenGLTextureManager textures)
    {
        ArgumentNullException.ThrowIfNull(gl);
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(textures);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        _gl = gl;
        _camera = camera;
        _textures = textures;

        _width = width;
        _height = height;

        _shader = new OpenGLShaderProgram(
            gl,
            VertexShaderSource,
            FragmentShaderSource);

        try
        {
            _resolutionLocation =
                _shader.GetUniformLocation("uResolution");

            _albedoLocation =
                _shader.GetUniformLocation("uAlbedo");

            _normalMapLocation =
                _shader.GetUniformLocation("uNormalMap");

            _hasNormalMapLocation =
                _shader.GetUniformLocation("uHasNormalMap");

            _normalStrengthLocation =
                _shader.GetUniformLocation("uNormalStrength");

            _normalTransform0Location =
                _shader.GetUniformLocation("uNormalTransform0");

            _normalTransform1Location =
                _shader.GetUniformLocation("uNormalTransform1");

            ValidateUniforms();

            _shader.Use();

            _gl.Uniform1(_albedoLocation, 0);
            _gl.Uniform1(_normalMapLocation, 1);

            _shader.StopUsing();

            (_vao, _vbo) = CreateBuffers();
        }
        catch
        {
            try
            {
                if (_vbo != 0)
                    _gl.DeleteBuffer(_vbo);

                if (_vao != 0)
                    _gl.DeleteVertexArray(_vao);
            }
            catch
            {
                // Сохраняем исходное исключение.
            }

            _shader.Dispose();
            throw;
        }
    }

    public void Begin()
    {
        ThrowIfDisposed();

        if (_frameActive)
        {
            throw new InvalidOperationException(
                "Normal buffer rendering has already begun.");
        }

        _shader.Use();

        _gl.Uniform2(
            _resolutionLocation,
            _width,
            _height);

        _gl.BindVertexArray(_vao);

        _frameActive = true;
    }

    public void DrawWorld(DrawWorldTextureCommand command)
    {
        DrawWorld(
            new DrawWorldTextureData(
                command.Texture,
                command.Position,
                command.Size,
                command.UV,
                command.Layer,
                command.RotationDegrees)
            {
                Material = command.Material,
                NormalTransform = command.NormalTransform
            });
    }

    public void DrawWorld(DrawWorldTextureData command)
    {
        ThrowIfDisposed();

        if (!_frameActive)
        {
            throw new InvalidOperationException(
                "Normal buffer rendering has not begun.");
        }

        if (!_textures.Exists(command.Texture))
            return;

        var normalTexture = command.Material.Normal;
        var hasNormalMap =
            normalTexture.IsValid &&
            _textures.Exists(normalTexture);

        var worldTopLeft =
            command.Position +
            new Vector2(0.0f, command.Size.Y);

        var screenTopLeft =
            _camera.WorldToScreen(worldTopLeft);

        var screenSize =
            command.Size * _camera.Zoom;

        if (!float.IsFinite(screenTopLeft.X) ||
            !float.IsFinite(screenTopLeft.Y) ||
            !float.IsFinite(screenSize.X) ||
            !float.IsFinite(screenSize.Y) ||
            screenSize.X <= 0.0f ||
            screenSize.Y <= 0.0f)
        {
            return;
        }

        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(
            TextureTarget.Texture2D,
            command.Texture.Value);

        _gl.ActiveTexture(TextureUnit.Texture1);
        _gl.BindTexture(
            TextureTarget.Texture2D,
            hasNormalMap ? normalTexture.Value : 0);

        _gl.ActiveTexture(TextureUnit.Texture0);

        var parameters = command.Material.Parameters;
        var transform = command.NormalTransform;

        _gl.Uniform1(
            _hasNormalMapLocation,
            hasNormalMap ? 1 : 0);

        _gl.Uniform1(
            _normalStrengthLocation,
            parameters.NormalStrength);

        // Столбцы матрицы преобразования tangent -> world.
        _gl.Uniform2(
            _normalTransform0Location,
            transform.M11,
            transform.M21);

        _gl.Uniform2(
            _normalTransform1Location,
            transform.M12,
            transform.M22);

        AddQuad(
            screenTopLeft,
            screenSize,
            command.UV,
            command.RotationDegrees);

        _gl.BindBuffer(
            BufferTargetARB.ArrayBuffer,
            _vbo);

        _gl.BufferData<float>(
            BufferTargetARB.ArrayBuffer,
            _vertices.AsSpan(),
            BufferUsageARB.DynamicDraw);

        _gl.DrawArrays(
            PrimitiveType.Triangles,
            0,
            6);

        _gl.BindBuffer(
            BufferTargetARB.ArrayBuffer,
            0);
    }

    public void End()
    {
        ThrowIfDisposed();

        if (!_frameActive)
        {
            throw new InvalidOperationException(
                "Normal buffer rendering has not begun.");
        }

        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, 0);

        _gl.ActiveTexture(TextureUnit.Texture1);
        _gl.BindTexture(TextureTarget.Texture2D, 0);

        _gl.ActiveTexture(TextureUnit.Texture0);

        _gl.BindBuffer(
            BufferTargetARB.ArrayBuffer,
            0);

        _gl.BindVertexArray(0);

        _shader.StopUsing();

        _frameActive = false;
    }

    public void Resize(int width, int height)
    {
        ThrowIfDisposed();

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        _width = width;
        _height = height;
    }

    private void AddQuad(
        Vector2 position,
        Vector2 size,
        Rectangle uv,
        float rotationDegrees)
    {
        var right = position.X + size.X;
        var bottom = position.Y + size.Y;

        var origin = new Vector2(
            position.X + size.X * 0.5f,
            position.Y + size.Y * 0.5f);

        var topLeft = RotatePoint(
            new Vector2(position.X, position.Y),
            origin,
            rotationDegrees);

        var topRight = RotatePoint(
            new Vector2(right, position.Y),
            origin,
            rotationDegrees);

        var bottomRight = RotatePoint(
            new Vector2(right, bottom),
            origin,
            rotationDegrees);

        var bottomLeft = RotatePoint(
            new Vector2(position.X, bottom),
            origin,
            rotationDegrees);

        SetVertex(0, topLeft, new Vector2(uv.X, uv.Y));
        SetVertex(1, topRight, new Vector2(uv.X + uv.Width, uv.Y));
        SetVertex(2, bottomRight, new Vector2(uv.X + uv.Width, uv.Y + uv.Height));

        SetVertex(3, topLeft, new Vector2(uv.X, uv.Y));
        SetVertex(4, bottomRight, new Vector2(uv.X + uv.Width, uv.Y + uv.Height));
        SetVertex(5, bottomLeft, new Vector2(uv.X, uv.Y + uv.Height));
    }

    private void SetVertex(
        int index,
        Vector2 position,
        Vector2 uv)
    {
        var offset = index * 4;

        _vertices[offset] = position.X;
        _vertices[offset + 1] = position.Y;
        _vertices[offset + 2] = uv.X;
        _vertices[offset + 3] = uv.Y;
    }

    private static Vector2 RotatePoint(
        Vector2 point,
        Vector2 origin,
        float rotationDegrees)
    {
        if (MathF.Abs(rotationDegrees) < 0.0001f)
            return point;

        var radians = rotationDegrees * (MathF.PI / 180.0f);
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);

        var dx = point.X - origin.X;
        var dy = point.Y - origin.Y;

        return new Vector2(
            origin.X + dx * cos - dy * sin,
            origin.Y + dx * sin + dy * cos);
    }

    private (uint Vao, uint Vbo) CreateBuffers()
    {
        uint vao = 0;
        uint vbo = 0;

        try
        {
            vao = _gl.GenVertexArray();
            vbo = _gl.GenBuffer();

            if (vao == 0 || vbo == 0)
            {
                throw new InvalidOperationException(
                    "OpenGL failed to create normal-buffer geometry.");
            }

            _gl.BindVertexArray(vao);

            _gl.BindBuffer(
                BufferTargetARB.ArrayBuffer,
                vbo);

            _gl.BufferData<float>(
                BufferTargetARB.ArrayBuffer,
                _vertices,
                BufferUsageARB.DynamicDraw);

            const uint stride = 4 * sizeof(float);

            _gl.VertexAttribPointer(
                0, 2, VertexAttribPointerType.Float,
                false, stride, 0);

            _gl.EnableVertexAttribArray(0);

            _gl.VertexAttribPointer(
                1, 2, VertexAttribPointerType.Float,
                false, stride, 2 * sizeof(float));

            _gl.EnableVertexAttribArray(1);

            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            _gl.BindVertexArray(0);

            return (vao, vbo);
        }
        catch
        {
            if (vbo != 0)
                _gl.DeleteBuffer(vbo);

            if (vao != 0)
                _gl.DeleteVertexArray(vao);

            throw;
        }
    }

    private void ValidateUniforms()
    {
        if (_resolutionLocation < 0 ||
            _albedoLocation < 0 ||
            _normalMapLocation < 0 ||
            _hasNormalMapLocation < 0 ||
            _normalStrengthLocation < 0 ||
            _normalTransform0Location < 0 ||
            _normalTransform1Location < 0)
        {
            throw new InvalidOperationException(
                "Normal-buffer shader has missing uniforms.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _shader.Dispose();

        _gl.DeleteBuffer(_vbo);
        _gl.DeleteVertexArray(_vao);

        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private const string VertexShaderSource = """
        #version 330 core

        layout (location = 0) in vec2 aPosition;
        layout (location = 1) in vec2 aTexCoord;

        uniform vec2 uResolution;

        out vec2 vTexCoord;

        void main()
        {
            vec2 normalized = aPosition / uResolution;
            vec2 clipPosition = normalized * 2.0 - 1.0;

            clipPosition.y = -clipPosition.y;

            gl_Position = vec4(clipPosition, 0.0, 1.0);
            vTexCoord = aTexCoord;
        }
        """;

    private const string FragmentShaderSource = """
        #version 330 core

        in vec2 vTexCoord;

        uniform sampler2D uAlbedo;
        uniform sampler2D uNormalMap;

        uniform int uHasNormalMap;
        uniform float uNormalStrength;

        uniform vec2 uNormalTransform0;
        uniform vec2 uNormalTransform1;

        out vec4 FragColor;

        void main()
        {
            // Прозрачные части спрайта не должны закрывать
            // нормали объектов, расположенных за ним.
            float alpha = texture(uAlbedo, vTexCoord).a;

            if (alpha <= 0.001)
                discard;

            // Без normal map используем плоскую нормаль.
            vec3 normal = vec3(0.0, 0.0, 1.0);

            if (uHasNormalMap != 0)
            {
                normal =
                    texture(uNormalMap, vTexCoord).rgb * 2.0 - 1.0;
            }

            normal.xy *= uNormalStrength;

            vec2 tangentXY = normal.xy;

            normal.xy =
                uNormalTransform0 * tangentXY.x +
                uNormalTransform1 * tangentXY.y;

            float normalLength = length(normal);

            if (normalLength <= 0.000001)
                normal = vec3(0.0, 0.0, 1.0);
            else
                normal /= normalLength;

            // Записываем нормаль из [-1, 1] в [0, 1].
            FragColor = vec4(normal * 0.5 + 0.5, 1.0);
        }
        """;
}