using Engine.Core.Commands;
using Engine.Core.Determinism;
using Engine.Core.Systems;
using Engine.Core.Time;

namespace Engine.Simulations.Lockstep;

public sealed class LockstepCoordinator
{
    private readonly Simulation _simulation;

    private readonly int _participantCount;

    public Tick LastExecutedTick =>
    _lastExecutedTick;

    private Tick _lastExecutedTick =
    Tick.Zero;

    private readonly Dictionary<
        Tick,
        TickCommands> _pending = new();

    public LockstepCoordinator(
        Simulation simulation,
        int participantCount)
    {
        ArgumentNullException.ThrowIfNull(
            simulation);

        if (participantCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(participantCount),
                "Participant count must be greater than zero.");
        }

        _simulation = simulation;
        _participantCount = participantCount;
    }

    public int ParticipantCount =>
        _participantCount;

    public DeterministicStateHash GetStateHash()
    {
        return _simulation.GetStateHash();
    }

    public void Submit(
        Tick tick,
        int participant,
        IReadOnlyList<ICommand> commands)
    {
        ArgumentNullException.ThrowIfNull(
            commands);

        if (tick.Value <= _lastExecutedTick.Value)
        {
            throw new InvalidOperationException(
                $"Tick '{tick}' has already been executed.");
        }

        ValidateParticipant(
            participant);

        if (!_pending.TryGetValue(
                tick,
                out var tickCommands))
        {
            tickCommands =
                new TickCommands(
                    _participantCount);

            _pending.Add(
                tick,
                tickCommands);
        }

        if (tickCommands.HasParticipant(
                participant))
        {
            throw new InvalidOperationException(
                $"Commands for participant '{participant}' " +
                $"and tick '{tick}' have already been submitted.");
        }

        tickCommands.Set(
            participant,
            commands);
    }

    public bool IsReady(
        Tick tick)
    {
        return _pending.TryGetValue(
                   tick,
                   out var tickCommands) &&
               tickCommands.IsComplete;
    }

    public bool TryExecute(
        Tick tick,
        FixedSystemContext context)
    {
        if (context.Time.Tick != tick)
        {
            throw new ArgumentException(
                "Fixed system context tick does not match " +
                "the requested lockstep tick.",
                nameof(context));
        }

        var expectedTick =
            _lastExecutedTick.Value + 1;

        if (tick.Value != expectedTick)
        {
            throw new InvalidOperationException(
                $"Expected tick '{expectedTick}', " +
                $"but received tick '{tick.Value}'.");
        }

        if (!_pending.TryGetValue(
                tick,
                out var tickCommands) ||
            !tickCommands.IsComplete)
        {
            return false;
        }

        for (var participant = 0;
             participant < _participantCount;
             participant++)
        {
            foreach (var command in
                     tickCommands.Get(participant))
            {
                _simulation.Commands.Enqueue(
                    command);
            }
        }

        _simulation.FixedUpdate(
            context);

        _pending.Remove(
            tick);

        _lastExecutedTick =
            tick;

        return true;
    }

    public void Clear()
    {
        _pending.Clear();
    }

    private void ValidateParticipant(
        int participant)
    {
        if (participant < 0 ||
            participant >= _participantCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(participant),
                "Participant index is outside the valid range.");
        }
    }

    private sealed class TickCommands
    {
        private readonly IReadOnlyList<ICommand>?[] _commands;

        public TickCommands(
            int participantCount)
        {
            _commands =
                new IReadOnlyList<ICommand>?[
                    participantCount];
        }

        public bool IsComplete
        {
            get
            {
                for (var i = 0;
                     i < _commands.Length;
                     i++)
                {
                    if (_commands[i] is null)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool HasParticipant(
            int participant)
        {
            return _commands[participant] is not null;
        }

        public void Set(
            int participant,
            IReadOnlyList<ICommand> commands)
        {
            _commands[participant] =
                commands;
        }

        public IReadOnlyList<ICommand> Get(
            int participant)
        {
            return _commands[participant]!;
        }
    }
}