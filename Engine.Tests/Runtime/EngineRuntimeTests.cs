using Engine.Audio;
using Engine.Core.Application;
using Engine.Core.Assets;
using Engine.Core.Diagnostics;
using Engine.Graphics;
using Engine.Graphics.Cameras;
using Engine.Graphics.Commands;
using Engine.Graphics.Fonts;
using Engine.Graphics.Resources;
using Engine.Input;
using Engine.Runtime;

namespace Engine.Tests.Runtime;

public sealed class EngineRuntimeTests
{
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

    private static EngineRuntime CreateRuntime()
    {
        var camera =
            new Camera(
                new Engine.Core.Math.Vector2(
                    1280,
                    720));

        var services =
            new EngineRuntimeServices(
                new FakeGraphicsDevice(),
                new FakeInput(),
                camera, new FakeAudioManager());

        return new EngineRuntime(
            new EngineRuntimeOptions(),
            services);
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
        public IFontManager Fonts =>
    throw new NotSupportedException();
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