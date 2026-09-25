using Engine.Core.Commands;
using Engine.Core.Time;

namespace Engine.Core.Replays;

public sealed class Replay
{
    private readonly List<ReplayFrame> _frames = new();

    public IReadOnlyList<ReplayFrame> Frames =>
        _frames;

    internal ReplayFrame GetOrCreateFrame(
        Tick tick)
    {
        if (_frames.Count > 0)
        {
            var last =
                _frames[^1];

            if (last.Tick == tick)
                return last;
        }

        var frame =
            new ReplayFrame(tick);

        _frames.Add(frame);

        return frame;
    }
}