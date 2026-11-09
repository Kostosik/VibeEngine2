namespace Engine.AI.BehaviorTrees.Core;

public interface IBehaviorTreeNode
{
    string Name { get; }

    BehaviorStatus Tick(
        in BehaviorTreeContext context);

    void Reset(
        in BehaviorTreeContext context);
}