using Engine.Graphics.OpenGL.Resources;
using Engine.Graphics3D.Commands;
using Engine.Graphics3D.Lighting;
using Engine.Graphics3D.Resources;
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

    public void Draw(
        DrawMeshCommand command,
        SceneLighting3D lighting)
    {
        ArgumentNullException.ThrowIfNull(lighting);
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

            shader.SetVector3(
                "uBaseColor",
                new Engine.Core.Math.Vector3(
                    command.Material.BaseColor.R,
                    command.Material.BaseColor.G,
                    command.Material.BaseColor.B));

            shader.SetFloat(
                "uAmbientStrength",
                command.Material.AmbientStrength);

            shader.SetFloat(
                "uDiffuseStrength",
                command.Material.DiffuseStrength);

            shader.SetVector3(
                "uLightDirection",
                command.DirectionalLight.DirectionToLight);

            shader.SetVector3(
                "uLightColor",
                command.DirectionalLight.Color);

            shader.SetFloat(
                "uLightIntensity",
                command.DirectionalLight.Intensity);

            shader.SetVector3(
                "uPointLightPosition",
                command.PointLight.Position);

            shader.SetVector3(
                "uPointLightColor",
                command.PointLight.Color);

            shader.SetFloat(
                "uPointLightIntensity",
                command.PointLight.Intensity);

            shader.SetFloat(
                "uPointLightRange",
                command.PointLight.Range);

            var directionalLight = lighting.DirectionalLight;

            shader.SetVector3(
                "uDirectionalLightDirection",
                directionalLight.DirectionToLight);

            shader.SetVector3(
                "uDirectionalLightColor",
                directionalLight.Color);

            shader.SetFloat(
                "uDirectionalLightIntensity",
                directionalLight.Intensity);

            for (var i = 0; i < SceneLighting3D.MaxPointLights; i++)
            {
                var light = i < lighting.PointLights.Count
                    ? lighting.PointLights[i]
                    : PointLight3D.Disabled;

                var prefix = $"uPointLights[{i}]";

                shader.SetVector3(
                    $"{prefix}.position",
                    light.Position);

                shader.SetVector3(
                    $"{prefix}.color",
                    light.Color);

                shader.SetFloat(
                    $"{prefix}.intensity",
                    light.Intensity);

                shader.SetFloat(
                    $"{prefix}.range",
                    light.Range);
            }

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