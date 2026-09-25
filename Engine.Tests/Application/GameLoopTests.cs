using Engine.Core.Application;
using Engine.Core.Math;
using Engine.Core.Time;

namespace Engine.Tests.Application;

public sealed class GameLoopTests
{
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