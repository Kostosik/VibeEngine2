
using Engine.Graphics.Resources;

namespace Engine.Graphics3D.Resources;

public interface IMeshManager
{
    MeshHandle Create(
        MeshData mesh,
        GraphicsBufferUsage usage = GraphicsBufferUsage.Static);

    bool Exists(MeshHandle mesh);

    MeshDescription GetDescription(MeshHandle mesh);

    void Destroy(MeshHandle mesh);
}
