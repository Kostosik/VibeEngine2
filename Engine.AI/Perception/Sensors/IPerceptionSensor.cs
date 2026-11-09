using Engine.AI.Perception.Core;

namespace Engine.AI.Perception.Sensors;

public interface IPerceptionSensor
{
    string Name { get; }

    void Sense(
        in PerceptionContext context,
        PerceptionBuffer results);
}