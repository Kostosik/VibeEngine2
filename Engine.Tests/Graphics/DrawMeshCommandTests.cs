
using Engine.Core.Math;
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;
using Engine.Graphics3D.Commands;
using Engine.Graphics3D.Resources;
using Xunit;

namespace Engine.Tests.Graphics;

public sealed class DrawMeshCommandTests
{
    [Fact]
    public void Constructor_StoresAllRenderData()
    {
        var mesh = new MeshHandle(1);
        var shader = new ShaderHandle(2);

        var model = Matrix4x4.CreateTranslation(
            new Vector3(1, 2, 3));

        var view = Matrix4x4.Identity;
        var projection = Matrix4x4.Identity;

        var command = new DrawMeshCommand(
            mesh,
            shader,
            model,
            view,
            projection,
            Layer: 10);

        Assert.Equal(mesh, command.Mesh);
        Assert.Equal(shader, command.Shader);
        Assert.Equal(model, command.Model);
        Assert.Equal(view, command.View);
        Assert.Equal(projection, command.Projection);
        Assert.Equal(10, command.Layer);
    }

    [Fact]
    public void Constructor_UsesWorldLayerByDefault()
    {
        var command = new DrawMeshCommand(
            new MeshHandle(1),
            new ShaderHandle(2),
            Matrix4x4.Identity,
            Matrix4x4.Identity,
            Matrix4x4.Identity);

        Assert.Equal(RenderLayers.World, command.Layer);
    }

    [Fact]
    public void ModelViewProjection_UsesExpectedMatrixOrder()
    {
        var model = Matrix4x4.CreateScale(
            new Vector3(2, 1, 1));

        var view = Matrix4x4.CreateTranslation(
            new Vector3(3, 0, 0));

        var projection = Matrix4x4.Identity;

        var command = new DrawMeshCommand(
            new MeshHandle(1),
            new ShaderHandle(2),
            model,
            view,
            projection);

        var result = command.ModelViewProjection.TransformPoint(
            new Vector3(1, 0, 0));

        Assert.Equal(new Vector3(5, 0, 0), result);
    }
}
