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
    public void Open_RejectsSecondDocumentForSameWorld()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var first =
            new EditorDocument(
                world);

        var second =
            new EditorDocument(
                world);

        var session =
            new EditorSession();

        session.Open(
            first);

        Assert.Throws<InvalidOperationException>(
            () =>
                session.Open(
                    second));

        second.Dispose();

        Assert.Single(
            session.Documents);

        Assert.Same(
            first,
            session.ActiveDocument);

        session.CloseAll();
    }


    [Fact]
    public void CloseAll_DisposesAllDocuments()
    {
        using var firstEcsWorld =
            new Engine.ECS.World();

        using var secondEcsWorld =
            new Engine.ECS.World();

        var firstWorld =
            new World(
                new ChunkSize(16, 16),
                firstEcsWorld);

        var secondWorld =
            new World(
                new ChunkSize(16, 16),
                secondEcsWorld);

        var first =
            new EditorDocument(
                firstWorld);

        var second =
            new EditorDocument(
                secondWorld);

        var session =
            new EditorSession();

        session.Open(first);
        session.Open(second);

        session.CloseAll();

        Assert.Empty(
            session.Documents);

        Assert.Null(
            session.ActiveDocument);
    }

    [Fact]
    public void Close_RaisesDocumentClosedAfterDocumentIsRemoved()
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

        EditorDocument? closedDocument = null;

        session.DocumentClosed +=
            document =>
            {
                closedDocument = document;

                Assert.DoesNotContain(
                    document,
                    session.Documents);

                Assert.NotSame(
                    document,
                    session.ActiveDocument);
            };

        Assert.True(
            session.Close(
                document));

        Assert.Same(
            document,
            closedDocument);
    }

    [Fact]
    public void CloseAll_RaisesDocumentClosedForEveryDocument()
    {
        using var firstEcsWorld =
            new Engine.ECS.World();

        using var secondEcsWorld =
            new Engine.ECS.World();

        var firstWorld =
            new World(
                new ChunkSize(16, 16),
                firstEcsWorld);

        var secondWorld =
            new World(
                new ChunkSize(16, 16),
                secondEcsWorld);

        var first =
            new EditorDocument(
                firstWorld);

        var second =
            new EditorDocument(
                secondWorld);

        var session =
            new EditorSession();

        session.Open(first);
        session.Open(second);

        var closed =
            new List<EditorDocument>();

        session.DocumentClosed +=
            closed.Add;

        session.CloseAll();

        Assert.Equal(
            2,
            closed.Count);

        Assert.Contains(
            first,
            closed);

        Assert.Contains(
            second,
            closed);

        Assert.Empty(
            session.Documents);

        Assert.Null(
            session.ActiveDocument);
    }

    [Fact]
    public void Open_RaisesActiveDocumentChanged()
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

        EditorDocument? changedDocument = null;

        session.ActiveDocumentChanged +=
            document =>
            {
                changedDocument = document;
            };

        session.Open(
            document);

        Assert.Same(
            document,
            changedDocument);
    }

    [Fact]
    public void Activate_WhenDocumentChanges_RaisesActiveDocumentChanged()
    {
        using var firstEcsWorld =
            new Engine.ECS.World();

        using var secondEcsWorld =
            new Engine.ECS.World();

        var firstWorld =
            new World(
                new ChunkSize(16, 16),
                firstEcsWorld);

        var secondWorld =
            new World(
                new ChunkSize(16, 16),
                secondEcsWorld);

        var first =
            new EditorDocument(
                firstWorld);

        var second =
            new EditorDocument(
                secondWorld);

        var session =
            new EditorSession();

        session.Open(first);
        session.Open(second);

        EditorDocument? changedDocument = null;

        session.ActiveDocumentChanged +=
            document =>
            {
                changedDocument = document;
            };

        session.Activate(first);

        Assert.Same(
            first,
            session.ActiveDocument);

        Assert.Same(
            first,
            changedDocument);
    }

    [Fact]
    public void Close_ActiveDocument_RaisesActiveDocumentChanged()
    {
        using var firstEcsWorld =
            new Engine.ECS.World();

        using var secondEcsWorld =
            new Engine.ECS.World();

        var firstWorld =
            new World(
                new ChunkSize(16, 16),
                firstEcsWorld);

        var secondWorld =
            new World(
                new ChunkSize(16, 16),
                secondEcsWorld);

        var first =
            new EditorDocument(
                firstWorld);

        var second =
            new EditorDocument(
                secondWorld);

        var session =
            new EditorSession();

        session.Open(first);
        session.Open(second);

        EditorDocument? changedDocument = null;

        session.ActiveDocumentChanged +=
            document =>
            {
                changedDocument = document;
            };

        Assert.True(
            session.Close(second));

        Assert.Same(
            first,
            session.ActiveDocument);

        Assert.Same(
            first,
            changedDocument);
    }
}