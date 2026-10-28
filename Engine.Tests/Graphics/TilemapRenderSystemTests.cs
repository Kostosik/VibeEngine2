using Engine.Audio;
using Engine.Camera;
using Engine.Content;
using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.Graphics;
using Engine.Graphics.Commands;
using Engine.Graphics.Fonts;
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;
using Engine.Graphics.Tilemaps;
using Engine.Graphics2D.Commands;
using Engine.Input;
using Engine.Runtime;
using Engine.Runtime.Tilemaps;
using Engine.Serialization.Content;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Text;

namespace Engine.Tests.Graphics;

public sealed class TilemapRenderSystemTests
{
    private const string MapPath =
        "Maps/Overworld.tilemap.json";

    private const string TileSetPath =
        "TileSets/terrain.tileset.json";

    private const string TexturePath =
        "Textures/test.png";

    [Fact]
    public void EngineGame_RenderRendersTilemapAndReleasesResources()
    {
        var textures = new FakeTextureManager();
        var graphics = new FakeGraphicsDevice(textures);

        using var content = CreateContent();

        using var runtime = new EngineRuntime(
            new EngineRuntimeOptions
            {
                ToolingEnabled = false
            },
            new EngineRuntimeServices(
                graphics,
                new FakeInput(),
                new Camera2D(new Vector2(100, 100)),
                new FakeAudioManager(),
                content));

        var entity = runtime.EcsWorld.CreateEntity();

        runtime.EcsWorld.Add(
            entity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromInt(10),
                    Fixed32.FromInt(12))));

        runtime.EcsWorld.Add(
            entity,
            new TilemapComponent(MapPath, layer: 17));

        var game = new TestGame(runtime);

        game.Initialize();
        game.Render(0.0);

        Assert.Equal(
            2,
            graphics.Commands.OfType<DrawWorldTextureCommand>().Count());

        game.Shutdown();

        Assert.False(
            content.IsLoaded<TilemapDefinition>(
                new AssetPath(MapPath)));

        Assert.False(
            content.IsLoaded<TileSetDefinition>(
                new AssetPath(TileSetPath)));

        Assert.False(
            content.IsLoaded<TextureData>(
                new AssetPath(TexturePath)));

        Assert.Equal(1, textures.CreateCount);
        Assert.Equal(1, textures.DestroyCount);
    }

    [Fact]
    public void EngineGame_WithoutTilemapsDoesNotLoadTilemapResources()
    {
        var textures = new FakeTextureManager();
        var graphics = new FakeGraphicsDevice(textures);

        using var content = CreateContent();

        using var runtime = new EngineRuntime(
            new EngineRuntimeOptions
            {
                ToolingEnabled = false
            },
            new EngineRuntimeServices(
                graphics,
                new FakeInput(),
                new Camera2D(new Vector2(100, 100)),
                new FakeAudioManager(),
                content));

        var game = new TestGame(runtime);

        game.Initialize();
        game.Render(0.0);
        game.Shutdown();

        Assert.Empty(graphics.Commands);

        Assert.False(
            content.IsLoaded<TilemapDefinition>(
                new AssetPath(MapPath)));

        Assert.False(
            content.IsLoaded<TileSetDefinition>(
                new AssetPath(TileSetPath)));

        Assert.Equal(0, textures.CreateCount);
    }

    [Fact]
    public void Render_LoadsTilemapAndRendersNonEmptyCellsWithTransform()
    {
        var textures = new FakeTextureManager();
        var graphics = new FakeGraphicsDevice(textures);

        using var content = CreateContent();
        using var world = new World();

        var entity = world.CreateEntity();

        var transform = new WorldTransform2D(
            new FixedVector2(
                Fixed32.FromInt(10),
                Fixed32.FromInt(12)))
        {
            Scale = new FixedVector2(
                Fixed32.FromInt(2),
                Fixed32.FromInt(3))
        };

        world.Add(entity, transform);
        world.Add(
            entity,
            new TilemapComponent(MapPath, layer: 17));

        var renderer = new TilemapRenderSystem(
            world,
            graphics,
            content,
            new Camera2D(new Vector2(100, 100)));

        try
        {
            renderer.Render();

            var commands = graphics.Commands
                .Select(command =>
                    Assert.IsType<DrawWorldTextureCommand>(command))
                .ToArray();

            Assert.Equal(2, commands.Length);

            Assert.Equal(
                new Vector2(10, 12),
                commands[0].Position);

            Assert.Equal(
                new Vector2(12, 15),
                commands[1].Position);

            Assert.All(
                commands,
                command =>
                {
                    Assert.Equal(
                        new Vector2(2, 3),
                        command.Size);

                    Assert.Equal(17, command.Layer);
                    Assert.True(command.Texture.IsValid);
                });
        }
        finally
        {
            renderer.Dispose();
        }

        Assert.False(
            content.IsLoaded<TilemapDefinition>(
                new AssetPath(MapPath)));

        Assert.False(
            content.IsLoaded<TileSetDefinition>(
                new AssetPath(TileSetPath)));

        Assert.False(
            content.IsLoaded<TextureData>(
                new AssetPath(TexturePath)));

        Assert.Equal(1, textures.CreateCount);
        Assert.Equal(1, textures.DestroyCount);
    }

    private sealed class TestGame : EngineGame
    {
        public TestGame(EngineRuntime runtime)
            : base(runtime)
        {
        }
    }

    private sealed class FakeInput : IInput
    {
        public void Update()
        {
        }

        public bool IsDown(InputAction action) => false;

        public bool IsPressed(InputAction action) => false;

        public bool IsReleased(InputAction action) => false;

        public float GetValue(InputAction action) => 0.0f;
    }

    private sealed class FakeAudioManager : IAudioManager
    {
        public IAudioListener CreateListener() =>
            throw new NotSupportedException();

        public IAudioBuffer Load(AssetPath assetPath) =>
            throw new NotSupportedException();

        public IAudioSource CreateSource(IAudioBuffer buffer) =>
            throw new NotSupportedException();

        public IAudioSource Play(IAudioBuffer buffer) =>
            throw new NotSupportedException();

        public void Dispose()
        {
        }
    }

    private static ContentManager CreateContent()
    {
        var assets = new Dictionary<string, byte[]>
        {
            [MapPath] = Encoding.UTF8.GetBytes(
                """
                {
                  "formatVersion": 1,
                  "tileSetPath": "TileSets/terrain.tileset.json",
                  "width": 2,
                  "height": 2,
                  "tiles": [1, 0, 0, 1]
                }
                """),

            [TileSetPath] = Encoding.UTF8.GetBytes(
                """
                {
                  "formatVersion": 1,
                  "atlases": [
                    {
                      "id": "terrain",
                      "texturePath": "Textures/test.png",
                      "tileWidth": 1,
                      "tileHeight": 1
                    }
                  ],
                  "tiles": [
                    {
                      "id": 1,
                      "name": "Grass",
                      "atlasId": "terrain",
                      "atlasIndex": 0
                    }
                  ]
                }
                """),

            [TexturePath] = CreatePng()
        };

        var source = new MemoryAssetSource(assets);
        var catalog = new MemoryContentCatalog(assets);
        var loaders = new ContentLoaderRegistry();

        loaders.Register(new ImageTextureContentLoader());
        loaders.Register(new JsonTileSetContentLoader());
        loaders.Register(new JsonTilemapContentLoader());

        return new ContentManager(source, catalog, loaders);
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

        public MemoryContentCatalog(
            IReadOnlyDictionary<string, byte[]> assets)
        {
            foreach (var pair in assets)
            {
                var path = new AssetPath(pair.Key);

                _assets.Add(
                    path,
                    new ContentAsset(
                        path,
                        Path.GetExtension(pair.Key),
                        pair.Value.LongLength,
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
        private readonly Dictionary<TextureHandle, TextureDescription>
            _textures = new();

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

        public RenderPipeline Pipeline => throw new NotSupportedException();

        public IFontManager Fonts => throw new NotSupportedException();

        public IRenderTargetManager RenderTargets =>
            throw new NotSupportedException();

        public IShaderManager Shaders => throw new NotSupportedException();

        public IGraphicsBufferManager Buffers =>
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