using Engine.Editor.Documents;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class EditorSessionTests
{
    [Fact]
    public void Close_DisposesDocument()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var document =
            new EditorDocument(
                world);

        var session =
            new EditorSession();

        session.Open(
            document);

        Assert.True(
            session.Close(
                document));

        // Destroying an entity after closing the document
        // must not reach the closed document.
        var entity =
            ecsWorld.CreateEntity();

        Assert.True(
            ecsWorld.DestroyEntity(
                entity));

        Assert.Empty(
            session.Documents);
        Assert.Null(
            session.ActiveDocument);
    }

    [Fact]
    public void CloseAll_DisposesAllDocuments()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var first =
            new EditorDocument(world);

        var second =
            new EditorDocument(world);

        var session =
            new EditorSession();

        session.Open(first);
        session.Open(second);

        session.CloseAll();

        Assert.Empty(session.Documents);
        Assert.Null(session.ActiveDocument);
    }
}