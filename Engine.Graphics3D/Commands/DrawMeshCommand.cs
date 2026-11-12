
using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;
using Engine.Graphics3D.Lighting;
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
    public PointLight3D PointLight { get; init; }
    = PointLight3D.Disabled;
    public DirectionalLight3D DirectionalLight { get; init; }
    = DirectionalLight3D.Default;
    public Material3D Material { get; init; } = Material3D.Default;
    public Matrix4x4 ModelViewProjection =>
        Model * View * Projection;
}
