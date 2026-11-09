using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering;

public interface IPostProcessEffect
{
    string Name { get; }

    bool Enabled { get; set; }

    ShaderHandle Shader { get; }

    void ApplyParameters(IPostProcessParameterWriter writer)
    {
    }
}