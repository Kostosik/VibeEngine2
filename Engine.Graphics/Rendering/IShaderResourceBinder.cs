using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering;

public interface IShaderResourceBinder
{
    /// <summary>
    /// Binds a texture to a named shader resource.
    /// </summary>
    void SetTexture(string name, TextureHandle texture);

    /// <summary>
    /// Binds sampler settings to a named shader sampler.
    /// </summary>
    void SetSampler(
        string name,
        TextureSamplerDescription sampler);
}