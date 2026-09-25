namespace Engine.Core.Systems;

internal sealed class SystemExecutionPlan<TSystem>
    where TSystem : class
{
    private readonly IReadOnlyList<IReadOnlyList<TSystem>> _batches;

    private SystemExecutionPlan(
        IReadOnlyList<IReadOnlyList<TSystem>> batches)
    {
        _batches = batches;
    }

    public static SystemExecutionPlan<TSystem> Build(
        IReadOnlyList<SystemRegistration> registrations)
    {
        ArgumentNullException.ThrowIfNull(registrations);

        var nodes =
            registrations
                .Where(registration =>
                    registration.System is TSystem)
                .ToDictionary(
                    registration => registration.SystemType,
                    registration => new Node<TSystem>(
                        registration));

        BuildDependencies(
            nodes,
            registrations);

        var batches =
            BuildBatches(nodes);

        return new SystemExecutionPlan<TSystem>(
            batches);
    }

    public void Execute(
    ISystemExecutionStrategy strategy,
    Action<TSystem> execute)
    {
        ArgumentNullException.ThrowIfNull(
            strategy);

        ArgumentNullException.ThrowIfNull(
            execute);

        strategy.Execute(
            _batches,
            execute);
    }
    public void Execute(
        Action<TSystem> execute)
    {
        ArgumentNullException.ThrowIfNull(execute);

        foreach (var batch in _batches)
        {
            foreach (var system in batch)
            {
                execute(system);
            }
        }
    }

    private static void BuildDependencies(
        Dictionary<Type, Node<TSystem>> nodes,
        IReadOnlyList<SystemRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            if (!nodes.TryGetValue(
                    registration.SystemType,
                    out _))
            {
                continue;
            }

            foreach (var dependency in registration.AfterTypes)
            {
                AddEdge(
                    nodes,
                    dependency,
                    registration.SystemType);
            }

            foreach (var dependency in registration.BeforeTypes)
            {
                AddEdge(
                    nodes,
                    registration.SystemType,
                    dependency);
            }
        }
    }

    private static void AddEdge(
        Dictionary<Type, Node<TSystem>> nodes,
        Type from,
        Type to)
    {
        if (!nodes.ContainsKey(from))
        {
            throw new InvalidOperationException(
                $"System '{from.Name}' is not registered.");
        }

        if (!nodes.ContainsKey(to))
        {
            throw new InvalidOperationException(
                $"System '{to.Name}' is not registered.");
        }

        if (nodes[to].DependsOn.Add(from))
        {
            nodes[from].Dependents.Add(to);
        }
    }

    private static IReadOnlyList<IReadOnlyList<TSystem>> BuildBatches(
        Dictionary<Type, Node<TSystem>> nodes)
    {
        var remaining =
            new Dictionary<Type, Node<TSystem>>(
                nodes);

        var batches =
            new List<IReadOnlyList<TSystem>>();

        while (remaining.Count > 0)
        {
            var ready =
                remaining.Values
                    .Where(node =>
                        node.DependsOn.Count == 0)
                    .OrderBy(node =>
                        node.RegistrationOrder)
                    .ToList();

            if (ready.Count == 0)
            {
                throw new InvalidOperationException(
                    "System dependency graph contains a cycle.");
            }

            var batch =
                ready
                    .Select(node =>
                        node.System)
                    .ToList();

            batches.Add(batch);

            foreach (var node in ready)
            {
                remaining.Remove(
                    node.SystemType);
            }

            foreach (var node in ready)
            {
                foreach (var dependentType in node.Dependents)
                {
                    if (remaining.TryGetValue(
                            dependentType,
                            out var dependent))
                    {
                        dependent.DependsOn.Remove(
                            node.SystemType);
                    }
                }
            }
        }

        return batches;
    }

    private sealed class Node<T>
        where T : class
    {
        public Node(
            SystemRegistration registration)
        {
            SystemType =
                registration.SystemType;

            System =
                (T)registration.System;

            RegistrationOrder =
                registration.RegistrationOrder;
        }

        public Type SystemType { get; }

        public T System { get; }

        public int RegistrationOrder { get; }

        public HashSet<Type> DependsOn { get; } = new();

        public HashSet<Type> Dependents { get; } = new();
    }
}