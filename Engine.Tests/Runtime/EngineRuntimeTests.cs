using Engine.Audio;
using Engine.Camera;
using Engine.Content;
using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Application;
using Engine.Core.Assets;
using Engine.Core.Diagnostics;
using Engine.Core.Math;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.Graphics;
using Engine.Graphics.Commands;
using Engine.Graphics.Fonts;
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;
using Engine.Input;
using Engine.Physics;
using Engine.Runtime;
using Engine.Scripting;
using Engine.Worlds.Chunks;
using Engine.Worlds.Spatial;
using Engine.Worlds.Tiles;


namespace Engine.Tests.Runtime;

public sealed class EngineRuntimeTests
{
    [Fact]
    public void RuntimeSchedulesScriptsAndDisposesThem()
    {
        using var runtime = CreateRuntime();

        var script = new RuntimeTestScript();

        runtime.ScriptRegistry.Register(
            "test.runtime",
            () => script);

        runtime.Initialize();

        var entity = runtime.EcsWorld.CreateEntity();

        runtime.Scripts.Attach(
            entity,
            "test.runtime");

        runtime.Update(default);
        runtime.FixedUpdate(default);

        Assert.Equal(1, script.UpdateCount);
        Assert.Equal(1, script.FixedUpdateCount);

        runtime.Dispose();

        Assert.Equal(1, script.DetachCount);
    }

    [Fact]
    public void StateHash_ChangesWhenChunkTilesOrSimulationStateChanges()
    {
        using var runtime =
            new EngineRuntime(
                new EngineRuntimeOptions
                {
                    ChunkSize = new ChunkSize(4, 4),
                    ToolingEnabled = false
                });

        runtime.Initialize();

        var position =
            new ChunkPosition(-2, 3);

        try
        {
            var emptyHash =
                runtime.GetStateHash();

            var chunk =
                runtime.World.CreateChunk(
                    position);

            var chunkHash =
                runtime.GetStateHash();

            Assert.NotEqual(
                emptyHash,
                chunkHash);

            chunk.Tiles.Set(
                new LocalPosition(1, 2),
                new Tile(42));

            var tileHash =
                runtime.GetStateHash();

            Assert.NotEqual(
                chunkHash,
                tileHash);

            runtime.World.Chunks
                .Get(position)
                .Lifecycle
                .SetSimulation(
                    ChunkSimulationState.Suspended);

            var suspendedHash =
                runtime.GetStateHash();

            Assert.NotEqual(
                tileHash,
                suspendedHash);
        }
        finally
        {
            ClearChunks(runtime);
        }
    }

    [Fact]
    public void Dispose_DisposesWorldChunksBeforeEcsWorld()
    {
        var runtime =
            new EngineRuntime(
                new EngineRuntimeOptions
                {
                    ChunkSize = new ChunkSize(4, 4),
                    ToolingEnabled = false
                });

        var chunk =
            runtime.World.CreateChunk(
                new ChunkPosition(2, -1));

        chunk.Tiles.Set(
            new LocalPosition(1, 1),
            new Tile(42));

        runtime.Dispose();

        Assert.Equal(
            0,
            runtime.World.ChunkCount);

        Assert.Throws<ObjectDisposedException>(
            () => chunk.Tiles.Get(
                new LocalPosition(1, 1)));

        Assert.Throws<ObjectDisposedException>(
            () => _ = runtime.EcsWorld.EntityCount);

        // Повторный Dispose безопасен.
        runtime.Dispose();
    }

    [Fact]
    public void StateHash_IgnoresChunkOrderPresentationAndResidency()
    {
        var options =
            new EngineRuntimeOptions
            {
                ChunkSize = new ChunkSize(4, 4),
                ToolingEnabled = false
            };

        using var first =
            new EngineRuntime(options);

        using var second =
            new EngineRuntime(options);

        first.Initialize();
        second.Initialize();

        var firstPosition =
            new ChunkPosition(-1, 3);

        var secondPosition =
            new ChunkPosition(2, -4);

        try
        {
            // Первый мир: чанки остаются загруженными.
            AddHashTestChunk(
                first,
                firstPosition,
                101,
                ChunkPresentationState.Relevant);

            AddHashTestChunk(
                first,
                secondPosition,
                202,
                ChunkPresentationState.Irrelevant);

            // Второй мир: порядок регистрации обратный.
            AddHashTestChunk(
                second,
                secondPosition,
                202,
                ChunkPresentationState.Relevant);

            AddHashTestChunk(
                second,
                firstPosition,
                101,
                ChunkPresentationState.Irrelevant);

            // Логическое содержимое не меняется при выгрузке.
            Assert.True(
                second.World.UnloadChunk(
                    firstPosition));

            Assert.Equal(
                first.GetStateHash(),
                second.GetStateHash());
        }
        finally
        {
            ClearChunks(first);
            ClearChunks(second);
        }
    }

    private static void AddHashTestChunk(
        EngineRuntime runtime,
        ChunkPosition position,
        uint tileValue,
        ChunkPresentationState presentation)
    {
        var chunk =
            runtime.World.CreateChunk(
                position);

        chunk.Tiles.Set(
            new LocalPosition(1, 1),
            new Tile(tileValue));

        var lifecycle =
            runtime.World.Chunks
                .Get(position)
                .Lifecycle;

        lifecycle.SetSimulation(
            ChunkSimulationState.Suspended);

        lifecycle.SetPresentation(
            presentation);
    }

    private static void ClearChunks(
        EngineRuntime runtime)
    {
        foreach (var record in
                 runtime.World.GetChunkRecords().ToArray())
        {
            if (record.Chunk is not null &&
                record.Lifecycle.Simulation !=
                    ChunkSimulationState.Suspended)
            {
                record.Lifecycle.SetSimulation(
                    ChunkSimulationState.Suspended);
            }

            runtime.World.RemoveChunk(
                record.Lifecycle.Position);
        }
    }

    [Fact]
    public void EngineRuntimeOptions_PreservesDefaults()
    {
        var options =
            new EngineRuntimeOptions();

        Assert.Equal(
            new ChunkSize(32, 32),
            options.ChunkSize);

        Assert.Equal(
            new SimulationRate(20),
            options.SimulationRate);

        Assert.Equal(
            4,
            options.Physics.Substeps);

        Assert.True(
            options.ToolingEnabled);
    }

    [Fact]
    public void RuntimeUsesConfiguredOptions()
    {
        var options =
            new EngineRuntimeOptions
            {
                ChunkSize =
                    new ChunkSize(64, 48),

                SimulationRate =
                    new SimulationRate(30),

                Physics =
                    new PhysicsSettings2D
                    {
                        VelocityIterations = 8
                    },

                ToolingEnabled = false
            };

        using var runtime =
            CreateRuntime(options);

        Assert.Equal(
            new ChunkSize(64, 48),
            runtime.World.ChunkSize);

        Assert.Equal(
            new SimulationRate(30),
            runtime.SimulationRate);

        Assert.Equal(
            8,
            runtime.PhysicsSettings.VelocityIterations);

        Assert.Null(
            runtime.Console);
    }

    [Fact]
    public void InitializeAndShutdownChangeRuntimeState()
    {
        using var runtime =
            CreateRuntime();

        Assert.False(
            runtime.IsInitialized);

        Assert.False(
            runtime.IsShutdown);

        runtime.Initialize();

        Assert.True(
            runtime.IsInitialized);

        Assert.False(
            runtime.IsShutdown);

        runtime.Shutdown();

        Assert.True(
            runtime.IsInitialized);

        Assert.True(
            runtime.IsShutdown);
    }

    [Fact]
    public void DisposeShutsDownInitializedRuntime()
    {
        var runtime =
            CreateRuntime();

        runtime.Initialize();

        runtime.Dispose();

        Assert.True(
            runtime.IsShutdown);

        runtime.Dispose();
    }

    [Fact]
    public void DebugCommandsControlGameLoop()
    {
        using var runtime =
            CreateRuntime();

        var loop =
            new FakeGameLoopController();

        runtime.AttachGameLoopController(
            loop);

        Assert.NotNull(
            runtime.DebugExecution);

        Assert.NotNull(
            runtime.Console);

        var pauseResult =
            runtime.Console!.Execute(
                "pause");

        Assert.True(
            pauseResult.Success);

        Assert.True(
            loop.IsPaused);

        var stepResult =
            runtime.Console.Execute(
                "step");

        Assert.True(
            stepResult.Success);

        Assert.Equal(
            1,
            loop.StepCount);

        var resumeResult =
            runtime.Console.Execute(
                "resume");

        Assert.True(
            resumeResult.Success);

        Assert.False(
            loop.IsPaused);
    }

    [Fact]
    public void DiagnosticsReachCollector()
    {
        using var runtime =
            CreateRuntime();

        runtime.DiagnosticReporter.Error(
            "Runtime.Test",
            "Test diagnostic.");

        Assert.Single(
            runtime.DiagnosticCollector.Diagnostics);

        var diagnostic =
            runtime.DiagnosticCollector.Diagnostics[0];

        Assert.Equal(
            DiagnosticLevel.Error,
            diagnostic.Level);

        Assert.Equal(
            "Engine.Runtime.Test",
            diagnostic.Code);

        Assert.Equal(
            "Test diagnostic.",
            diagnostic.Message);
    }

    [Fact]
    public void FatalErrorReportsCriticalDiagnosticAndThrows()
    {
        using var runtime =
            CreateRuntime();

        var exception =
            Assert.Throws<FatalEngineException>(
                () =>
                    runtime.FatalError.Throw(
                        "Runtime.Fatal",
                        "Fatal test."));

        Assert.Contains(
            "Runtime.Fatal",
            exception.Message);

        Assert.Single(
            runtime.DiagnosticCollector.Diagnostics);

        var diagnostic =
            runtime.DiagnosticCollector.Diagnostics[0];

        Assert.Equal(
            DiagnosticLevel.Critical,
            diagnostic.Level);

        Assert.Equal(
            "Engine.Runtime.Fatal",
            diagnostic.Code);

        Assert.Equal(
            "Fatal test.",
            diagnostic.Message);
    }

    [Fact]
    public void CustomFixedSystem_ExecutesAfterInitialization()
    {
        using var runtime =
            CreateRuntime();

        var system =
            new CountingFixedSystem();

        runtime.Scheduler.Add(
            system,
            SystemPhase.FixedUpdate);

        runtime.Initialize();

        runtime.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.FromRatio(1, 20),
                    new Tick(1))));

        Assert.Equal(
            1,
            system.ExecutionCount);
    }

    [Fact]
    public void HeadlessRuntime_CanInitializeAndShutdownWithoutApplicationServices()
    {
        using var runtime =
            new EngineRuntime(
                new EngineRuntimeOptions
                {
                    ToolingEnabled = false
                });

        Assert.NotNull(runtime.EcsWorld);
        Assert.NotNull(runtime.World);
        Assert.NotNull(runtime.Simulation);
        Assert.NotNull(runtime.Physics);

        runtime.Initialize();

        Assert.True(runtime.IsInitialized);
        Assert.False(runtime.IsShutdown);

        runtime.Shutdown();

        Assert.True(runtime.IsShutdown);

        Assert.Throws<InvalidOperationException>(
            () => _ = runtime.Services.Graphics);
    }

    [Fact]
    public void Dispose_DisposesOwnedEcsWorld()
    {
        var runtime =
            CreateRuntime();

        var ecsWorld =
            runtime.EcsWorld;

        runtime.Dispose();

        Assert.Throws<ObjectDisposedException>(
            () =>
            {
                _ =
                    ecsWorld.EntityCount;
            });
    }

    private sealed class CountingFixedSystem :
    IFixedUpdateSystem
    {
        public int ExecutionCount { get; private set; }

        public void FixedUpdate(
            FixedSystemContext context)
        {
            ExecutionCount++;
        }
    }

    private static EngineRuntime CreateRuntime(
        EngineRuntimeOptions? options = null)
    {
        var camera =
            new Camera2D(
                new Engine.Core.Math.Vector2(
                    1280,
                    720));

        var services =
            new EngineRuntimeServices(
                new FakeGraphicsDevice(),
                new FakeInput(),
                camera,
                new FakeAudioManager(),
                new ContentManager(
                    new EmptyAssetSource(),
                    new EmptyContentCatalog(),
                    new ContentLoaderRegistry()));

        return new EngineRuntime(
            options ?? new EngineRuntimeOptions(),
            services);
    }

    private sealed class EmptyAssetSource :
    IAssetSource
    {
        public ReadOnlyMemory<byte> Load(
            AssetPath path)
        {
            throw new FileNotFoundException(
                $"Test asset '{path}' does not exist.");
        }
    }

    private sealed class EmptyContentCatalog :
        IContentCatalog
    {
        public IReadOnlyList<ContentAsset> GetAssets()
        {
            return Array.Empty<ContentAsset>();
        }

        public bool Contains(
            AssetPath path)
        {
            return false;
        }

        public bool TryGet(
            AssetPath path,
            out ContentAsset? asset)
        {
            asset = null;

            return false;
        }

        public void Refresh()
        {
        }
    }

    private sealed class FakeAudioManager : IAudioManager
    {
        public IAudioListener CreateListener()
        {
            return null;
        }
        public IAudioBuffer Load(AssetPath assetPath)
        {
            return null;
        }

        public IAudioSource CreateSource(IAudioBuffer buffer) { return null; }

        public IAudioSource Play(IAudioBuffer buffer) { return null; }

        public void Dispose()
        {

        }
    }

    private sealed class FakeGraphicsDevice :
        IGraphicsDevice
    {
        public RenderPipeline Pipeline => throw new NotSupportedException();
        public IFontManager Fonts =>
    throw new NotSupportedException();
        public IRenderTargetManager RenderTargets => throw new NotSupportedException();
        public IGraphicsBufferManager Buffers => throw new NotSupportedException();
        public IShaderManager Shaders => throw new NotSupportedException();
        public ITextureManager Textures =>
            throw new NotSupportedException();

        public void BeginFrame()
        {
        }

        public void Submit(
            IRenderCommand command)
        {
        }

        public void EndFrame()
        {
        }
    }

    private sealed class FakeInput :
        IInput
    {
        public void Update()
        {
        }

        public bool IsDown(
            InputAction action)
        {
            return false;
        }

        public bool IsPressed(
            InputAction action)
        {
            return false;
        }

        public bool IsReleased(
            InputAction action)
        {
            return false;
        }

        public float GetValue(
            InputAction action)
        {
            return 0.0f;
        }
    }

    private sealed class RuntimeTestScript : IEntityScript
    {
        public int UpdateCount { get; private set; }

        public int FixedUpdateCount { get; private set; }

        public int DetachCount { get; private set; }

        public void Update(
            ScriptContext context,
            TimeSnapshot time)
        {
            UpdateCount++;
        }

        public void FixedUpdate(
            ScriptContext context,
            SimulationTime time)
        {
            FixedUpdateCount++;
        }

        public void OnDetach(ScriptContext context)
        {
            DetachCount++;
        }
    }

    private sealed class FakeGameLoopController :
        IGameLoopController
    {
        public bool IsPaused { get; private set; }

        public int StepCount { get; private set; }

        public void Pause()
        {
            IsPaused = true;
        }

        public void Resume()
        {
            IsPaused = false;
        }

        public bool RequestStep()
        {
            if (!IsPaused)
            {
                return false;
            }

            StepCount++;

            return true;
        }
    }
}