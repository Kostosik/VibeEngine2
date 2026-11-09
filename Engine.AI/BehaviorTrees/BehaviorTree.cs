using Engine.AI.Behaviors;
using Engine.AI.BehaviorTrees.Core;
using Engine.AI.Core;

namespace Engine.AI.BehaviorTrees;

public sealed class BehaviorTree :
    IAIBehavior,
    IAIBehaviorLifecycle
{
    private sealed class AgentRuntimeState
    {
        public readonly BehaviorTreeRuntimeState Nodes = new();

        public AIContext LastContext;

        public bool HasContext;

        public bool IsCompleted;

        public BehaviorStatus? LastStatus;
    }

    private readonly IBehaviorTreeNode _root;

    public string Name { get; }

    public IBehaviorTreeNode Root => _root;

    public bool RestartOnCompletion { get; set; } = true;

    public BehaviorTree(
        string name,
        IBehaviorTreeNode root,
        bool restartOnCompletion = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(root);

        Name = name;
        _root = root;

        RestartOnCompletion = restartOnCompletion;

        ValidateTree(root);
    }

    public void Update(in AIContext context)
    {
        var state = context.AgentState.GetOrCreate(
            this,
            static () => new AgentRuntimeState());

        state.LastContext = context;
        state.HasContext = true;

        if (state.IsCompleted && !RestartOnCompletion)
            return;

        var treeContext = new BehaviorTreeContext(
            context,
            state.Nodes);

        var result = _root.Tick(in treeContext);

        if (!Enum.IsDefined(result))
        {
            throw new InvalidOperationException(
                $"Behavior tree '{Name}' returned an invalid status.");
        }

        state.LastStatus = result;

        if (result == BehaviorStatus.Running)
            return;

        _root.Reset(in treeContext);

        state.IsCompleted = !RestartOnCompletion;
    }

    public BehaviorStatus? GetLastStatus(
        AIAgentState agentState)
    {
        ArgumentNullException.ThrowIfNull(agentState);

        if (!agentState.TryGet(
                this,
                out AgentRuntimeState? state))
        {
            return null;
        }

        return state.LastStatus;
    }

    public void Abort(AIAgentState agentState)
    {
        ArgumentNullException.ThrowIfNull(agentState);

        if (!agentState.TryGet(
                this,
                out AgentRuntimeState? state))
        {
            return;
        }

        try
        {
            if (state.HasContext)
            {
                var treeContext = new BehaviorTreeContext(
                    state.LastContext,
                    state.Nodes);

                _root.Reset(in treeContext);
            }
        }
        finally
        {
            state.Nodes.Clear();
            agentState.Remove(this);
        }
    }

    public void OnDetached(AIAgentState agentState)
    {
        Abort(agentState);
    }

    private static void ValidateTree(
        IBehaviorTreeNode root)
    {
        var visited = new HashSet<IBehaviorTreeNode>(
            ReferenceEqualityComparer.Instance);

        var active = new HashSet<IBehaviorTreeNode>(
            ReferenceEqualityComparer.Instance);

        Visit(root);

        void Visit(IBehaviorTreeNode node)
        {
            if (active.Contains(node))
            {
                throw new InvalidOperationException(
                    $"Cycle detected in behavior tree at '{node.Name}'.");
            }

            if (!visited.Add(node))
            {
                throw new InvalidOperationException(
                    $"Node '{node.Name}' is used more than once in the tree.");
            }

            active.Add(node);

            if (node is IBehaviorTreeParentNode parent)
            {
                foreach (var child in parent.Children)
                {
                    Visit(child);
                }
            }

            active.Remove(node);
        }
    }
}