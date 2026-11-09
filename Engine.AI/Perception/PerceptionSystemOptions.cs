
using Engine.Core.Time;

namespace Engine.AI.Perception;

public sealed class PerceptionSystemOptions
{
    private int _maxSensorUpdatesPerUpdate = 128;

    private Duration _priorityAgingInterval =
        Duration.FromMilliseconds(100);

    /// <summary>
    /// Maximum number of sensor executions per system update.
    /// </summary>
    public int MaxSensorUpdatesPerUpdate
    {
        get => _maxSensorUpdatesPerUpdate;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
                value);

            _maxSensorUpdatesPerUpdate = value;
        }
    }

    /// <summary>
    /// How often an overdue sensor receives an additional
    /// scheduling priority point.
    /// </summary>
    public Duration PriorityAgingInterval
    {
        get => _priorityAgingInterval;
        set
        {
            if (value.IsNegative || value.IsZero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Priority aging interval must be positive.");
            }

            _priorityAgingInterval = value;
        }
    }
}
