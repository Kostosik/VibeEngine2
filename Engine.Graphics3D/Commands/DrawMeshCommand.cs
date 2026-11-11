
using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;
using Engine.Graphics3D.Resources;

namespace Engine.Graphics3D.Commands;

public readonly record struct DrawMeshCommand(
    MeshHandle Mesh,
    ShaderHandle Shader,
    Matrix4x4 Model,
    Matrix4x4 View,
    Matrix4x4 Projection,
    int Layer = RenderLayers.World) : IRenderCommand
{
    public Matrix4x4 ModelViewProjection =>
        Model * View * Projection;
}
