
using Engine.Camera;
using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.Graphics3D.Commands;
using Engine.Graphics3D.Components;
using Engine.Graphics3D.Resources;

namespace Engine.Graphics3D.Rendering;

public sealed class MeshRenderSystem3D
{
    private readonly World _world;
    private readonly IGraphicsDevice _graphics;
    private readonly IMeshManager _meshes;
    private readonly Camera3D _camera;

    public MeshRenderSystem3D(
        World world,
        IGraphicsDevice graphics,
        Camera3D camera)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(camera);

        if (graphics is not IMeshManagerProvider provider)
        {
            throw new NotSupportedException(
                "Graphics backend does not support 3D meshes.");
        }

        _world = world;
        _graphics = graphics;
        _meshes = provider.Meshes;
        _camera = camera;
    }

    public void Render()
    {
        var view = _camera.ViewMatrix;
        var projection = _camera.ProjectionMatrix;

        foreach (var item in
            _world.QueryReadOnly<MeshRenderer3D, WorldTransform3D>())
        {
            var renderer = item.First;
            var transform = item.Second.Value;

            if (!renderer.Enabled ||
                !renderer.Mesh.IsValid ||
                !renderer.Shader.IsValid)
            {
                continue;
            }

            if (!_meshes.Exists(renderer.Mesh) ||
                !_graphics.Shaders.Exists(renderer.Shader))
            {
                continue;
            }

            if (!transform.IsFinite ||
                !IsValidRotation(transform.Rotation))
            {
                continue;
            }

            // Row-vector convention:
            // Model = Scale * Rotation * Translation.
            var model =
                Matrix4x4.CreateScale(transform.Scale) *
                Matrix4x4.CreateRotation(transform.Rotation) *
                Matrix4x4.CreateTranslation(transform.Position);

            _graphics.Submit(
                new DrawMeshCommand(
                    renderer.Mesh,
                    renderer.Shader,
                    model,
                    view,
                    projection,
                    renderer.Layer)
                {
                    Material = renderer.Material
                        ?? Material3D.Default
                });
        }
    }

    private static bool IsValidRotation(Quaternion rotation)
    {
        var lengthSquared =
            (double)rotation.X * rotation.X +
            (double)rotation.Y * rotation.Y +
            (double)rotation.Z * rotation.Z +
            (double)rotation.W * rotation.W;

        return double.IsFinite(lengthSquared) &&
               lengthSquared > 1e-12;
    }
}
