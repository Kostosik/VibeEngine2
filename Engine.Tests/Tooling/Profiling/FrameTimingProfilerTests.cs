
using Engine.Tooling.Profiling;

namespace Engine.Tests.Tooling.Profiling;

public sealed class FrameTimingProfilerTests
{
    [Fact]
    public void Profiler_IsDisabledByDefault()
    {
        var profiler = new FrameTimingProfiler();

        var sample = CreateSample(1);

        Assert.False(profiler.IsCapturing);
        Assert.False(profiler.Record(sample));
        Assert.Equal(0, profiler.SampleCount);
    }

    [Fact]
    public void Constructor_RejectsInvalidCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new FrameTimingProfiler(0));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new FrameTimingProfiler(-1));
    }

    [Fact]
    public void StartCapture_EnablesRecording()
    {
        var profiler = new FrameTimingProfiler();

        profiler.StartCapture();

        Assert.True(profiler.IsCapturing);

        Assert.True(
            profiler.Record(CreateSample(1)));

        Assert.Equal(1, profiler.SampleCount);
    }

    [Fact]
    public void StopCapture_PreservesSamples()
    {
        var profiler = new FrameTimingProfiler();

        profiler.StartCapture();
        profiler.Record(CreateSample(1));

        profiler.StopCapture();

        Assert.False(profiler.IsCapturing);
        Assert.Equal(1, profiler.SampleCount);

        Assert.False(
            profiler.Record(CreateSample(2)));

        Assert.Equal(1, profiler.SampleCount);
    }

    [Fact]
    public void StartCapture_ClearsPreviousSession()
    {
        var profiler = new FrameTimingProfiler();

        profiler.StartCapture();
        profiler.Record(CreateSample(1));
        profiler.StopCapture();

        profiler.StartCapture();

        Assert.Equal(0, profiler.SampleCount);
    }

    [Fact]
    public void Record_RejectsNegativeTimings()
    {
        var profiler = new FrameTimingProfiler();

        profiler.StartCapture();

        var sample = new FrameTimingSample(
            TimeSpan.FromMilliseconds(1),
            TimeSpan.FromMilliseconds(-1),
            TimeSpan.Zero,
            TimeSpan.Zero,
            TimeSpan.Zero);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => profiler.Record(sample));

        Assert.Equal(0, profiler.SampleCount);
    }

    [Fact]
    public void Record_OverwritesOldestSamplesWhenCapacityIsReached()
    {
        var profiler = new FrameTimingProfiler(3);

        profiler.StartCapture();

        profiler.Record(CreateSample(1));
        profiler.Record(CreateSample(2));
        profiler.Record(CreateSample(3));
        profiler.Record(CreateSample(4));

        Assert.Equal(3, profiler.SampleCount);

        var samples = new FrameTimingSample[3];

        var copied = profiler.CopySamples(samples);

        Assert.Equal(3, copied);

        Assert.Equal(
            TimeSpan.FromMilliseconds(2),
            samples[0].FrameInterval);

        Assert.Equal(
            TimeSpan.FromMilliseconds(3),
            samples[1].FrameInterval);

        Assert.Equal(
            TimeSpan.FromMilliseconds(4),
            samples[2].FrameInterval);
    }

    [Fact]
    public void CopySamples_ReturnsNewestSamplesWhenDestinationIsSmaller()
    {
        var profiler = new FrameTimingProfiler(4);

        profiler.StartCapture();

        profiler.Record(CreateSample(1));
        profiler.Record(CreateSample(2));
        profiler.Record(CreateSample(3));
        profiler.Record(CreateSample(4));

        var samples = new FrameTimingSample[2];

        var copied = profiler.CopySamples(samples);

        Assert.Equal(2, copied);

        Assert.Equal(
            TimeSpan.FromMilliseconds(3),
            samples[0].FrameInterval);

        Assert.Equal(
            TimeSpan.FromMilliseconds(4),
            samples[1].FrameInterval);
    }

    [Fact]
    public void GetStatistics_CalculatesAverageMinMaxAndPercentiles()
    {
        var profiler = new FrameTimingProfiler(4);

        profiler.StartCapture();

        profiler.Record(CreateSample(1));
        profiler.Record(CreateSample(2));
        profiler.Record(CreateSample(3));
        profiler.Record(CreateSample(4));

        var statistics = profiler.GetStatistics();

        Assert.Equal(4, statistics.SampleCount);

        var frame = statistics.FrameInterval;

        Assert.Equal(
            TimeSpan.FromMilliseconds(2.5),
            frame.Average);

        Assert.Equal(
            TimeSpan.FromMilliseconds(1),
            frame.Minimum);

        Assert.Equal(
            TimeSpan.FromMilliseconds(4),
            frame.Maximum);

        Assert.Equal(
            TimeSpan.FromMilliseconds(4),
            frame.P95);

        Assert.Equal(
            TimeSpan.FromMilliseconds(4),
            frame.P99);
    }

    [Fact]
    public void GetStatistics_ReturnsEmptyStatisticsWithoutSamples()
    {
        var profiler = new FrameTimingProfiler();

        var statistics = profiler.GetStatistics();

        Assert.Equal(0, statistics.SampleCount);
        Assert.Equal(0, statistics.FrameInterval.SampleCount);
        Assert.Equal(0, statistics.Update.SampleCount);
    }

    [Fact]
    public void Clear_RemovesAllSamplesWithoutStoppingCapture()
    {
        var profiler = new FrameTimingProfiler();

        profiler.StartCapture();
        profiler.Record(CreateSample(1));

        profiler.Clear();

        Assert.True(profiler.IsCapturing);
        Assert.Equal(0, profiler.SampleCount);
    }

    [Fact]
    public void RecordsAllFrameTimingPhases()
    {
        var profiler = new FrameTimingProfiler();

        profiler.StartCapture();

        // Первый кадр используется для определения интервала.
        profiler.BeginUpdate();
        profiler.RecordUpdate(
            TimeSpan.FromMilliseconds(2));

        profiler.RecordFixedUpdate(
            TimeSpan.FromMilliseconds(3));

        profiler.BeginFrame();
        profiler.RecordRender(
            TimeSpan.FromMilliseconds(4));

        profiler.RecordEndFrame(
            TimeSpan.FromMilliseconds(1));

        profiler.EndFrame();

        Thread.Sleep(2);

        // Второй кадр уже имеет предыдущий timestamp.
        profiler.BeginUpdate();
        profiler.RecordUpdate(
            TimeSpan.FromMilliseconds(6));

        profiler.RecordFixedUpdate(
            TimeSpan.FromMilliseconds(2));

        profiler.RecordFixedUpdate(
            TimeSpan.FromMilliseconds(1));

        profiler.BeginFrame();
        profiler.RecordRender(
            TimeSpan.FromMilliseconds(4));

        profiler.RecordEndFrame(
            TimeSpan.FromMilliseconds(2));

        profiler.EndFrame();

        Assert.Equal(1, profiler.SampleCount);

        var samples = new FrameTimingSample[1];

        Assert.Equal(
            1,
            profiler.CopySamples(samples));

        var sample = samples[0];

        Assert.True(sample.FrameInterval > TimeSpan.Zero);

        Assert.Equal(
            TimeSpan.FromMilliseconds(6),
            sample.Update);

        Assert.Equal(
            TimeSpan.FromMilliseconds(3),
            sample.FixedUpdate);

        Assert.Equal(
            TimeSpan.FromMilliseconds(4),
            sample.Render);

        Assert.Equal(
            TimeSpan.FromMilliseconds(2),
            sample.EndFrame);
    }

    [Fact]
    public void StartAndStopCapture_AreIdempotent()
    {
        var profiler = new FrameTimingProfiler();

        profiler.StartCapture();
        profiler.StartCapture();

        Assert.True(profiler.IsCapturing);

        profiler.StopCapture();
        profiler.StopCapture();

        Assert.False(profiler.IsCapturing);
    }

    private static FrameTimingSample CreateSample(double milliseconds)
    {
        var duration = TimeSpan.FromMilliseconds(milliseconds);

        return new FrameTimingSample(
            duration,
            duration,
            duration,
            duration,
            duration);
    }
}
