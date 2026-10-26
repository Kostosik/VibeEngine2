using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.Editor;
using Engine.Editor.Commands;
using Engine.Editor.Documents;
using Engine.Editor.Documents.Persistence;
using Engine.Graphics2D.Sprites;
using Engine.Networking.Replication;
using Engine.Physics.Components;
using Engine.Physics.Materials;
using Engine.Physics.Shapes;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.Types;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class EditorDocumentPersistenceTests
{
    [Fact]
    public void BinaryPersistence_RoundTripsNetworkIdentity()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"vibe-network-identity-{Guid.NewGuid():N}.world");

        try
        {
            var serializers = new EcsComponentSerializerRegistry();
            serializers.RegisterEngineComponents();
            serializers.RegisterNetworkComponents();

            var persistence =
                new BinaryEditorDocumentPersistence(serializers);

            using var ecsWorld = new Engine.ECS.World();

            using var world = new World(
                new ChunkSize(16, 16),
                ecsWorld);

            var entity = ecsWorld.CreateEntity();
            var expected = NetworkIdentity.Create();

            ecsWorld.Add(entity, expected);

            using var document = new EditorDocument(world);

            persistence.Save(path, document);

            using var restoredWorld = persistence.Load(path);

            Assert.True(restoredWorld.EcsWorld.Exists(entity));

            var actual = restoredWorld.EcsWorld.Get<NetworkIdentity>(entity);

            Assert.Equal(expected.Id, actual.Id);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void BinaryPersistence_RoundTripsSpriteComponentInEditorWorld()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"vibe-sprite-scene-{Guid.NewGuid():N}.world");

        try
        {
            var serializers = new EcsComponentSerializerRegistry();
            serializers.RegisterEngineComponents();

            var persistence =
                new BinaryEditorDocumentPersistence(serializers);

            using var ecsWorld = new Engine.ECS.World();

            using var world = new World(
                new ChunkSize(16, 16),
                ecsWorld);

            var entity = ecsWorld.CreateEntity();

            ecsWorld.Add(
                entity,
                new WorldTransform2D(
                    new FixedVector2(
                        Fixed32.FromInt(4),
                        Fixed32.FromInt(7))));

            var expected = new SpriteComponent(
                "Textures/player.png",
                3.5f,
                2.25f,
                17);

            ecsWorld.Add(entity, expected);

            using var document = new EditorDocument(world);

            // Используем реальный механизм сохранения Editor.
            persistence.Save(path, document);

            // Загружаем сохранённый мир заново.
            using var restoredWorld = persistence.Load(path);

            var restoredEcs = restoredWorld.EcsWorld;

            Assert.True(restoredEcs.Exists(entity));
            Assert.True(restoredEcs.Has<SpriteComponent>(entity));

            var actual = restoredEcs.Get<SpriteComponent>(entity);

            Assert.Equal(expected.TexturePath, actual.TexturePath);
            Assert.Equal(expected.Width, actual.Width);
            Assert.Equal(expected.Height, actual.Height);
            Assert.Equal(expected.Layer, actual.Layer);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void BinaryPersistence_RoundTripsPhysicsComponentsAddedThroughEditorCommands()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"vibe-physics-scene-{Guid.NewGuid():N}.world");

        try
        {
            using var ecsWorld = new Engine.ECS.World();

            using var world = new World(
                new ChunkSize(16, 16),
                ecsWorld);

            using var document = new EditorDocument(world);

            // Настройка регистрации повторяет текущий Editor.Sandbox.
            var editor = new EditorContext();

            editor.ComponentTypes.RegisterEngineComponents();

            editor.ComponentTypes.Register<PhysicsBody2D>(
                () => PhysicsBody2D.Dynamic(
                    Fixed32.FromInt(2),
                    Fixed32.FromInt(3)));

            editor.ComponentTypes.Register<Collider2D>(
                () => new Collider2D(
                    new AabbShape2D(
                        new FixedVector2(
                            Fixed32.One,
                            Fixed32.One))));

            editor.ComponentDependencies.Register(
                typeof(PhysicsBody2D),
                typeof(WorldTransform2D));

            editor.ComponentDependencies.Register(
                typeof(Collider2D),
                typeof(PhysicsBody2D),
                typeof(WorldTransform2D));

            // Создаём сущность тем же механизмом, который использует редактор.
            var createEntity = new CreateEditorEntityCommand(document);
            document.Execute(createEntity);

            var entity = createEntity.Entity;
            var reference = createEntity.Reference!;

            // WorldTransform2D уже добавлен командой создания сущности.
            // Физические компоненты добавляем через редакторские команды.
            document.Execute(
                new AddEditorComponentCommand(
                    world,
                    reference,
                    typeof(PhysicsBody2D),
                    editor.ComponentDependencies,
                    editor.ComponentTypes));

            document.Execute(
                new AddEditorComponentCommand(
                    world,
                    reference,
                    typeof(Collider2D),
                    editor.ComponentDependencies,
                    editor.ComponentTypes));

            // Настраиваем преобразование сущности.
            var position = new FixedVector2(
                Fixed32.FromInt(12),
                Fixed32.FromInt(-4));

            var scale = new FixedVector2(
                Fixed32.FromInt(2),
                Fixed32.FromInt(3));

            ref var transform =
                ref ecsWorld.Get<WorldTransform2D>(entity);

            transform.Position = position;
            transform.Rotation = Fixed32.HalfPi;
            transform.Scale = scale;

            // Настраиваем сохраняемое состояние физического тела.
            var velocity = new FixedVector2(
                Fixed32.FromInt(3),
                Fixed32.FromInt(-2));

            var force = new FixedVector2(
                Fixed32.FromInt(4),
                Fixed32.FromInt(5));

            ref var body =
                ref ecsWorld.Get<PhysicsBody2D>(entity);

            body.Velocity = velocity;
            body.GravityScale = Fixed32.FromInt(2);
            body.AngularVelocity = Fixed32.FromInt(1);
            body.LinearDamping = Fixed32.FromRatio(1, 4);
            body.AngularDamping = Fixed32.FromRatio(1, 8);
            body.AddForce(force);
            body.AddTorque(Fixed32.FromInt(2));

            // Настраиваем состояние коллайдера.
            var offset = new FixedVector2(
                Fixed32.FromInt(1),
                Fixed32.FromInt(-1));

            var material = new PhysicsMaterial2D(
                Fixed32.FromRatio(1, 4),
                Fixed32.FromRatio(3, 4));

            ref var collider =
                ref ecsWorld.Get<Collider2D>(entity);

            collider.Offset = offset;
            collider.Material = material;
            collider.IsTrigger = true;
            collider.Enabled = false;
            collider.CollisionLayer = 4u;
            collider.CollisionMask = 8u;

            // Используем производственный механизм сохранения сцены.
            var serializers =
                new EcsComponentSerializerRegistry();

            serializers.RegisterEngineComponents();

            var persistence =
                new BinaryEditorDocumentPersistence(serializers);

            persistence.Save(path, document);

            // Загружаем сцену в новый World.
            using var restoredWorld =
                persistence.Load(path);

            var restoredEcs = restoredWorld.EcsWorld;

            Assert.True(restoredEcs.Exists(entity));

            Assert.True(restoredEcs.Has<WorldTransform2D>(entity));
            Assert.True(restoredEcs.Has<PhysicsBody2D>(entity));
            Assert.True(restoredEcs.Has<Collider2D>(entity));

            var restoredTransform =
                restoredEcs.Get<WorldTransform2D>(entity);

            Assert.Equal(position, restoredTransform.Position);
            Assert.Equal(Fixed32.HalfPi, restoredTransform.Rotation);
            Assert.Equal(scale, restoredTransform.Scale);

            var restoredBody =
                restoredEcs.Get<PhysicsBody2D>(entity);

            Assert.Equal(
                PhysicsBodyType.Dynamic,
                restoredBody.BodyType);

            Assert.Equal(Fixed32.FromInt(2), restoredBody.Mass);
            Assert.Equal(Fixed32.FromInt(3), restoredBody.Inertia);
            Assert.Equal(velocity, restoredBody.Velocity);
            Assert.Equal(force, restoredBody.Force);
            Assert.Equal(Fixed32.FromInt(2), restoredBody.GravityScale);
            Assert.Equal(Fixed32.FromInt(1), restoredBody.AngularVelocity);
            Assert.Equal(Fixed32.FromInt(2), restoredBody.Torque);
            Assert.Equal(Fixed32.FromRatio(1, 4), restoredBody.LinearDamping);
            Assert.Equal(Fixed32.FromRatio(1, 8), restoredBody.AngularDamping);

            var restoredCollider =
                restoredEcs.Get<Collider2D>(entity);

            Assert.Equal(offset, restoredCollider.Offset);
            Assert.Equal(material, restoredCollider.Material);
            Assert.True(restoredCollider.IsTrigger);
            Assert.False(restoredCollider.Enabled);
            Assert.Equal(4u, restoredCollider.CollisionLayer);
            Assert.Equal(8u, restoredCollider.CollisionMask);

            Assert.Equal(
                new FixedVector2(Fixed32.One, Fixed32.One),
                restoredCollider.Shape.Aabb.Size);

            Assert.Equal(
                new WorldPosition(0, 0),
                restoredWorld.SpatialEntities.GetPosition(entity));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }


    [Fact]
    public void SaveDocument_MarksDocumentSavedAndAssignsPath()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var editor =
            new EditorContext();

        var document =
            editor.OpenDocument(
                world);

        document.Execute(
            new CreateEditorEntityCommand(
                document));

        Assert.True(
            document.IsDirty);

        var persistence =
            new TestPersistence();

        editor.SaveDocument(
            document,
            persistence,
            "test.world");

        Assert.False(
            document.IsDirty);

        Assert.Equal(
            "test.world",
            document.FilePath);

        Assert.Same(
            document,
            persistence.SavedDocument);

        Assert.Equal(
            "test.world",
            persistence.SavedPath);
    }

    [Fact]
    public void LoadDocument_OpensWorldAndAssignsPath()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var persistence =
            new TestPersistence
            {
                WorldToLoad =
                    world
            };

        var editor =
            new EditorContext();

        var document =
            editor.LoadDocument(
                persistence,
                "loaded.world");

        Assert.Same(
            world,
            document.World);

        Assert.Equal(
            "loaded.world",
            document.FilePath);

        Assert.Same(
            document,
            editor.ActiveDocument);
    }

    [Fact]
    public void BinaryPersistence_RoundTripsSpatialEntity()
    {
        var path =
            Path.Combine(
                Path.GetTempPath(),
                $"vibe-editor-{Guid.NewGuid():N}.world");

        try
        {
            var registry =
                new EcsComponentSerializerRegistry();

            registry.Register(
                "engine.world.position",
                new WorldPositionComponentSerializer());

            var persistence =
                new BinaryEditorDocumentPersistence(
                    registry);

            using var ecsWorld =
                new Engine.ECS.World();

            var world =
                new World(
                    new ChunkSize(16, 16),
                    ecsWorld);

            var entity =
                world.SpatialEntities.CreateEntity(
                    new WorldPosition(
                        -7,
                        11));

            persistence.Save(
                path,
                new EditorDocument(
                    world));

            var loadedWorld =
                persistence.Load(
                    path);

            Assert.True(
                loadedWorld.EcsWorld.Exists(
                    entity));

            Assert.Equal(
                new WorldPosition(
                    -7,
                    11),
                loadedWorld.SpatialEntities.GetPosition(
                    entity));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private sealed class TestPersistence :
        IEditorDocumentPersistence
    {
        public EditorDocument? SavedDocument { get; private set; }

        public string? SavedPath { get; private set; }

        public World? WorldToLoad { get; init; }

        public void Save(
            string path,
            EditorDocument document)
        {
            SavedPath = path;
            SavedDocument = document;
        }

        public World Load(
            string path)
        {
            return WorldToLoad
                ?? throw new InvalidOperationException(
                    "Test world has not been configured.");
        }
    }
}