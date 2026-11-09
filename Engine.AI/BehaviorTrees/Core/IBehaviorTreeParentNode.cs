namespace Engine.AI.BehaviorTrees.Core;

public interface IBehaviorTreeParentNode : IBehaviorTreeNode
{
    IReadOnlyList<IBehaviorTreeNode> Children { get; }
}