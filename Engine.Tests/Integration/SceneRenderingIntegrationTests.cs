
using Engine.Content;
using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.Graphics;
using Engine.Graphics.Commands;
using Engine.Graphics.Fonts;
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;
using Engine.Graphics.Sprites;
using Engine.Graphics2D.Commands;
using Engine.Graphics2D.Sprites;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Integration;

public sealed class SceneRenderingIntegrationTests
{
    [Fact]
    public void SceneReload_RestoresSpriteAndRendersItsTexture()
    {
        var scenePath = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngine-{Guid.NewGuid():N}.world");

        var textures = new FakeTextureManager();
        var graphics = new FakeGraphicsDevice(textures);

        using var content = CreateContent();
        using var sourceEcsWorld = new Engine.ECS.World();

        var sourceWorld = new Engine.Worlds.World(
            new ChunkSize(16, 16),
            sourceEcsWorld);

        try
        {
            var entity = sourceEcsWorld.CreateEntity();

            sourceEcsWorld.Add(
                entity,
                new WorldTransform2D(
                    new FixedVector2(
                        Fixed32.FromFloat(10.0f),
                        Fixed32.FromFloat(12.0f))));

            sourceEcsWorld.Add(
                entity,
                new SpriteComponent(
                    "Textures/test.png",
                    4.0f,
                    2.0f,
                    25));

            var serializers =
                new EcsComponentSerializerRegistry();

            serializers.RegisterEngineComponents();

            var saveLoad =
                new WorldSaveLoadService(serializers);

            saveLoad.Save(scenePath, sourceWorld);

            var restoredWorld =
                saveLoad.LoadWorld(scenePath);

            try
            {
                var restoredEntityCount = 0;

                foreach (var restoredEntity in
                         restoredWorld.EcsWorld.Query<
                             SpriteComponent,
                             WorldTransform2D>())
                {
                    restoredEntityCount++;

                    Assert.Equal(
                        "Textures/test.png",
                        restoredEntity.First.TexturePath);

                    Assert.Equal(4.0f, restoredEntity.First.Width);
                    Assert.Equal(2.0f, restoredEntity.First.Height);
                    Assert.Equal(25, restoredEntity.First.Layer);
                }

                Assert.Equal(1, restoredEntityCount);

                using var renderer = new SpriteRenderSystem(
                    restoredWorld.EcsWorld,
                    graphics,
                    content,
                    new Engine.Camera.Camera2D(
                        new Vector2(100.0f, 100.0f)));

                renderer.Render();

                var command =
                    Assert.IsType<DrawWorldTextureCommand>(
                        Assert.Single(graphics.Commands));

                Assert.True(command.Texture.IsValid);

                Assert.Equal(
                    new Vector2(8.0f, 11.0f),
                    command.Position);

                Assert.Equal(
                    new Vector2(4.0f, 2.0f),
                    command.Size);

                Assert.Equal(25, command.Layer);
                Assert.Equal(1, textures.CreateCount);
            }
            finally
            {
                try
                {
                    restoredWorld.Dispose();
                }
                finally
                {
                    restoredWorld.EcsWorld.Dispose();
                }
            }
        }
        finally
        {
            try
            {
                sourceWorld.Dispose();
            }
            finally
            {
                if (File.Exists(scenePath))
                {
                    File.Delete(scenePath);
                }
            }
        }
    }

    private static ContentManager CreateContent()
    {
        var path = new AssetPath("Textures/test.png");

        var assets = new MemoryAssetSource(
            new Dictionary<string, byte[]>
            {
                [path.Value] = CreatePng()
            });

        var catalog = new MemoryContentCatalog(path);
        var loaders = new ContentLoaderRegistry();

        loaders.Register(new ImageTextureContentLoader());

        return new ContentManager(
            assets,
            catalog,
            loaders);
    }

    private static byte[] CreatePng()
    {
        return
        [
            137, 80, 78, 71, 13, 10, 26, 10,
            0, 0, 0, 13, 73, 72, 68, 82,
            0, 0, 0, 1, 0, 0, 0, 1,
            8, 6, 0, 0, 0, 31, 21, 196, 137,
            0, 0, 0, 13, 73, 68, 65, 84,
            8, 215, 99, 248, 207, 192, 240, 31,
            0, 5, 0, 1, 255, 137, 153, 61, 29,
            0, 0, 0, 0, 73, 69, 78, 68,
            174, 66, 96, 130
        ];
    }

    private sealed class MemoryAssetSource : IAssetSource
    {
        private readonly IReadOnlyDictionary<string, byte[]> _assets;

        public MemoryAssetSource(
            IReadOnlyDictionary<string, byte[]> assets)
        {
            _assets = assets;
        }

        public ReadOnlyMemory<byte> Load(AssetPath path)
        {
            return _assets[path.Value];
        }
    }

    private sealed class MemoryContentCatalog : IContentCatalog
    {
        private readonly Dictionary<AssetPath, ContentAsset> _assets = new();

        public MemoryContentCatalog(params AssetPath[] paths)
        {
            foreach (var path in paths)
            {
                _assets.Add(
                    path,
                    new ContentAsset(
                        path,
                        Path.GetExtension(path.Value),
                        0,
                        DateTime.UnixEpoch));
            }
        }

        public void Refresh()
        {
        }

        public IReadOnlyList<ContentAsset> GetAssets()
        {
            return _assets.Values.ToArray();
        }

        public bool Contains(AssetPath path)
        {
            return _assets.ContainsKey(path);
        }

        public bool TryGet(
            AssetPath path,
            out ContentAsset? asset)
        {
            return _assets.TryGetValue(path, out asset);
        }
    }

    private sealed class FakeTextureManager : ITextureManager
    {
        private readonly Dictionary<
            TextureHandle,
            TextureDescription> _textures = new();

        private uint _nextId = 1;

        public int CreateCount { get; private set; }

        public int DestroyCount { get; private set; }

        public TextureHandle Create(TextureDescription description)
        {
            var handle = new TextureHandle(_nextId++);

            _textures.Add(handle, description);
            CreateCount++;

            return handle;
        }

        public TextureHandle Create(TextureData data)
        {
            return Create(data.Description);
        }

        public bool Exists(TextureHandle texture)
        {
            return _textures.ContainsKey(texture);
        }

        public TextureDescription GetDescription(TextureHandle texture)
        {
            return _textures[texture];
        }

        public void Destroy(TextureHandle texture)
        {
            if (_textures.Remove(texture))
            {
                DestroyCount++;
            }
        }
    }

    private sealed class FakeGraphicsDevice : IGraphicsDevice
    {
        public FakeGraphicsDevice(ITextureManager textures)
        {
            Textures = textures;
        }

        public ITextureManager Textures { get; }

        public List<IRenderCommand> Commands { get; } = new();

        public IFontManager Fonts =>
            throw new NotSupportedException();

        public IGraphicsBufferManager Buffers =>
            throw new NotSupportedException();

        public IShaderManager Shaders =>
            throw new NotSupportedException();

        public IRenderTargetManager RenderTargets =>
            throw new NotSupportedException();

        public RenderPipeline Pipeline =>
            throw new NotSupportedException();

        public void BeginFrame()
        {
        }

        public void Submit(IRenderCommand command)
        {
            Commands.Add(command);
        }

        public void EndFrame()
        {
        }
    }
}
