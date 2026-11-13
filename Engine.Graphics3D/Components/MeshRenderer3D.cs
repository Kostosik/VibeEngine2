
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;
using Engine.Graphics3D.Resources;

namespace Engine.Graphics3D.Components;

public struct MeshRenderer3D
{
    public MeshRenderer3D(
        MeshHandle mesh,
        ShaderHandle shader,
        Material3D? material = null,
        int layer = RenderLayers.World)
    {
        if (!mesh.IsValid)
            throw new ArgumentException(
                "Mesh handle must be valid.", nameof(mesh));

        if (!shader.IsValid)
            throw new ArgumentException(
                "Shader handle must be valid.", nameof(shader));

        Mesh = mesh;
        Shader = shader;
        Material = material ?? Material3D.Default;
        Layer = layer;
        Enabled = true;
    }

    public MeshHandle Mesh { get; set; }

    public ShaderHandle Shader { get; set; }

    public Material3D? Material { get; set; }

    public int Layer { get; set; }

    public bool Enabled { get; set; }
}
