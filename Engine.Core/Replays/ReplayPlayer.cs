using Engine.Core.Commands;
using Engine.Core.Time;

namespace Engine.Core.Replays;

public sealed class ReplayPlayer
{
    private readonly Replay _replay;

    public ReplayPlayer(
        Replay replay)
    {
        ArgumentNullException.ThrowIfNull(replay);

        _replay = replay;
    }

    public void EnqueueCommands(
        Tick tick,
        CommandQueue queue)
    {
        ArgumentNullException.ThrowIfNull(queue);

        foreach (var frame in _replay.Frames)
        {
            if (frame.Tick != tick)
            {
                continue;
            }

            foreach (var command in frame.Commands)
            {
                queue.Enqueue(
                    command);
            }

            return;
        }
    }
}