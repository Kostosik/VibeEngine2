using Engine.AI.Core;

namespace Engine.AI.Behaviors;

public interface IAIBehavior
{
    void Update(in AIContext context);
}