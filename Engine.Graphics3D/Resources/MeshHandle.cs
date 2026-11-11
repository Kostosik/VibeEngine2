
namespace Engine.Graphics3D.Resources;

public readonly record struct MeshHandle(uint Value)
{
    public bool IsValid => Value != 0;

    public static MeshHandle Invalid => default;
}
