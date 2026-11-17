
using Engine.Core.Diagnostics.Metrics;
using System.Diagnostics;

namespace Engine.Tooling.Profiling;

public sealed class Profiler : IProfiler
{
    private readonly MetricCollector _metrics = new();

    private readonly Stopwatch _frameTimer = new();
    private readonly Stopwatch _fpsTimer = new();

    private int _frameCount;
    private bool _hasPreviousFrame;
    private bool _isCapturing;

    public bool IsCapturing => _isCapturing;

    public IReadOnlyCollection<Metric> Metrics =>
        _metrics.Metrics;

    public int FramesPerSecond { get; private set; }

    public TimeSpan LastFrameTime { get; private set; }

    public void StartCapture()
    {
        if (_isCapturing)
        {
            return;
        }

        Clear();

        _isCapturing = true;

        _frameTimer.Restart();
        _fpsTimer.Restart();
    }

    public void StopCapture()
    {
        if (!_isCapturing)
        {
            return;
        }

        _isCapturing = false;

        _frameTimer.Stop();
        _fpsTimer.Stop();
    }

    public IDisposable BeginScope(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!_isCapturing)
        {
            return EmptyScope.Instance;
        }

        _metrics.Increment($"{name}.Calls");

        return new ProfileScope(this, name);
    }

    public void RecordFrame()
    {
        if (!_isCapturing)
        {
            return;
        }

        if (!_hasPreviousFrame)
        {
            _hasPreviousFrame = true;
            _frameTimer.Restart();
            return;
        }

        var frameTime = _frameTimer.Elapsed;

        _frameTimer.Restart();

        LastFrameTime = frameTime;

        _metrics.RecordTime("Frame", frameTime);

        _frameCount++;

        if (_fpsTimer.Elapsed >= TimeSpan.FromSeconds(1))
        {
            FramesPerSecond = (int)Math.Round(
                _frameCount / _fpsTimer.Elapsed.TotalSeconds);

            _metrics.Set("Frame.FPS", FramesPerSecond);

            _frameCount = 0;
            _fpsTimer.Restart();
        }
    }

    public bool TryGetMetric(
        string name,
        out Metric metric)
    {
        return _metrics.TryGet(name, out metric);
    }

    public void Clear()
    {
        _metrics.Clear();

        _frameCount = 0;
        FramesPerSecond = 0;
        LastFrameTime = TimeSpan.Zero;

        _hasPreviousFrame = false;

        _frameTimer.Reset();
        _fpsTimer.Reset();

        if (_isCapturing)
        {
            _frameTimer.Start();
            _fpsTimer.Start();
        }
    }

    private void RecordScope(
        string name,
        TimeSpan elapsed)
    {
        if (!_isCapturing)
        {
            return;
        }

        _metrics.RecordTime(name, elapsed);
    }

    private sealed class ProfileScope : IDisposable
    {
        private readonly Profiler _profiler;
        private readonly string _name;
        private readonly PerformanceTimer _timer;

        private bool _disposed;

        public ProfileScope(
            Profiler profiler,
            string name)
        {
            _profiler = profiler;
            _name = name;
            _timer = PerformanceTimer.Start();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _profiler.RecordScope(
                _name,
                _timer.Elapsed);
        }
    }

    private sealed class EmptyScope : IDisposable
    {
        public static readonly EmptyScope Instance = new();

        private EmptyScope()
        {
        }

        public void Dispose()
        {
        }
    }
}
