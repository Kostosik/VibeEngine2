using Engine.Core.Math;
using Engine.Graphics.Rendering;
using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL.Rendering;

internal sealed class OpenGLRenderState
{
    private readonly GL _gl;

    private RenderState _current;

    private bool _hasCurrentState;

    /// <summary>
    /// Marks cached GPU state as unknown.
    /// The next Apply call will reapply every tracked state.
    /// </summary>
    public void Invalidate()
    {
        _hasCurrentState = false;
    }

    public OpenGLRenderState(GL gl)
    {
        ArgumentNullException.ThrowIfNull(gl);

        _gl = gl;
    }

    public void Apply(RenderState state)
    {
        ValidateState(state);

        try
        {
            ApplyScissorState(state.Scissor);
            ApplyBlendState(state.Blend);
            ApplyDepthState(state.Depth);
            ApplyRasterizerState(state.Rasterizer);
        }
        catch
        {
            // После ошибки фактическое состояние GPU
            // может отличаться от кэша.
            Invalidate();
            throw;
        }

        _current = state;
        _hasCurrentState = true;
    }

    private static void ValidateState(RenderState state)
    {
        if (!Enum.IsDefined(
        typeof(RenderPolygonMode),
        state.Rasterizer.PolygonMode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                "Unsupported polygon mode.");
        }

        if (!float.IsFinite(state.Rasterizer.PolygonOffset.Factor) ||
            !float.IsFinite(state.Rasterizer.PolygonOffset.Units))
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                "Polygon offset values must be finite.");
        }

        if (state.Scissor.Width < 0 ||
    state.Scissor.Height < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                "Scissor dimensions cannot be negative.");
        }

        if (!Enum.IsDefined(typeof(RenderBlendFactor), state.Blend.SourceColorFactor) ||
    !Enum.IsDefined(typeof(RenderBlendFactor), state.Blend.DestinationColorFactor) ||
    !Enum.IsDefined(typeof(RenderBlendFactor), state.Blend.SourceAlphaFactor) ||
    !Enum.IsDefined(typeof(RenderBlendFactor), state.Blend.DestinationAlphaFactor))
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                "Unsupported blend factor.");
        }

        if (!Enum.IsDefined(typeof(RenderBlendEquation), state.Blend.ColorEquation) ||
            !Enum.IsDefined(typeof(RenderBlendEquation), state.Blend.AlphaEquation))
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                "Unsupported blend equation.");
        }

        if (!Enum.IsDefined(typeof(RenderBlendMode), state.Blend.Mode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(state), state.Blend.Mode, "Unsupported blend mode.");
        }

        if (!Enum.IsDefined(typeof(RenderCullMode), state.Rasterizer.CullMode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(state), state.Rasterizer.CullMode, "Unsupported cull mode.");
        }

        if (!Enum.IsDefined(typeof(RenderDepthFunction), state.Depth.Function))
        {
            throw new ArgumentOutOfRangeException(
                nameof(state), state.Depth.Function, "Unsupported depth function.");
        }

        if (!Enum.IsDefined(typeof(RenderFrontFace), state.Rasterizer.FrontFace))
        {
            throw new ArgumentOutOfRangeException(
                nameof(state), state.Rasterizer.FrontFace, "Unsupported front face.");
        }
    }

    public void ClearBuffers(
        ClearBufferMask mask,
        Vector4? clearColor = null)
    {
        var hadKnownState = _hasCurrentState;

        var previousScissor = _current.Scissor;
        var previousDepthWrite = _current.Depth.WriteEnabled;

        try
        {
            // Temporarily disable scissor for a full clear.
            if (!hadKnownState || previousScissor.Enabled)
            {
                _gl.Disable(EnableCap.ScissorTest);
            }

            // Allow depth buffer clearing.
            if (!hadKnownState || !previousDepthWrite)
            {
                _gl.DepthMask(true);
            }

            if (clearColor.HasValue)
            {
                var color = clearColor.Value;

                _gl.ClearColor(
                    color.X,
                    color.Y,
                    color.Z,
                    color.W);
            }

            _gl.Clear(mask);
        }
        catch
        {
            Invalidate();
            throw;
        }

        if (!hadKnownState)
        {
            Invalidate();
            return;
        }

        try
        {
            // Restore the cached scissor state.
            if (previousScissor.Enabled)
            {
                _gl.Scissor(
                    previousScissor.X,
                    previousScissor.Y,
                    (uint)previousScissor.Width,
                    (uint)previousScissor.Height);

                _gl.Enable(EnableCap.ScissorTest);
            }

            // Restore the cached depth write state.
            if (!previousDepthWrite)
            {
                _gl.DepthMask(false);
            }
        }
        catch
        {
            Invalidate();
            throw;
        }
    }

    private void ApplyBlendState(RenderBlendState state)
    {
        var previous = _current.Blend;

        if (!_hasCurrentState || previous.Mode != state.Mode)
        {
            if (state.Mode == RenderBlendMode.Disabled)
            {
                _gl.Disable(EnableCap.Blend);
            }
            else
            {
                _gl.Enable(EnableCap.Blend);
            }
        }

        var parametersChanged =
            !_hasCurrentState ||
            previous.SourceColorFactor != state.SourceColorFactor ||
            previous.DestinationColorFactor != state.DestinationColorFactor ||
            previous.ColorEquation != state.ColorEquation ||
            previous.SourceAlphaFactor != state.SourceAlphaFactor ||
            previous.DestinationAlphaFactor != state.DestinationAlphaFactor ||
            previous.AlphaEquation != state.AlphaEquation;

        if (!parametersChanged)
            return;

        _gl.BlendFuncSeparate(
            ToOpenGLBlendFactor(state.SourceColorFactor),
            ToOpenGLBlendFactor(state.DestinationColorFactor),
            ToOpenGLBlendFactor(state.SourceAlphaFactor),
            ToOpenGLBlendFactor(state.DestinationAlphaFactor));

        _gl.BlendEquationSeparate(
            ToOpenGLBlendEquation(state.ColorEquation),
            ToOpenGLBlendEquation(state.AlphaEquation));
    }

    private static BlendingFactor ToOpenGLBlendFactor(
    RenderBlendFactor factor)
    {
        return factor switch
        {
            RenderBlendFactor.Zero => BlendingFactor.Zero,
            RenderBlendFactor.One => BlendingFactor.One,

            RenderBlendFactor.SourceColor => BlendingFactor.SrcColor,
            RenderBlendFactor.OneMinusSourceColor => BlendingFactor.OneMinusSrcColor,

            RenderBlendFactor.DestinationColor => BlendingFactor.DstColor,
            RenderBlendFactor.OneMinusDestinationColor => BlendingFactor.OneMinusDstColor,

            RenderBlendFactor.SourceAlpha => BlendingFactor.SrcAlpha,
            RenderBlendFactor.OneMinusSourceAlpha => BlendingFactor.OneMinusSrcAlpha,

            RenderBlendFactor.DestinationAlpha => BlendingFactor.DstAlpha,
            RenderBlendFactor.OneMinusDestinationAlpha => BlendingFactor.OneMinusDstAlpha,

            RenderBlendFactor.SourceAlphaSaturate => BlendingFactor.SrcAlphaSaturate,

            _ => throw new ArgumentOutOfRangeException(nameof(factor))
        };
    }

    private static BlendEquationModeEXT ToOpenGLBlendEquation(
        RenderBlendEquation equation)
    {
        return equation switch
        {
            RenderBlendEquation.Add => BlendEquationModeEXT.FuncAdd,
            RenderBlendEquation.Subtract => BlendEquationModeEXT.FuncSubtract,
            RenderBlendEquation.ReverseSubtract => BlendEquationModeEXT.FuncReverseSubtract,
            RenderBlendEquation.Minimum => BlendEquationModeEXT.Min,
            RenderBlendEquation.Maximum => BlendEquationModeEXT.Max,

            _ => throw new ArgumentOutOfRangeException(nameof(equation))
        };
    }

    private void ApplyDepthState(RenderDepthState state)
    {
        var previous = _current.Depth;

        if (!_hasCurrentState ||
            previous.TestEnabled != state.TestEnabled)
        {
            if (state.TestEnabled)
                _gl.Enable(EnableCap.DepthTest);
            else
                _gl.Disable(EnableCap.DepthTest);
        }

        if (!_hasCurrentState ||
            previous.WriteEnabled != state.WriteEnabled)
        {
            _gl.DepthMask(state.WriteEnabled);
        }

        if (!_hasCurrentState ||
            previous.Function != state.Function)
        {
            _gl.DepthFunc(ToOpenGLDepthFunction(state.Function));
        }
    }

    private void ApplyScissorState(RenderScissorState state)
    {
        var previous = _current.Scissor;

        if (!_hasCurrentState ||
            previous.Enabled != state.Enabled)
        {
            if (state.Enabled)
            {
                _gl.Enable(EnableCap.ScissorTest);
            }
            else
            {
                _gl.Disable(EnableCap.ScissorTest);
            }
        }

        if (!state.Enabled)
            return;

        var rectangleChanged =
            !_hasCurrentState ||
            !previous.Enabled ||
            previous.X != state.X ||
            previous.Y != state.Y ||
            previous.Width != state.Width ||
            previous.Height != state.Height;

        if (!rectangleChanged)
            return;

        _gl.Scissor(
            state.X,
            state.Y,
            (uint)state.Width,
            (uint)state.Height);
    }

    private void ApplyRasterizerState(RenderRasterizerState state)
    {
        var previous = _current.Rasterizer;

        ApplyCullMode(state.CullMode, previous.CullMode);

        if (!_hasCurrentState ||
            previous.FrontFace != state.FrontFace)
        {
            _gl.FrontFace(state.FrontFace switch
            {
                RenderFrontFace.Clockwise =>
                    FrontFaceDirection.CW,

                RenderFrontFace.CounterClockwise =>
                    FrontFaceDirection.Ccw,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(state.FrontFace))
            });
        }

        if (!_hasCurrentState ||
            previous.PolygonMode != state.PolygonMode)
        {
            _gl.PolygonMode(
                TriangleFace.FrontAndBack,
                ToOpenGLPolygonMode(state.PolygonMode));
        }

        ApplyPolygonOffset(state, previous);
    }

    private void ApplyCullMode(
    RenderCullMode mode,
    RenderCullMode previous)
    {
        if (_hasCurrentState && previous == mode)
            return;

        switch (mode)
        {
            case RenderCullMode.Disabled:
                _gl.Disable(EnableCap.CullFace);
                break;

            case RenderCullMode.Front:
                _gl.Enable(EnableCap.CullFace);
                _gl.CullFace(TriangleFace.Front);
                break;

            case RenderCullMode.Back:
                _gl.Enable(EnableCap.CullFace);
                _gl.CullFace(TriangleFace.Back);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    private void ApplyPolygonOffset(
    RenderRasterizerState state,
    RenderRasterizerState previous)
    {
        var currentEnabled = state.PolygonOffset.Enabled;

        // Unknown cache: establish a deterministic baseline.
        if (!_hasCurrentState)
        {
            _gl.Disable(EnableCap.PolygonOffsetFill);
            _gl.Disable(EnableCap.PolygonOffsetLine);
            _gl.Disable(EnableCap.PolygonOffsetPoint);

            if (currentEnabled)
            {
                _gl.Enable(
                    GetPolygonOffsetCapability(state.PolygonMode));

                _gl.PolygonOffset(
                    state.PolygonOffset.Factor,
                    state.PolygonOffset.Units);
            }

            return;
        }

        var previousEnabled = previous.PolygonOffset.Enabled;

        var modeChanged =
            previous.PolygonMode != state.PolygonMode;

        var offsetChanged =
            previous.PolygonOffset.Factor != state.PolygonOffset.Factor ||
            previous.PolygonOffset.Units != state.PolygonOffset.Units;

        if (previousEnabled &&
            (!currentEnabled || modeChanged))
        {
            _gl.Disable(
                GetPolygonOffsetCapability(previous.PolygonMode));
        }

        if (!currentEnabled)
            return;

        var capability =
            GetPolygonOffsetCapability(state.PolygonMode);

        if (!previousEnabled || modeChanged)
        {
            _gl.Enable(capability);
        }

        if (!previousEnabled || modeChanged || offsetChanged)
        {
            _gl.PolygonOffset(
                state.PolygonOffset.Factor,
                state.PolygonOffset.Units);
        }
    }

    private static EnableCap GetPolygonOffsetCapability(
    RenderPolygonMode mode)
    {
        return mode switch
        {
            RenderPolygonMode.Fill =>
                EnableCap.PolygonOffsetFill,

            RenderPolygonMode.Line =>
                EnableCap.PolygonOffsetLine,

            RenderPolygonMode.Point =>
                EnableCap.PolygonOffsetPoint,

            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }

    private static Silk.NET.OpenGL.PolygonMode ToOpenGLPolygonMode(
    RenderPolygonMode mode)
    {
        return mode switch
        {
            RenderPolygonMode.Fill =>
                Silk.NET.OpenGL.PolygonMode.Fill,

            RenderPolygonMode.Line =>
                Silk.NET.OpenGL.PolygonMode.Line,

            RenderPolygonMode.Point =>
                Silk.NET.OpenGL.PolygonMode.Point,

            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }

    private static DepthFunction ToOpenGLDepthFunction(
        RenderDepthFunction function)
    {
        return function switch
        {
            RenderDepthFunction.Never => DepthFunction.Never,
            RenderDepthFunction.Less => DepthFunction.Less,
            RenderDepthFunction.Equal => DepthFunction.Equal,
            RenderDepthFunction.LessOrEqual => DepthFunction.Lequal,
            RenderDepthFunction.Greater => DepthFunction.Greater,
            RenderDepthFunction.NotEqual => DepthFunction.Notequal,
            RenderDepthFunction.GreaterOrEqual => DepthFunction.Gequal,
            RenderDepthFunction.Always => DepthFunction.Always,

            _ => throw new ArgumentOutOfRangeException(nameof(function))
        };
    }
}