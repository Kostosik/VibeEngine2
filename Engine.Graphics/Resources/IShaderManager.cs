namespace Engine.Graphics.Resources;

public interface IShaderManager
{
    ShaderHandle Create(
        ShaderDescription description);

    bool Exists(
        ShaderHandle shader);

    ShaderDescription GetDescription(
        ShaderHandle shader);

    void Destroy(
        ShaderHandle shader);
}