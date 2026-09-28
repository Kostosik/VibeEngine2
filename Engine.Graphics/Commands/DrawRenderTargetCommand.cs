using Engine.Core.Math;
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;

namespace Engine.Graphics.Commands;

public sealed record DrawRenderTargetCommand(
    RenderTargetHandle Target,
    Vector2 Position,
    Vector2 Size,
    Rectangle UV,
    int Layer = RenderLayers.Present)
    : IRenderCommand;