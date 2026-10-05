using System.Diagnostics;
using Engine.Core.Diagnostics.Metrics;

namespace Engine.Tooling.Profiling;

public sealed class Profiler :
    IProfiler
{
    private readonly MetricCollector _metrics = new();

    private readonly Stopwatch _frameTimer = new();
    private readonly Stopwatch _fpsTimer = Stopwatch.StartNew();

    private int _frameCount;
    private bool _hasPreviousFrame;

    public IReadOnlyCollection<Metric> Metrics =>
        _metrics.Metrics;

    public int FramesPerSecond { get; private set; }

    public TimeSpan LastFrameTime { get; private set; }

    public IDisposable BeginScope(
        string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new ProfileScope(
            _metrics,
            name);
    }

    public void RecordFrame()
    {
        if (!_hasPreviousFrame)
        {
            _hasPreviousFrame = true;
            _frameTimer.Restart();
            return;
        }

        var frameTime =
            _frameTimer.Elapsed;

        _frameTimer.Restart();

        LastFrameTime =
            frameTime;

        _metrics.RecordTime(
            "Frame",
            frameTime);

        _frameCount++;

        if (_fpsTimer.Elapsed >=
            TimeSpan.FromSeconds(1))
        {
            FramesPerSecond =
                (int)Math.Round(
                    _frameCount /
                    _fpsTimer.Elapsed.TotalSeconds);

            _metrics.Set(
                "Frame.FPS",
                FramesPerSecond);

            _frameCount = 0;
            _fpsTimer.Restart();
        }
    }
    public bool TryGetMetric(
string name,
out Metric metric)
    {
        return _metrics.TryGet(
            name,
            out metric);
    }
    public void Clear()
    {
        _metrics.Clear();

        _frameCount = 0;
        FramesPerSecond = 0;
        LastFrameTime = TimeSpan.Zero;

        _hasPreviousFrame = false;

        _frameTimer.Reset();
        _fpsTimer.Restart();
    }

    private sealed class ProfileScope :
        IDisposable
    {
        private readonly MetricCollector _metrics;
        private readonly string _name;
        private readonly PerformanceTimer _timer;

        private bool _disposed;

        public ProfileScope(
            MetricCollector metrics,
            string name)
        {
            _metrics = metrics;
            _name = name;
            _timer = PerformanceTimer.Start();

            _metrics.Increment(
                $"{_name}.Calls");
        }



        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _metrics.RecordTime(
                _name,
                _timer.Elapsed);
        }
    }
}