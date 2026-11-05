using Engine.Content;
using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Assets;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.ECS.Prefabs;
using Engine.Serialization.Content;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Prefabs;
using Engine.Worlds.Prefabs;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Integration.Prefabs;

public sealed class PrefabAssetLifecycleIntegrationTests
{
    [Fact]
    public void CaptureSaveLoadInstantiate_PreservesHierarchyAndSpatialData()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"));

        var prefabDirectory = Path.Combine(
            directory,
            "Prefabs");

        Directory.CreateDirectory(prefabDirectory);

        try
        {
            // 1. Создаём исходную ECS-иерархию.
            using var sourceWorld = new Engine.ECS.World();

            var sourceRoot = sourceWorld.CreateEntity();
            var sourceChild = sourceWorld.CreateEntity();

            sourceWorld.Add(
                sourceRoot,
                new WorldTransform2D());

            sourceWorld.Add(
                sourceRoot,
                new WorldPositionComponent(
                    new WorldPosition(10, 20)));

            sourceWorld.Add(
                sourceChild,
                new TransformParent2D(sourceRoot));

            sourceWorld.Add(
                sourceChild,
                new LocalTransform2D());

            sourceWorld.Add(
                sourceChild,
                new WorldTransform2D());

            sourceWorld.Add(
                sourceChild,
                new WorldPositionComponent(
                    new WorldPosition(30, 40)));

            // 2. Захватываем иерархию в PrefabAsset.
            var prefab = PrefabAssetCapture.Create(
                "Crate",
                sourceWorld,
                sourceRoot);

            Assert.Equal(2, prefab.Entities.Count);
            Assert.Equal(new PrefabEntityId(1), prefab.RootEntity);

            // 3. Сохраняем ресурс.
            var registry = new EcsComponentSerializerRegistry();
            registry.RegisterEngineComponents();

            var saveLoad = new PrefabAssetSaveLoadService(registry);

            var prefabPath = Path.Combine(
                prefabDirectory,
                "Crate.prefab");

            saveLoad.Save(prefabPath, prefab);

            Assert.True(File.Exists(prefabPath));

            // 4. Загружаем через ContentManager.
            using var content = new ContentManager(
                new FileAssetSource(directory),
                new FileContentCatalog(directory),
                new ContentLoaderRegistry());

            content.Register(
                new PrefabAssetContentLoader(registry));

            var loaded = content.Load<PrefabAsset>(
                new AssetPath("Prefabs/Crate.prefab"));

            Assert.Equal("Crate", loaded.Name);
            Assert.Equal(2, loaded.Entities.Count);
            Assert.Equal(prefab.RootEntity, loaded.RootEntity);

            var childPrefabEntity = Assert.Single(
                loaded.Entities.Where(
                    entity => entity.Id != loaded.RootEntity));

            var serializedParent = (TransformParent2D)
                loaded.Entities
                    .Single(entity => entity.Id == childPrefabEntity.Id)
                    .Components
                    .Single(component =>
                        component.ComponentType == typeof(TransformParent2D))
                    .BoxedValue;

            Assert.Equal(
                new EntityId(loaded.RootEntity.Value, 0),
                serializedParent.Parent);

            // 5. Создаём новый World и Instantiate из загруженного ресурса.
            using var targetEcsWorld = new Engine.ECS.World();

            using var targetWorld = new Engine.Worlds.World(
                new ChunkSize(16, 16),
                targetEcsWorld);

            var instance = PrefabInstantiator.Instantiate(
                targetWorld,
                loaded,
                new WorldPosition(100, 200));

            var runtimeRoot = instance.Root;
            var runtimeChild = instance.GetEntity(childPrefabEntity.Id);

            Assert.True(targetEcsWorld.Exists(runtimeRoot));
            Assert.True(targetEcsWorld.Exists(runtimeChild));

            // 6. Проверяем переназначение иерархической ссылки.
            Assert.Equal(
                runtimeRoot,
                targetEcsWorld.Get<TransformParent2D>(
                    runtimeChild).Parent);

            // 7. Проверяем сохранение остальных компонентов.
            Assert.True(
                targetEcsWorld.Has<WorldTransform2D>(runtimeRoot));

            Assert.True(
                targetEcsWorld.Has<WorldTransform2D>(runtimeChild));

            Assert.True(
                targetEcsWorld.Has<LocalTransform2D>(runtimeChild));

            // 8. Проверяем перенос позиций относительно нового корня.
            Assert.Equal(
                new WorldPosition(100, 200),
                targetEcsWorld.Get<WorldPositionComponent>(
                    runtimeRoot).Position);

            Assert.Equal(
                new WorldPosition(120, 220),
                targetEcsWorld.Get<WorldPositionComponent>(
                    runtimeChild).Position);

            // 9. Проверяем SpatialEntityManager.
            Assert.True(targetWorld.SpatialEntities.Contains(runtimeRoot));
            Assert.True(targetWorld.SpatialEntities.Contains(runtimeChild));

            // 10. Проверяем регистрацию в разных пространственных чанках.
            var rootChunk = targetWorld.GetChunkPosition(
                new WorldPosition(100, 200));

            var childChunk = targetWorld.GetChunkPosition(
                new WorldPosition(120, 220));

            Assert.NotEqual(rootChunk, childChunk);

            Assert.True(
                targetWorld.SpatialIndex.Contains(
                    runtimeRoot,
                    rootChunk));

            Assert.True(
                targetWorld.SpatialIndex.Contains(
                    runtimeChild,
                    childChunk));

            Assert.Equal(2, targetWorld.SpatialIndex.EntityCount);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}