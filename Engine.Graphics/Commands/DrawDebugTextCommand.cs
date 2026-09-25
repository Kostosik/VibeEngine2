using Engine.Core.Math;
using Engine.Graphics.Debug;

namespace Engine.Graphics.Commands;

public readonly record struct DrawDebugTextCommand(
    string Text,
    Vector2 Position,
    DebugColor Color,
    float Scale = 2.0f,
    DebugRenderSpace Space = DebugRenderSpace.Screen,
    int Layer = 10001) :
    IRenderCommand;