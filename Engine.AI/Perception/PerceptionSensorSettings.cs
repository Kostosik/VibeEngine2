
using Engine.Core.Time;

namespace Engine.AI.Perception;

public readonly struct PerceptionSensorSettings
{
    /// <summary>
    /// Zero means update on every PerceptionSystem update.
    /// </summary>
    public Duration UpdateInterval { get; }

    /// <summary>
    /// Higher values give the sensor higher scheduling priority.
    /// </summary>
    public int Priority { get; }

    public PerceptionSensorSettings(
        Duration updateInterval = default,
        int priority = 0)
    {
        if (updateInterval.IsNegative)
        {
            throw new ArgumentOutOfRangeException(
                nameof(updateInterval));
        }

        UpdateInterval = updateInterval;
        Priority = priority;
    }
}
