using Engine.AI.Core;

namespace Engine.AI.DecisionMaking.Utility;

public interface IUtilityConsideration
{
    float Evaluate(in AIContext context);
}