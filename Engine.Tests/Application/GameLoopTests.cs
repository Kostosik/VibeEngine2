using Engine.Core.Application;
using Engine.Core.Math;
using Engine.Core.Time;

namespace Engine.Tests.Application;

public sealed class GameLoopTests
{
    [Fact]
    public void PausedTimeIsNotAccumulatedForFixedUpdatesAfterResume()
    {
        var application = new TestApplication();
        var clock = new TestClock();

        var loop = CreateLoop(
            application,
            clock);

        loop.Initialize();
        loop.Pause();

        loop.Update(
            Duration.FromMilliseconds(250));

        Assert.Equal(
            0,
            application.FixedUpdateCount);

        loop.Resume();

        loop.Update(Duration.Zero);

        Assert.Equal(
            0,
            application.FixedUpdateCount);
    }

    [Fact]
    public void Shutdown_WhenClockStopThrowsStillShutsDownApplication()
    {
        var originalException =
            new InvalidOperationException("Clock stop failed.");

        var application = new TestApplication();
        var clock = new ThrowingStopClock(originalException);

        var loop = new GameLoop(
            application,
            new SimulationRate(20),
            clock,
            Duration.FromMilliseconds(250),
            8);

        loop.Initialize();

        var exception = Assert.Throws<InvalidOperationException>(
            () => loop.Shutdown());

        Assert.Same(originalException, exception);
        Assert.Equal(1, application.ShutdownCount);
        Assert.True(loop.IsShutdown);

        Assert.Throws<InvalidOperationException>(
            () => loop.Shutdown());

        Assert.Equal(1, application.ShutdownCount);
    }

    private sealed class ThrowingStopClock : IClock
    {
        private readonly Exception _exception;

        public TimeSpan Elapsed => TimeSpan.Zero;

        public bool IsRunning { get; private set; }

        public ThrowingStopClock(Exception exception)
        {
            _exception = exception;
        }

        public void Start()
        {
            IsRunning = true;
        }

        public void Restart()
        {
            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
            throw _exception;
        }
    }

    [Fact]
    public void Initialize_WhenApplicationThrows_ShutsDownApplicationAndPreservesOriginalException()
    {
        var originalException =
            new InvalidOperationException("Initialize failed.");

        var application =
            new ThrowingInitializeApplication(originalException);

        var clock = new TestClock();

        var loop = new GameLoop(
            application,
            new SimulationRate(20),
            clock,
            Duration.FromMilliseconds(250),
            8);

        var exception = Assert.Throws<InvalidOperationException>(
            () => loop.Initialize());

        Assert.Same(originalException, exception);
        Assert.Equal(1, application.ShutdownCount);
        Assert.False(loop.IsInitialized);
        Assert.True(loop.IsShutdown);
        Assert.False(clock.IsRunning);

        // Не разрешаем повторно запускать экземпляр
        // после неудачной инициализации.
        Assert.Throws<InvalidOperationException>(
            () => loop.Initialize());

        Assert.Equal(1, application.InitializeCount);
        Assert.Equal(1, application.ShutdownCount);
    }

    private sealed class ThrowingInitializeApplication
        : Engine.Core.Application.Application
    {
        private readonly Exception _exception;

        public int InitializeCount { get; private set; }

        public int ShutdownCount { get; private set; }

        public ThrowingInitializeApplication(Exception exception)
        {
            _exception = exception;
        }

        public override void Initialize()
        {
            InitializeCount++;
            throw _exception;
        }

        public override void Shutdown()
        {
            ShutdownCount++;
        }
    }


    [Fact]
    public void ShutdownFailureStillMarksLoopAsShutdown()
    {
        var application = new ThrowingShutdownApplication(new InvalidOperationException());
        var clock = new TestClock();

        var loop = new GameLoop(
            application,
            new SimulationRate(20),
            clock,
            Duration.FromMilliseconds(250),
            8);

        loop.Initialize();

        Assert.Throws<InvalidOperationException>(
            () => loop.Shutdown());

        Assert.False(clock.IsRunning);
        Assert.True(loop.IsShutdown);
        Assert.Equal(1, application.ShutdownCount);

        Assert.Throws<InvalidOperationException>(
            () => loop.Update(Duration.FromMilliseconds(16)));

        Assert.Throws<InvalidOperationException>(
            () => loop.Shutdown());

        Assert.Equal(1, application.ShutdownCount);
    }

    [Fact]
    public void Shutdown_WhenApplicationThrows_MarksLoopAsShutdown()
    {
        var originalException =
            new InvalidOperationException("Shutdown failed.");

        var application =
            new ThrowingShutdownApplication(originalException);

        var clock = new TestClock();

        var loop = new GameLoop(
            application,
            new SimulationRate(20),
            clock,
            Duration.FromMilliseconds(250),
            8);

        loop.Initialize();

        var exception = Assert.Throws<InvalidOperationException>(
            () => loop.Shutdown());

        Assert.Same(originalException, exception);
        Assert.False(clock.IsRunning);
        Assert.True(loop.IsShutdown);
        Assert.Equal(1, application.ShutdownCount);

        Assert.Throws<InvalidOperationException>(
            () => loop.Shutdown());

        Assert.Equal(1, application.ShutdownCount);
    }

    private sealed class ThrowingShutdownApplication
        : Engine.Core.Application.Application
    {
        private readonly Exception _exception;

        public int ShutdownCount { get; private set; }

        public ThrowingShutdownApplication(Exception exception)
        {
            _exception = exception;
        }

        public override void Shutdown()
        {
            ShutdownCount++;
            throw _exception;
        }
    }

    [Fact]
    public void InitializeInitializesApplicationAndStartsClock()
    {
        var application =
            new TestApplication();

        var clock =
            new TestClock();

        var loop =
            CreateLoop(
                application,
                clock);

        loop.Initialize();

        Assert.Equal(
            1,
            application.InitializeCount);

        Assert.True(
            clock.IsRunning);

        Assert.True(
            loop.IsInitialized);

        Assert.False(
            loop.IsShutdown);
    }

    [Fact]
    public void UpdateWithoutInitializeThrows()
    {
        var loop =
            CreateLoop(
                new TestApplication(),
                new TestClock());

        Assert.Throws<InvalidOperationException>(
            () =>
            {
                loop.Update(
                    Duration.FromMilliseconds(16));
            });
    }

    [Fact]
    public void UpdateCallsApplicationUpdate()
    {
        var application =
            new TestApplication();

        var clock =
            new TestClock();

        var loop =
            CreateLoop(
                application,
                clock);

        loop.Initialize();

        loop.Update(
            Duration.FromMilliseconds(16));

        Assert.Equal(
            1,
            application.UpdateCount);

        Assert.Single(
            application.Updates);

        Assert.Equal(
            16,
            application.Updates[0]
                .Delta
                .TotalMilliseconds);
    }

    [Fact]
    public void FrameDeltaIsAddedToElapsedTime()
    {
        var application =
            new TestApplication();

        var clock =
            new TestClock();

        var loop =
            CreateLoop(
                application,
                clock);

        loop.Initialize();

        loop.Update(
            Duration.FromMilliseconds(16));

        loop.Update(
            Duration.FromMilliseconds(20));

        Assert.Equal(
            36,
            loop.Elapsed.TotalMilliseconds);
    }

    [Fact]
    public void FixedUpdateRunsWhenAccumulatorReachesTickDuration()
    {
        var application =
            new TestApplication();

        var clock =
            new TestClock();

        var loop =
            CreateLoop(
                application,
                clock);

        loop.Initialize();

        loop.Update(
            Duration.FromMilliseconds(50));

        Assert.Equal(
            1,
            application.FixedUpdateCount);

        Assert.Equal(
            1UL,
            loop.Tick.Value);
    }

    [Fact]
    public void FixedUpdateUsesFixedDelta()
    {
        var application =
            new TestApplication();

        var clock =
            new TestClock();

        var loop =
            CreateLoop(
                application,
                clock);

        loop.Initialize();

        loop.Update(
            Duration.FromMilliseconds(50));

        Assert.Equal(
            Fixed32.FromRatio(1, 20),
            application.FixedUpdates[0].Delta);
    }

    [Fact]
    public void MultipleFixedUpdatesUseSequentialTicks()
    {
        var application =
            new TestApplication();

        var clock =
            new TestClock();

        var loop =
            CreateLoop(
                application,
                clock);

        loop.Initialize();

        loop.Update(
            Duration.FromMilliseconds(150));

        Assert.Equal(
            3,
            application.FixedUpdateCount);

        Assert.Equal(
            1UL,
            application.FixedUpdates[0]
                .Tick
                .Value);

        Assert.Equal(
            2UL,
            application.FixedUpdates[1]
                .Tick
                .Value);

        Assert.Equal(
            3UL,
            application.FixedUpdates[2]
                .Tick
                .Value);
    }

    [Fact]
    public void FrameDeltaIsClamped()
    {
        var application =
            new TestApplication();

        var clock =
            new TestClock();

        var loop =
            CreateLoop(
                application,
                clock,
                maxFrameDelta:
                Duration.FromMilliseconds(100));

        loop.Initialize();

        loop.Update(
            Duration.FromMilliseconds(500));

        Assert.Equal(
            100,
            application.Updates[0]
                .Delta
                .TotalMilliseconds);

        Assert.Equal(
            100,
            loop.Elapsed.TotalMilliseconds);
    }

    [Fact]
    public void FixedUpdatesAreLimitedPerFrame()
    {
        var application =
            new TestApplication();

        var clock =
            new TestClock();

        var loop =
            CreateLoop(
                application,
                clock,
                maxFixedStepsPerFrame: 3);

        loop.Initialize();

        loop.Update(
            Duration.FromMilliseconds(500));

        Assert.Equal(
            3,
            application.FixedUpdateCount);
    }

    [Fact]
    public void ExcessAccumulatedTimeIsDiscardedAfterFixedStepLimit()
    {
        var application =
            new TestApplication();

        var clock =
            new TestClock();

        var loop =
            CreateLoop(
                application,
                clock,
                maxFixedStepsPerFrame: 2);

        loop.Initialize();

        loop.Update(
            Duration.FromMilliseconds(500));

        Assert.Equal(
            0,
            loop.Accumulator.TotalMilliseconds);
    }

    [Fact]
    public void InterpolationAlphaRepresentsRemainingAccumulator()
    {
        var application =
            new TestApplication();

        var clock =
            new TestClock();

        var loop =
            CreateLoop(
                application,
                clock);

        loop.Initialize();

        loop.Update(
            Duration.FromMilliseconds(75));

        Assert.Equal(
            0.5,
            loop.InterpolationAlpha);
    }

    [Fact]
    public void NegativeDeltaThrows()
    {
        var loop =
            CreateLoop(
                new TestApplication(),
                new TestClock());

        loop.Initialize();

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
            {
                loop.Update(
                    new Duration(
                        TimeSpan.FromMilliseconds(-1)));
            });
    }

    [Fact]
    public void RenderRequiresInitialization()
    {
        var loop =
            CreateLoop(
                new TestApplication(),
                new TestClock());

        Assert.Throws<InvalidOperationException>(
            () =>
            {
                loop.Render();
            });
    }

    [Fact]
    public void RenderCallsApplicationRender()
    {
        var application =
            new TestApplication();

        var clock =
            new TestClock();

        var loop =
            CreateLoop(
                application,
                clock);

        loop.Initialize();

        loop.Render();

        Assert.Equal(
            1,
            application.RenderCount);
    }

    [Fact]
    public void ShutdownStopsClockAndShutsDownApplication()
    {
        var application =
            new TestApplication();

        var clock =
            new TestClock();

        var loop =
            CreateLoop(
                application,
                clock);

        loop.Initialize();
        loop.Shutdown();

        Assert.False(
            clock.IsRunning);

        Assert.Equal(
            1,
            application.ShutdownCount);

        Assert.True(
            loop.IsShutdown);
    }

    [Fact]
    public void UpdateAfterShutdownThrows()
    {
        var loop =
            CreateLoop(
                new TestApplication(),
                new TestClock());

        loop.Initialize();
        loop.Shutdown();

        Assert.Throws<InvalidOperationException>(
            () =>
            {
                loop.Update(
                    Duration.FromMilliseconds(16));
            });
    }

    [Fact]
    public void ShutdownBeforeInitializeThrows()
    {
        var loop =
            CreateLoop(
                new TestApplication(),
                new TestClock());

        Assert.Throws<InvalidOperationException>(
            () =>
            {
                loop.Shutdown();
            });
    }

    [Fact]
    public void InitializeTwiceThrows()
    {
        var loop =
            CreateLoop(
                new TestApplication(),
                new TestClock());

        loop.Initialize();

        Assert.Throws<InvalidOperationException>(
            () =>
            {
                loop.Initialize();
            });
    }

    [Fact]
    public void ShutdownTwiceThrows()
    {
        var loop =
            CreateLoop(
                new TestApplication(),
                new TestClock());

        loop.Initialize();
        loop.Shutdown();

        Assert.Throws<InvalidOperationException>(
            () =>
            {
                loop.Shutdown();
            });
    }

    private static GameLoop CreateLoop(
        TestApplication application,
        TestClock clock,
        Duration? maxFrameDelta = null,
        int maxFixedStepsPerFrame = 8)
    {
        return new GameLoop(
            application,
            new SimulationRate(20),
            clock,
            maxFrameDelta ??
            Duration.FromMilliseconds(250),
            maxFixedStepsPerFrame);
    }
}