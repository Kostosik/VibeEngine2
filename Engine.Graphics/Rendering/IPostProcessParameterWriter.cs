using Engine.Core.Math;

namespace Engine.Graphics.Rendering;

public interface IPostProcessParameterWriter
{
    void SetFloat(string name, float value);

    void SetVector3(string name, Vector3 value);
}