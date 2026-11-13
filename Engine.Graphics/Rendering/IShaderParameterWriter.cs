
using Engine.Core.Math;

namespace Engine.Graphics.Rendering;

/// <summary>
/// Backend-neutral interface for writing typed shader parameters.
/// Parameter names are resolved by the active graphics backend.
/// </summary>
public interface IShaderParameterWriter
{
    void SetInt32(string name, int value);

    void SetFloat(string name, float value);

    void SetVector2(string name, Vector2 value);

    void SetVector3(string name, Vector3 value);

    /// <summary>
    /// Sets a matrix using the engine's row-vector convention.
    /// The backend is responsible for API-specific matrix layout.
    /// </summary>
    void SetMatrix4x4(string name, Matrix4x4 value);
}
