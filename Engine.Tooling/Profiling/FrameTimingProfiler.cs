
using Engine.Core.Application;
using System.Diagnostics;

namespace Engine.Tooling.Profiling;

public sealed class FrameTimingProfiler : IFrameTimingRecorder
{
    private long _previousFrameTimestamp;

    private bool _hasPreviousFrameTimestamp;
    private bool _hasFrameInterval;
    private bool _updateOpen;
    private bool _hasUpdate;
    private bool _frameOpen;

    private TimeSpan _frameInterval;
    private TimeSpan _update;
    private TimeSpan _fixedUpdate;
    private TimeSpan _render;
    private TimeSpan _endFrame;

    private readonly FrameTimingSample[] _samples;

    private int _nextWriteIndex;
    private int _sampleCount;
    private bool _isCapturing;

    public FrameTimingProfiler(int capacity = 1024)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            capacity);

        _samples = new FrameTimingSample[capacity];
    }

    public bool IsCapturing => _isCapturing;

    public int Capacity => _samples.Length;

    public int SampleCount => _sampleCount;

    public void StartCapture()
    {
        if (_isCapturing)
        {
            return;
        }

        Clear();

        _isCapturing = true;
    }

    public void StopCapture()
    {
        _isCapturing = false;

        _updateOpen = false;
        _hasUpdate = false;
        _frameOpen = false;
        _hasPreviousFrameTimestamp = false;
    }

    public void Clear()
    {
        Array.Clear(_samples);

        _nextWriteIndex = 0;
        _sampleCount = 0;

        _hasPreviousFrameTimestamp = false;
        _hasFrameInterval = false;

        _updateOpen = false;
        _hasUpdate = false;
        _frameOpen = false;

        _frameInterval = TimeSpan.Zero;
        _update = TimeSpan.Zero;
        _fixedUpdate = TimeSpan.Zero;
        _render = TimeSpan.Zero;
        _endFrame = TimeSpan.Zero;
    }

    public bool Record(in FrameTimingSample sample)
    {
        if (!_isCapturing)
        {
            return false;
        }

        sample.Validate();

        _samples[_nextWriteIndex] = sample;

        _nextWriteIndex++;

        if (_nextWriteIndex >= Capacity)
        {
            _nextWriteIndex = 0;
        }

        if (_sampleCount < Capacity)
        {
            _sampleCount++;
        }

        return true;
    }

    public void BeginUpdate()
    {
        if (!_isCapturing)
        {
            return;
        }

        _update = TimeSpan.Zero;
        _fixedUpdate = TimeSpan.Zero;

        _updateOpen = true;
        _hasUpdate = false;
    }

    public void RecordUpdate(TimeSpan elapsed)
    {
        if (!_isCapturing || !_updateOpen)
        {
            return;
        }

        ValidateDuration(elapsed);

        _update = elapsed;
        _hasUpdate = true;
    }

    public void RecordFixedUpdate(TimeSpan elapsed)
    {
        if (!_isCapturing || !_updateOpen)
        {
            return;
        }

        ValidateDuration(elapsed);

        _fixedUpdate += elapsed;
    }

    public void BeginFrame()
    {
        if (!_isCapturing)
        {
            return;
        }

        var timestamp = Stopwatch.GetTimestamp();

        _hasFrameInterval = _hasPreviousFrameTimestamp;

        _frameInterval = _hasFrameInterval
            ? Stopwatch.GetElapsedTime(
                _previousFrameTimestamp,
                timestamp)
            : TimeSpan.Zero;

        _previousFrameTimestamp = timestamp;
        _hasPreviousFrameTimestamp = true;

        _render = TimeSpan.Zero;
        _endFrame = TimeSpan.Zero;

        _frameOpen = true;
    }

    public void RecordRender(TimeSpan elapsed)
    {
        if (!_isCapturing || !_frameOpen)
        {
            return;
        }

        ValidateDuration(elapsed);

        _render = elapsed;
    }

    public void RecordEndFrame(TimeSpan elapsed)
    {
        if (!_isCapturing || !_frameOpen)
        {
            return;
        }

        ValidateDuration(elapsed);

        _endFrame = elapsed;
    }

    public void EndFrame()
    {
        if (!_isCapturing || !_frameOpen)
        {
            return;
        }

        _frameOpen = false;

        if (_hasFrameInterval && _hasUpdate)
        {
            Record(
                new FrameTimingSample(
                    _frameInterval,
                    _update,
                    _fixedUpdate,
                    _render,
                    _endFrame));
        }

        _updateOpen = false;
        _hasUpdate = false;
    }

    private static void ValidateDuration(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(elapsed),
                "Timing duration cannot be negative.");
        }
    }

    /// <summary>
    /// Copies samples in chronological order.
    /// If the destination is smaller than the buffer,
    /// the newest samples are copied.
    /// </summary>
    public int CopySamples(
        Span<FrameTimingSample> destination)
    {
        var count = Math.Min(
            _sampleCount,
            destination.Length);

        if (count == 0)
        {
            return 0;
        }

        var startIndex =
            (_nextWriteIndex - count + Capacity) % Capacity;

        for (var i = 0; i < count; i++)
        {
            var index = (startIndex + i) % Capacity;

            destination[i] = _samples[index];
        }

        return count;
    }

    public FrameTimingStatistics GetStatistics()
    {
        if (_sampleCount == 0)
        {
            return default;
        }

        return new FrameTimingStatistics(
            _sampleCount,
            Calculate(static x => x.FrameInterval),
            Calculate(static x => x.Update),
            Calculate(static x => x.FixedUpdate),
            Calculate(static x => x.Render),
            Calculate(static x => x.EndFrame));
    }

    private TimingStatistics Calculate(
        Func<FrameTimingSample, TimeSpan> selector)
    {
        var values = new long[_sampleCount];

        var samples = new FrameTimingSample[_sampleCount];

        CopySamples(samples);

        decimal totalTicks = 0;

        for (var i = 0; i < samples.Length; i++)
        {
            var ticks = selector(samples[i]).Ticks;

            values[i] = ticks;
            totalTicks += ticks;
        }

        Array.Sort(values);

        var p95Index = GetPercentileIndex(
            values.Length,
            95);

        var p99Index = GetPercentileIndex(
            values.Length,
            99);

        return new TimingStatistics(
            values.Length,
            TimeSpan.FromTicks(
                (long)(totalTicks / values.Length)),
            TimeSpan.FromTicks(values[0]),
            TimeSpan.FromTicks(values[^1]),
            TimeSpan.FromTicks(values[p95Index]),
            TimeSpan.FromTicks(values[p99Index]));
    }

    private static int GetPercentileIndex(
        int count,
        int percentile)
    {
        // Nearest-rank percentile.
        var rank = (int)Math.Ceiling(
            count * percentile / 100.0);

        return Math.Clamp(
            rank - 1,
            0,
            count - 1);
    }
}
