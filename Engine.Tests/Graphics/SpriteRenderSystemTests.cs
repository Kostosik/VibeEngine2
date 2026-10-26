using Engine.Audio;
using Engine.Camera;
using Engine.Content;
using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Assets;
using Engine.Core.Determinism;
using Engine.Core.Math;
using Engine.Core.Time;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.Graphics;
using Engine.Graphics.Commands;
using Engine.Graphics.Fonts;
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;
using Engine.Graphics.Sprites;
using Engine.Graphics2D.Commands;
using Engine.Graphics2D.Particles;
using Engine.Graphics2D.Sprites;
using Engine.Input;
using Engine.Particles;
using Engine.Particles.Emission;
using Engine.Particles.Particles;
using Engine.Runtime;
using Engine.Runtime.Particles;
using Engine.Serialization.Content;
using System.Text;

namespace Engine.Tests.Graphics;

public sealed class SpriteRenderSystemTests
{
    [Fact]
    public void ParticleRenderer_RendersParticlesAndReusesTexture()
    {
        var textures = new FakeTextureManager();
        var graphics = new FakeGraphicsDevice(textures);

        using var content = CreateContent();

        var particleSystem = new ParticleSystem(4);

        particleSystem.Spawn(
            new Particle(
                new Vector3(10.0f, 12.0f, 0.0f),
                Vector3.Zero,
                lifetimeSeconds: 5.0f,
                size: 4.0f));

        var camera = new Camera2D(
            new Vector2(100.0f, 100.0f));

        using var renderer = new ParticleRenderer(
            graphics,
            content,
            camera);

        renderer.Render(
            particleSystem,
            "Textures/test.png",
            layer: 25);

        var command = Assert.IsType<DrawWorldTextureCommand>(
            Assert.Single(graphics.Commands));

        Assert.True(command.Texture.IsValid);

        Assert.Equal(
            new Vector2(8.0f, 10.0f),
            command.Position);

        Assert.Equal(
            new Vector2(4.0f, 4.0f),
            command.Size);

        Assert.Equal(25, command.Layer);
        Assert.Equal(1, textures.CreateCount);

        graphics.Commands.Clear();

        renderer.Render(
            particleSystem,
            "Textures/test.png",
            layer: 25);

        Assert.Single(graphics.Commands);
        Assert.Equal(1, textures.CreateCount);
    }

    [Fact]
    public void EngineGame_UpdatesAndRendersEmitterAttachedToEntity()
    {
        var textures = new FakeTextureManager();
        var graphics = new FakeGraphicsDevice(textures);

        using var content = CreateContent();

        var camera = new Camera2D(
            new Vector2(100.0f, 100.0f));

        var runtime = new EngineRuntime(
            new EngineRuntimeOptions
            {
                ToolingEnabled = false
            },
            new EngineRuntimeServices(
                graphics,
                new FakeInput(),
                camera,
                new FakeAudioManager(),
                content));

        var entity = runtime.EcsWorld.CreateEntity();

        runtime.EcsWorld.Add(
            entity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromFloat(10.0f),
                    Fixed32.FromFloat(12.0f))));

        var emitter = runtime.Particles.CreateEmitter(
            entity,
            "Textures/test.png",
            new ParticleEmitter(10.0f),
            maxParticles: 8,
            factory: static (ref DeterministicRandom random) =>
                new Particle(
                    Vector3.Zero,
                    Vector3.Zero,
                    lifetimeSeconds: 5.0f,
                    size: 4.0f),
            seed: 123,
            layer: 25);

        var game = new TestEngineGame(runtime);

        try
        {
            game.Initialize();

            game.Update(
                new TimeSnapshot(
                    Duration.FromSeconds(0.1),
                    Duration.FromSeconds(0.1),
                    Tick.Zero));

            Assert.Equal(
                1,
                emitter.ParticleSystem.Count);

            game.Render(0.0);

            var command =
                Assert.IsType<DrawWorldTextureCommand>(
                    Assert.Single(graphics.Commands));

            Assert.Equal(
                new Vector2(8.0f, 10.0f),
                command.Position);

            Assert.Equal(
                new Vector2(4.0f, 4.0f),
                command.Size);

            Assert.Equal(25, command.Layer);
            Assert.Equal(1, textures.CreateCount);

            game.Shutdown();

            Assert.False(emitter.IsActive);
            Assert.Equal(1, textures.DestroyCount);
        }
        finally
        {
            runtime.Dispose();
        }
    }

    [Fact]
    public void Render_CullsSpritesOutsideCameraAndShowsThemAfterCameraMoves()
    {
        var textures = new FakeTextureManager();
        var graphics = new FakeGraphicsDevice(textures);

        using var content = CreateContent();
        using var world = new World();

        var entity = world.CreateEntity();

        world.Add(
            entity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromFloat(200.0f),
                    Fixed32.FromFloat(200.0f))));

        world.Add(
            entity,
            new SpriteComponent(
                "Textures/test.png",
                4.0f,
                4.0f));

        var camera = new Camera2D(new Vector2(100, 100));

        using var renderer = new SpriteRenderSystem(
            world,
            graphics,
            content,
            camera);

        // Объект находится за пределами видимой области.
        renderer.Render();

        Assert.Empty(graphics.Commands);

        // Перемещаем камеру к объекту.
        camera.Position = new Vector2(200, 200);

        renderer.Render();

        Assert.Single(graphics.Commands);

        Assert.IsType<DrawWorldTextureCommand>(
            graphics.Commands[0]);
    }

    [Fact]
    public void EngineGame_CameraMovementChangesParticleVisibility()
    {
        var textures = new FakeTextureManager();
        var graphics = new FakeGraphicsDevice(textures);

        using var content = CreateContent();

        var camera = new Camera2D(
            new Vector2(100.0f, 100.0f));

        var runtime = new EngineRuntime(
            new EngineRuntimeOptions
            {
                ToolingEnabled = false
            },
            new EngineRuntimeServices(
                graphics,
                new FakeInput(),
                camera,
                new FakeAudioManager(),
                content));

        var entity = runtime.EcsWorld.CreateEntity();

        runtime.EcsWorld.Add(
            entity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromFloat(200.0f),
                    Fixed32.FromFloat(200.0f))));

        var emitter = runtime.Particles.CreateEmitter(
            entity,
            "Textures/test.png",
            new ParticleEmitter(),
            maxParticles: 8,
            factory: static (ref DeterministicRandom random) =>
                new Particle(
                    Vector3.Zero,
                    Vector3.Zero,
                    lifetimeSeconds: 5.0f,
                    size: 4.0f),
            seed: 123);

        var game = new TestEngineGame(runtime);

        try
        {
            game.Initialize();

            Assert.Equal(1, emitter.Burst(1));

            // Эмиттер далеко за пределами текущего обзора камеры.
            game.Render(0.0);
            Assert.Empty(graphics.Commands);

            // Перемещаем ту же камеру, которой пользуется runtime.
            camera.Position = new Vector2(200.0f, 200.0f);

            game.Render(0.0);

            var command =
                Assert.IsType<DrawWorldTextureCommand>(
                    Assert.Single(graphics.Commands));

            Assert.Equal(
                new Vector2(198.0f, 198.0f),
                command.Position);

            Assert.Equal(
                new Vector2(4.0f, 4.0f),
                command.Size);

            game.Shutdown();

            Assert.False(emitter.IsActive);
        }
        finally
        {
            runtime.Dispose();
        }
    }

    [Fact]
    public void EngineGame_CanConfigureCameraAndCreateEmitterDuringInitialization()
    {
        var textures = new FakeTextureManager();
        var graphics = new FakeGraphicsDevice(textures);

        using var content = CreateContent();

        var camera = new Camera2D(
            new Vector2(100.0f, 100.0f));

        var runtime = new EngineRuntime(
            new EngineRuntimeOptions
            {
                ToolingEnabled = false
            },
            new EngineRuntimeServices(
                graphics,
                new FakeInput(),
                camera,
                new FakeAudioManager(),
                content));

        var game = new CameraAndParticleProbeGame(runtime);

        try
        {
            game.Initialize();

            Assert.NotNull(game.Emitter);
            Assert.Equal(
                new Vector2(200.0f, 200.0f),
                camera.Position);
            Assert.Equal(2.0f, camera.Zoom);

            game.Render(0.0);

            var command =
                Assert.IsType<DrawWorldTextureCommand>(
                    Assert.Single(graphics.Commands));

            Assert.Equal(
                new Vector2(198.0f, 198.0f),
                command.Position);

            graphics.Commands.Clear();

            // Та же камера определяет видимость частиц.
            camera.Position = Vector2.Zero;

            game.Render(0.0);

            Assert.Empty(graphics.Commands);

            game.Shutdown();

            Assert.False(game.Emitter!.IsActive);
            Assert.Equal(1, textures.DestroyCount);
        }
        finally
        {
            runtime.Dispose();
        }
    }

    [Fact]
    public void Dispose_ReleasesSpriteResourcesWhenGameWasAlreadyShutdown()
    {
        var textures = new FakeTextureManager();
        var graphics = new FakeGraphicsDevice(textures);

        using var content = CreateContent();

        var runtime = new EngineRuntime(
            new EngineRuntimeOptions(),
            new EngineRuntimeServices(
                graphics,
                new FakeInput(),
                new Camera2D(new Vector2(100.0f, 100.0f)),
                new FakeAudioManager(),
                content));

        var entity = runtime.EcsWorld.CreateEntity();

        runtime.EcsWorld.Add(
            entity,
            new WorldTransform2D());

        runtime.EcsWorld.Add(
            entity,
            new SpriteComponent("Textures/test.png"));

        var assetPath = new AssetPath("Textures/test.png");

        try
        {
            var game = new TestEngineGame(runtime);

            game.Initialize();
            game.Render(0.0);

            Assert.Equal(1, textures.CreateCount);
            Assert.True(content.IsLoaded<TextureData>(assetPath));

            // Сначала останавливаем игру.
            game.Shutdown();

            Assert.True(runtime.IsShutdown);

            // Затем освобождаем runtime.
            runtime.Dispose();

            Assert.Equal(1, textures.DestroyCount);

            // Текстура выгружена, но ContentManager продолжает работать.
            Assert.False(content.IsLoaded<TextureData>(assetPath));
        }
        finally
        {
            runtime.Dispose();
        }
    }

    [Fact]
    public void Render_SubmitsSpriteAndReusesLoadedTexture()
    {
        var textures =
            new FakeTextureManager();

        var graphics =
            new FakeGraphicsDevice(textures);

        using var content =
            CreateContent();

        using var world =
            new World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromFloat(5.0f),
                    Fixed32.FromFloat(6.0f))));

        world.Add(
            entity,
            new SpriteComponent(
                "Textures/test.png",
                4.0f,
                2.0f,
                25));

        var camera =
            new Camera2D(
                new Vector2(100.0f, 100.0f));

        using var renderer =
            new SpriteRenderSystem(
                world,
                graphics,
                content,
                camera);

        renderer.Render();

        var command =
            Assert.IsType<DrawWorldTextureCommand>(
                Assert.Single(graphics.Commands));

        Assert.True(command.Texture.IsValid);

        Assert.Equal(
            new Vector2(3.0f, 5.0f),
            command.Position);

        Assert.Equal(
            new Vector2(4.0f, 2.0f),
            command.Size);

        Assert.Equal(
            25,
            command.Layer);

        Assert.Equal(
            1,
            textures.CreateCount);

        graphics.Commands.Clear();

        renderer.Render();

        Assert.Single(graphics.Commands);

        Assert.Equal(
            1,
            textures.CreateCount);
    }

    private sealed class TestEngineGame : EngineGame
    {
        public TestEngineGame(EngineRuntime runtime)
            : base(runtime)
        {
        }
    }

    private static ContentManager CreateContent()
    {
        var path =
            new AssetPath("Textures/test.png");

        var assets =
            new MemoryAssetSource(
                new Dictionary<string, byte[]>
                {
                    [path.Value] = CreatePng()
                });

        var catalog =
            new MemoryContentCatalog(path);

        var loaders =
            new ContentLoaderRegistry();

        loaders.Register(
            new ImageTextureContentLoader());

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

    private sealed class MemoryAssetSource :
        IAssetSource
    {
        private readonly IReadOnlyDictionary<string, byte[]> _assets;

        public MemoryAssetSource(
            IReadOnlyDictionary<string, byte[]> assets)
        {
            _assets = assets;
        }

        public ReadOnlyMemory<byte> Load(
            AssetPath path)
        {
            return _assets[path.Value];
        }
    }

    private sealed class MemoryContentCatalog :
        IContentCatalog
    {
        private readonly Dictionary<AssetPath, ContentAsset> _assets = new();

        public MemoryContentCatalog(
            params AssetPath[] paths)
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
            return _assets.TryGetValue(
                path,
                out asset);
        }
    }

    private sealed class FakeInput : IInput
    {
        public void Update()
        {
        }

        public bool IsDown(InputAction action)
        {
            return false;
        }

        public bool IsPressed(InputAction action)
        {
            return false;
        }

        public bool IsReleased(InputAction action)
        {
            return false;
        }

        public float GetValue(InputAction action)
        {
            return 0.0f;
        }
    }

    private sealed class FakeAudioManager : IAudioManager
    {
        public IAudioListener CreateListener()
        {
            throw new NotSupportedException();
        }

        public IAudioBuffer Load(AssetPath assetPath)
        {
            throw new NotSupportedException();
        }

        public IAudioSource CreateSource(IAudioBuffer buffer)
        {
            throw new NotSupportedException();
        }

        public IAudioSource Play(IAudioBuffer buffer)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeTextureManager :
        ITextureManager
    {
        private readonly Dictionary<
            TextureHandle,
            TextureDescription> _textures = new();

        private uint _nextId = 1;
        public int DestroyCount { get; private set; }
        public int CreateCount { get; private set; }

        public TextureHandle Create(
            TextureDescription description)
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

        public TextureDescription GetDescription(
            TextureHandle texture)
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

    private sealed class CameraAndParticleProbeGame : EngineGame
    {
        public ParticleEmitterInstance? Emitter { get; private set; }

        public CameraAndParticleProbeGame(EngineRuntime runtime)
            : base(runtime)
        {
        }

        protected override void OnInitialize()
        {
            var camera = Runtime.Services.Camera;

            camera.Position = new Vector2(200.0f, 200.0f);
            camera.Zoom = 2.0f;

            var entity = Runtime.EcsWorld.CreateEntity();

            Runtime.EcsWorld.Add(
                entity,
                new WorldTransform2D(
                    new FixedVector2(
                        Fixed32.FromFloat(200.0f),
                        Fixed32.FromFloat(200.0f))));

            Emitter = Runtime.Particles.CreateEmitter(
                entity,
                "Textures/test.png",
                new ParticleEmitter(),
                maxParticles: 8,
                factory: static (ref DeterministicRandom random) =>
                    new Particle(
                        Vector3.Zero,
                        Vector3.Zero,
                        lifetimeSeconds: 5.0f,
                        size: 4.0f),
                seed: 123);

            Emitter.Burst(1);
        }
    }

    [Fact]
    public void EngineGame_CreatesParticleEmitterFromEntityComponent()
    {
        var textures = new FakeTextureManager();
        var graphics = new FakeGraphicsDevice(textures);

        using var content = CreateContentWithParticleEffect();

        var effectPath =
            new AssetPath("Particles/Smoke.particle.json");

        using var runtime = new EngineRuntime(
            new EngineRuntimeOptions
            {
                ToolingEnabled = false
            },
            new EngineRuntimeServices(
                graphics,
                new FakeInput(),
                new Camera2D(new Vector2(100.0f, 100.0f)),
                new FakeAudioManager(),
                content));

        var entity = runtime.EcsWorld.CreateEntity();

        runtime.EcsWorld.Add(
            entity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromFloat(10.0f),
                    Fixed32.FromFloat(12.0f))));

        runtime.EcsWorld.Add(
            entity,
            new ParticleEffectComponent
            {
                EffectPath = effectPath.Value,
                InitialBurstCount = 1
            });

        var game = new TestEngineGame(runtime);

        try
        {
            game.Initialize();

            game.Update(
                new TimeSnapshot(
                    Duration.FromSeconds(0.1),
                    Duration.FromSeconds(0.1),
                    Tick.Zero));

            // Runtime создал эмиттер без ручного CreateEmitter().
            Assert.Equal(1, runtime.Particles.EmitterCount);

            game.Render(0.0);

            var command =
                Assert.IsType<DrawWorldTextureCommand>(
                    Assert.Single(graphics.Commands));

            Assert.Equal(
                new Vector2(8.0f, 10.0f),
                command.Position);

            Assert.Equal(
                new Vector2(4.0f, 4.0f),
                command.Size);

            game.Shutdown();

            Assert.False(
                content.IsLoaded<ParticleEffectDefinition>(
                    effectPath));

            Assert.Equal(1, textures.DestroyCount);
        }
        finally
        {
            if (!runtime.IsShutdown)
            {
                game.Shutdown();
            }
        }
    }

    [Fact]
    public void EngineGame_ReconcilesParticleEffectComponentChanges()
    {
        var textures = new FakeTextureManager();
        var graphics = new FakeGraphicsDevice(textures);

        using var content = CreateContentWithParticleEffect();

        var smokePath =
            new AssetPath("Particles/Smoke.particle.json");

        var sparkPath =
            new AssetPath("Particles/Spark.particle.json");

        using var runtime = new EngineRuntime(
            new EngineRuntimeOptions
            {
                ToolingEnabled = false
            },
            new EngineRuntimeServices(
                graphics,
                new FakeInput(),
                new Camera2D(new Vector2(100.0f, 100.0f)),
                new FakeAudioManager(),
                content));

        var entity = runtime.EcsWorld.CreateEntity();

        runtime.EcsWorld.Add(
            entity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromFloat(10.0f),
                    Fixed32.FromFloat(12.0f))));

        runtime.EcsWorld.Add(
            entity,
            new ParticleEffectComponent
            {
                EffectPath = smokePath.Value,
                InitialBurstCount = 1
            });

        var game = new TestEngineGame(runtime);

        try
        {
            game.Initialize();

            var time = new TimeSnapshot(
                Duration.FromSeconds(0.1),
                Duration.FromSeconds(0.1),
                Tick.Zero);

            // Первый Update создаёт эмиттер из компонента.
            game.Update(time);

            Assert.Equal(1, runtime.Particles.EmitterCount);
            Assert.True(
                content.IsLoaded<ParticleEffectDefinition>(smokePath));

            game.Render(0.0);

            Assert.Single(graphics.Commands);
            graphics.Commands.Clear();

            // Меняем ресурс эффекта у той же сущности.
            ref var component =
                ref runtime.EcsWorld.Get<ParticleEffectComponent>(
                    entity);

            component.EffectPath = sparkPath.Value;

            game.Update(time);

            Assert.Equal(1, runtime.Particles.EmitterCount);

            Assert.False(
                content.IsLoaded<ParticleEffectDefinition>(smokePath));

            Assert.True(
                content.IsLoaded<ParticleEffectDefinition>(sparkPath));

            // Удаляем компонент: связанный эмиттер должен исчезнуть.
            Assert.True(
                runtime.EcsWorld.Remove<ParticleEffectComponent>(
                    entity));

            game.Update(time);

            Assert.Equal(0, runtime.Particles.EmitterCount);

            Assert.False(
                content.IsLoaded<ParticleEffectDefinition>(sparkPath));

            game.Shutdown();

            Assert.Equal(1, textures.DestroyCount);
        }
        finally
        {
            if (!runtime.IsShutdown)
            {
                game.Shutdown();
            }
        }
    }

    [Fact]
    public void Dispose_ReleasesParticleResourcesWithoutExplicitGameShutdown()
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
                new Camera2D(
                    new Vector2(100.0f, 100.0f)),
                new FakeAudioManager(),
                content));

        var game = new TestEngineGame(runtime);

        game.Initialize();

        var entity = runtime.EcsWorld.CreateEntity();

        runtime.EcsWorld.Add(
            entity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromFloat(10.0f),
                    Fixed32.FromFloat(12.0f))));

        var emitter = runtime.Particles.CreateEmitter(
            entity,
            "Textures/test.png",
            new ParticleEmitter(10.0f),
            maxParticles: 8,
            factory: static (ref DeterministicRandom random) =>
                new Particle(
                    Vector3.Zero,
                    Vector3.Zero,
                    lifetimeSeconds: 5.0f,
                    size: 4.0f));

        emitter.Burst(1);

        game.Render(0.0);

        Assert.Equal(1, textures.CreateCount);

        // Проверяем прямое освобождение runtime.
        // game.Shutdown() намеренно не вызывается.
        runtime.Dispose();

        Assert.Equal(1, textures.DestroyCount);
    }



    private static ContentManager CreateContentWithParticleEffect()
    {
        var texturePath =
            new AssetPath("Textures/test.png");

        var smokePath =
            new AssetPath("Particles/Smoke.particle.json");

        var sparkPath =
            new AssetPath("Particles/Spark.particle.json");

        static byte[] CreateEffectJson(int layer, int size)
        {
            var json = $$"""
    {
      "formatVersion": 2,
      "texturePath": "Textures/test.png",
      "layer": {{layer}},
      "emitter": {
        "ratePerSecond": 0,
        "maxParticles": 8,
        "shape": {
          "type": "point"
        }
      },
      "particle": {
        "lifetimeSeconds": {
          "min": 5,
          "max": 5
        },
        "size": {
          "min": {{size}},
          "max": {{size}}
        },
        "velocity": {
          "min": {
            "x": 0,
            "y": 0
          },
          "max": {
            "x": 0,
            "y": 0
          }
        },
        "modifiers": []
      }
    }
    """;

            return Encoding.UTF8.GetBytes(json);
        }

        var assets = new MemoryAssetSource(
            new Dictionary<string, byte[]>
            {
                [texturePath.Value] = CreatePng(),
                [smokePath.Value] = CreateEffectJson(25, 4),
                [sparkPath.Value] = CreateEffectJson(40, 2)
            });

        var catalog = new MemoryContentCatalog(
            texturePath,
            smokePath,
            sparkPath);

        var loaders = new ContentLoaderRegistry();

        loaders.Register(new ImageTextureContentLoader());
        loaders.Register(new JsonParticleEffectContentLoader());

        return new ContentManager(
            assets,
            catalog,
            loaders);
    }

    private sealed class FakeGraphicsDevice :
        IGraphicsDevice
    {
        public FakeGraphicsDevice(
            ITextureManager textures)
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