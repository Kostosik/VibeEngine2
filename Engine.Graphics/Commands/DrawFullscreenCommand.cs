using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;

namespace Engine.Graphics.Commands;

public sealed record DrawFullscreenCommand(
    TextureHandle Source,
    ShaderHandle Shader,
    int Layer = RenderLayers.Present)
    : IRenderCommand;