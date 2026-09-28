using Engine.Graphics.Rendering;
using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL.Rendering;

internal sealed class OpenGLRenderState
{
    private readonly GL _gl;

    private RenderState _current;

    private bool _hasCurrentState;

    public OpenGLRenderState(
        GL gl)
    {
        ArgumentNullException.ThrowIfNull(
            gl);

        _gl = gl;
    }

    public void Apply(
        RenderState state)
    {
        ApplyBlendMode(
            state.BlendMode);

        ApplyDepthTest(
            state.DepthTestEnabled);

        ApplyCullMode(
            state.CullMode);

        _current =
            state;

        _hasCurrentState = true;
    }

    private void ApplyBlendMode(
        RenderBlendMode mode)
    {
        if (_hasCurrentState &&
            _current.BlendMode == mode)
        {
            return;
        }

        switch (mode)
        {
            case RenderBlendMode.Disabled:

                _gl.Disable(
                    EnableCap.Blend);

                break;

            case RenderBlendMode.Alpha:

                _gl.Enable(
                    EnableCap.Blend);

                _gl.BlendFunc(
                    BlendingFactor.SrcAlpha,
                    BlendingFactor.OneMinusSrcAlpha);

                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(mode),
                    mode,
                    null);
        }
    }

    private void ApplyDepthTest(
        bool enabled)
    {
        if (_hasCurrentState &&
            _current.DepthTestEnabled ==
            enabled)
        {
            return;
        }

        if (enabled)
        {
            _gl.Enable(
                EnableCap.DepthTest);
        }
        else
        {
            _gl.Disable(
                EnableCap.DepthTest);
        }
    }

    private void ApplyCullMode(
        RenderCullMode mode)
    {
        if (_hasCurrentState &&
            _current.CullMode == mode)
        {
            return;
        }

        switch (mode)
        {
            case RenderCullMode.Disabled:

                _gl.Disable(
                    EnableCap.CullFace);

                break;

            case RenderCullMode.Front:

                _gl.Enable(
                    EnableCap.CullFace);

                _gl.CullFace(
                    TriangleFace.Front);

                break;

            case RenderCullMode.Back:

                _gl.Enable(
                    EnableCap.CullFace);

                _gl.CullFace(
                    TriangleFace.Back);

                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(mode),
                    mode,
                    null);
        }
    }
}