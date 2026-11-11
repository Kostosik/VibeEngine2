using Engine.Graphics3D.Commands;
using Engine.Graphics3D.Resources;
using Engine.Graphics.OpenGL.Resources;
using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL.Rendering;

internal sealed class OpenGLMeshRenderer
{
    private readonly GL _gl;
    private readonly OpenGLMeshManager _meshes;
    private readonly OpenGLShaderManager _shaders;

    public OpenGLMeshRenderer(
        GL gl,
        OpenGLMeshManager meshes,
        OpenGLShaderManager shaders)
    {
        ArgumentNullException.ThrowIfNull(gl);
        ArgumentNullException.ThrowIfNull(meshes);
        ArgumentNullException.ThrowIfNull(shaders);

        _gl = gl;
        _meshes = meshes;
        _shaders = shaders;
    }

    public bool Exists(MeshHandle mesh)
    {
        return _meshes.Exists(mesh);
    }

    public void Draw(DrawMeshCommand command)
    {
        if (!_meshes.Exists(command.Mesh))
        {
            throw new KeyNotFoundException(
                $"Mesh '{command.Mesh.Value}' was not found.");
        }

        if (!_shaders.Exists(command.Shader))
        {
            throw new KeyNotFoundException(
                $"Shader '{command.Shader.Value}' was not found.");
        }

        var description = _meshes.GetDescription(command.Mesh);
        var shader = _shaders.GetProgram(command.Shader);

        var shaderStarted = false;

        try
        {
            shader.Use();
            shaderStarted = true;

            shader.SetMatrix4x4("uModel", command.Model);
            shader.SetMatrix4x4("uView", command.View);
            shader.SetMatrix4x4("uProjection", command.Projection);

            _gl.BindVertexArray(
                _meshes.GetVertexArray(command.Mesh));

            unsafe
            {
                _gl.DrawElements(
                    PrimitiveType.Triangles,
                    (uint)description.IndexCount,
                    DrawElementsType.UnsignedInt,
                    (void*)0);
            }
        }
        finally
        {
            try
            {
                _gl.BindVertexArray(0);
            }
            finally
            {
                if (shaderStarted)
                {
                    shader.StopUsing();
                }
            }
        }
    }
}