using Engine.Core.Time;

namespace Engine.AI.Scheduling;

public sealed class AISchedulerOptions
{
    private int _maxAgentsPerUpdate = 64;

    private Duration _criticalInterval =
        Duration.FromMilliseconds(50);

    private Duration _highInterval =
        Duration.FromMilliseconds(100);

    private Duration _normalInterval =
        Duration.FromMilliseconds(250);

    private Duration _lowInterval =
        Duration.FromMilliseconds(500);

    private Duration _backgroundInterval =
        Duration.FromMilliseconds(1000);

    private Duration _priorityAgingInterval =
        Duration.FromMilliseconds(100);

    public int MaxAgentsPerUpdate
    {
        get => _maxAgentsPerUpdate;

        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
                value);

            _maxAgentsPerUpdate = value;
        }
    }

    public Duration CriticalInterval
    {
        get => _criticalInterval;
        set => _criticalInterval = ValidateInterval(value);
    }

    public Duration HighInterval
    {
        get => _highInterval;
        set => _highInterval = ValidateInterval(value);
    }

    public Duration NormalInterval
    {
        get => _normalInterval;
        set => _normalInterval = ValidateInterval(value);
    }

    public Duration LowInterval
    {
        get => _lowInterval;
        set => _lowInterval = ValidateInterval(value);
    }

    public Duration BackgroundInterval
    {
        get => _backgroundInterval;
        set => _backgroundInterval = ValidateInterval(value);
    }

    public Duration PriorityAgingInterval
    {
        get => _priorityAgingInterval;

        set
        {
            ValidateInterval(value);

            if (value.IsZero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Priority aging interval must be positive.");
            }

            _priorityAgingInterval = value;
        }
    }

    public bool StaggerInitialUpdates { get; set; }

    public Duration GetInterval(AIUpdateGroup group)
    {
        return group switch
        {
            AIUpdateGroup.EveryUpdate =>
                Duration.Zero,

            AIUpdateGroup.Critical =>
                CriticalInterval,

            AIUpdateGroup.High =>
                HighInterval,

            AIUpdateGroup.Normal =>
                NormalInterval,

            AIUpdateGroup.Low =>
                LowInterval,

            AIUpdateGroup.Background =>
                BackgroundInterval,

            _ => throw new ArgumentOutOfRangeException(
                nameof(group))
        };
    }

    private static Duration ValidateInterval(
        Duration interval)
    {
        if (interval.IsNegative)
        {
            throw new ArgumentOutOfRangeException(
                nameof(interval));
        }

        return interval;
    }
}