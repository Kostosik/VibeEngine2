using Engine.Core.Time;

namespace Engine.Core.Replays;

public sealed class Replay
{
    private readonly List<ReplayFrame> _frames = new();

    private readonly Dictionary<
        Tick,
        ReplayFrame> _framesByTick =
        new();

    public IReadOnlyList<ReplayFrame> Frames =>
        _frames;

    public ReplayFrame GetOrCreateFrame(
        Tick tick)
    {
        if (_framesByTick.TryGetValue(
                tick,
                out var existing))
        {
            return existing;
        }

        var frame =
            new ReplayFrame(
                tick);

        _frames.Add(
            frame);

        _framesByTick.Add(
            tick,
            frame);

        return frame;
    }
}