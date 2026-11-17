namespace Engine.Graphics.Rendering.RenderGraph;

/// <summary>
/// Validates a declarative render graph and compiles it into an immutable execution plan.
/// The compiler does not create GPU resources or execute passes.
/// </summary>
public sealed class RenderGraphCompiler
{
    public RenderGraphExecutionPlan Compile(RenderGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var resources = graph.ResourceDefinitions;
        var passes = graph.PassDefinitions;
        var passCount = passes.Count;

        ValidateGraphHandles(graph);

        var dependencies = CreateDependencySets(passCount);
        var writers = FindWriters(resources, passes);

        ValidateStoreActions(
            resources,
            passes,
            graph.Outputs,
            writers);

        BuildTransientResourceDependencies(
            resources,
            passes,
            graph.Outputs,
            writers,
            dependencies);

        BuildImportedResourceDependencies(
            resources,
            passes,
            dependencies);

        BuildExplicitDependencies(
            passes,
            dependencies);
        // Сначала проверяем зависимости всего объявленного графа.
        var topologicalOrder = TopologicalSort(passes, dependencies);

        // Определяем проходы, которые действительно влияют на выходы.
        var livePasses = FindLivePasses(
            passes,
            graph.Outputs,
            resources,
            writers,
            dependencies);

        // Добавляем порядок записи только между живыми writers.
        // Благодаря этому ранее перезаписанные ресурсы не мешают culling.
        BuildImportedWriterOrderingDependencies(
            resources,
            passes,
            livePasses,
            dependencies);
        BuildImportedReaderOrderingDependencies(
            resources,
            passes,
            livePasses,
            dependencies);
        // Повторно строим порядок с учётом новых зависимостей.
        topologicalOrder = TopologicalSort(passes, dependencies);

        var compiledPasses = BuildCompiledPasses(
                    passes,
            dependencies,
            topologicalOrder,
            livePasses);

        var compiledResources = BuildCompiledResources(
            resources,
            graph.Outputs,
            compiledPasses);

        return new RenderGraphExecutionPlan(
            graph.GraphId,
            compiledPasses,
            compiledResources,
            graph.Outputs,
            passCount - livePasses.Count);
    }

    private static void BuildImportedWriterOrderingDependencies(
    IReadOnlyList<RenderGraphResourceDefinition> resources,
    IReadOnlyList<RenderGraphPassDefinition> passes,
    IReadOnlySet<int> livePasses,
    IReadOnlyList<HashSet<int>> dependencies)
    {
        foreach (var resource in resources)
        {
            if (resource.Kind == RenderGraphResourceKind.Transient)
            {
                continue;
            }

            int? previousLiveWriter = null;

            foreach (var pass in passes)
            {
                var passIndex = pass.Handle.Index;

                if (!livePasses.Contains(passIndex))
                {
                    continue;
                }

                var writesResource = pass.ResourceAccesses.Any(access =>
                    access.Resource == resource.Handle &&
                    access.Kind == RenderGraphResourceAccessKind.WriteColor);

                if (!writesResource)
                {
                    continue;
                }

                if (previousLiveWriter.HasValue)
                {
                    AddDependency(
                        dependencies,
                        previousLiveWriter.Value,
                        passIndex);
                }

                previousLiveWriter = passIndex;
            }
        }
    }

    private static void ValidateGraphHandles(RenderGraph graph)
    {
        foreach (var resource in graph.ResourceDefinitions)
        {
            if (!resource.Handle.IsValid ||
                resource.Handle.GraphId != graph.GraphId ||
                resource.Handle.Index >= graph.ResourceCount)
            {
                throw new InvalidOperationException(
                    $"Render graph contains an invalid resource handle for '{resource.Name}'.");
            }

            resource.Descriptor.Validate();
        }

        foreach (var pass in graph.PassDefinitions)
        {
            if (!pass.Handle.IsValid ||
                pass.Handle.GraphId != graph.GraphId ||
                pass.Handle.Index >= graph.PassCount)
            {
                throw new InvalidOperationException(
                    $"Render graph contains an invalid pass handle for '{pass.Name}'.");
            }

            foreach (var access in pass.ResourceAccesses)
            {
                ValidateResourceHandle(graph, access.Resource, pass.Name);
                if (!Enum.IsDefined(access.StoreAction))
                {
                    throw new InvalidOperationException(
                        $"Pass '{pass.Name}' contains an unsupported store action.");
                }
                if (!Enum.IsDefined(access.Kind))
                {
                    throw new InvalidOperationException(
                        $"Pass '{pass.Name}' contains an unsupported resource access kind.");
                }

                if (!Enum.IsDefined(access.LoadAction))
                {
                    throw new InvalidOperationException(
                        $"Pass '{pass.Name}' contains an unsupported load action.");
                }

                if (access.Kind == RenderGraphResourceAccessKind.Read &&
                    access.LoadAction != RenderGraphLoadAction.DontCare)
                {
                    throw new InvalidOperationException(
                        $"Pass '{pass.Name}' has a load action on a read-only resource access.");
                }
            }
        }

        foreach (var output in graph.Outputs)
        {
            ValidateResourceHandle(graph, output, "graph output");
        }
    }

    private static void ValidateResourceHandle(
        RenderGraph graph,
        RenderGraphResourceHandle handle,
        string ownerName)
    {
        if (!handle.IsValid ||
            handle.GraphId != graph.GraphId ||
            handle.Index >= graph.ResourceCount)
        {
            throw new InvalidOperationException(
                $"'{ownerName}' references a resource outside this render graph.");
        }
    }

    private static List<HashSet<int>> CreateDependencySets(int passCount)
    {
        var dependencies = new List<HashSet<int>>(passCount);

        for (var i = 0; i < passCount; i++)
        {
            dependencies.Add(new HashSet<int>());
        }

        return dependencies;
    }

    private static List<int>[] FindWriters(
        IReadOnlyList<RenderGraphResourceDefinition> resources,
        IReadOnlyList<RenderGraphPassDefinition> passes)
    {
        var writers = new List<int>[resources.Count];

        for (var resourceIndex = 0; resourceIndex < resources.Count; resourceIndex++)
        {
            writers[resourceIndex] = new List<int>();
        }

        foreach (var pass in passes)
        {
            foreach (var access in pass.ResourceAccesses)
            {
                if (access.Kind == RenderGraphResourceAccessKind.WriteColor)
                {
                    writers[access.Resource.Index].Add(pass.Handle.Index);
                }
            }
        }

        for (var resourceIndex = 0; resourceIndex < resources.Count; resourceIndex++)
        {
            var resource = resources[resourceIndex];
            var resourceWriters = writers[resourceIndex];

            if (resource.Kind == RenderGraphResourceKind.Transient &&
                resourceWriters.Count > 1)
            {
                var names = string.Join(
                    "', '",
                    resourceWriters.Select(index => passes[index].Name));

                throw new InvalidOperationException(
                    $"Transient resource '{resource.Name}' has multiple writers: '{names}'. " +
                    "A transient resource must have exactly one producer.");
            }
        }

        return writers;
    }

    private static void BuildTransientResourceDependencies(
        IReadOnlyList<RenderGraphResourceDefinition> resources,
        IReadOnlyList<RenderGraphPassDefinition> passes,
        IReadOnlyList<RenderGraphResourceHandle> outputs,
        IReadOnlyList<int>[] writers,
        IReadOnlyList<HashSet<int>> dependencies)
    {
        var outputSet = outputs.ToHashSet();

        foreach (var resource in resources)
        {
            if (resource.Kind != RenderGraphResourceKind.Transient)
            {
                continue;
            }

            var resourceWriters = writers[resource.Handle.Index];
            var hasWriter = resourceWriters.Count == 1;
            var writerIndex = hasWriter ? resourceWriters[0] : -1;

            var readers = passes
                .Where(pass => pass.ResourceAccesses.Any(
                    access => access.Resource == resource.Handle &&
                              access.Kind == RenderGraphResourceAccessKind.Read))
                .Select(pass => pass.Handle.Index)
                .ToArray();

            if (!hasWriter &&
                (readers.Length > 0 || outputSet.Contains(resource.Handle)))
            {
                throw new InvalidOperationException(
                    $"Transient resource '{resource.Name}' is read or exported but has no producer.");
            }

            if (!hasWriter)
            {
                continue;
            }

            foreach (var readerIndex in readers)
            {
                AddDependency(
                    dependencies,
                    writerIndex,
                    readerIndex);
            }
        }
    }

    private static void BuildImportedResourceDependencies(
        IReadOnlyList<RenderGraphResourceDefinition> resources,
        IReadOnlyList<RenderGraphPassDefinition> passes,
        IReadOnlyList<HashSet<int>> dependencies)
    {
        foreach (var resource in resources)
        {
            if (resource.Kind == RenderGraphResourceKind.Transient)
            {
                continue;
            }

            int? latestWriter = null;

            // Imported resources may be written more than once. Declaration
            // order defines resource versions. Reads before the first graph
            // write consume the externally supplied initial contents.
            foreach (var pass in passes)
            {
                var access = pass.ResourceAccesses.FirstOrDefault(
                    item => item.Resource == resource.Handle);

                if (access.Resource != resource.Handle)
                {
                    continue;
                }

                if (access.Kind == RenderGraphResourceAccessKind.Read)
                {
                    if (latestWriter.HasValue)
                    {
                        AddDependency(
                            dependencies,
                            latestWriter.Value,
                            pass.Handle.Index);
                    }
                    continue;
                }

                if (access.LoadAction == RenderGraphLoadAction.Load &&
                    latestWriter.HasValue)
                {
                    AddDependency(
                        dependencies,
                        latestWriter.Value,
                        pass.Handle.Index);
                }

                // Clear and DontCare writes replace previous contents. They do
                // not need the previous writer unless a live reader consumed
                // that writer's version.
                latestWriter = pass.Handle.Index;
            }
        }
    }

    private static void ValidateStoreActions(
    IReadOnlyList<RenderGraphResourceDefinition> resources,
    IReadOnlyList<RenderGraphPassDefinition> passes,
    IReadOnlyList<RenderGraphResourceHandle> outputs,
    IReadOnlyList<int>[] writers)
    {
        var outputSet = outputs.ToHashSet();

        foreach (var resource in resources)
        {
            // A transient resource has exactly one writer.
            if (resource.Kind == RenderGraphResourceKind.Transient)
            {
                var resourceWriters = writers[resource.Handle.Index];

                if (resourceWriters.Count == 0)
                {
                    continue;
                }

                var writer = passes[resourceWriters[0]];

                var writeAccess = writer.ResourceAccesses.Single(
                    access =>
                        access.Resource == resource.Handle &&
                        access.Kind == RenderGraphResourceAccessKind.WriteColor);

                if (writeAccess.StoreAction != RenderGraphStoreAction.DontCare)
                {
                    continue;
                }

                var hasReaders = passes.Any(pass =>
                    pass.ResourceAccesses.Any(access =>
                        access.Resource == resource.Handle &&
                        access.Kind == RenderGraphResourceAccessKind.Read));

                if (hasReaders || outputSet.Contains(resource.Handle))
                {
                    throw new InvalidOperationException(
                        $"Resource '{resource.Name}' is read or exported, " +
                        "but its writer uses StoreAction.DontCare.");
                }

                continue;
            }

            // Imported resources can have multiple writers.
            // Declaration order defines their resource versions.
            RenderGraphStoreAction? latestStoreAction = null;

            foreach (var pass in passes)
            {
                foreach (var access in pass.ResourceAccesses)
                {
                    if (access.Resource != resource.Handle)
                    {
                        continue;
                    }

                    if (access.Kind == RenderGraphResourceAccessKind.Read)
                    {
                        if (latestStoreAction == RenderGraphStoreAction.DontCare)
                        {
                            throw new InvalidOperationException(
                                $"Pass '{pass.Name}' reads resource '{resource.Name}' " +
                                "after its contents were discarded.");
                        }

                        continue;
                    }

                    if (access.LoadAction == RenderGraphLoadAction.Load &&
                        latestStoreAction == RenderGraphStoreAction.DontCare)
                    {
                        throw new InvalidOperationException(
                            $"Pass '{pass.Name}' loads resource '{resource.Name}' " +
                            "after its contents were discarded.");
                    }

                    latestStoreAction = access.StoreAction;
                }
            }

            if (outputSet.Contains(resource.Handle) &&
                latestStoreAction == RenderGraphStoreAction.DontCare)
            {
                throw new InvalidOperationException(
                    $"Output resource '{resource.Name}' cannot use StoreAction.DontCare.");
            }
        }
    }

    private static void BuildImportedReaderOrderingDependencies(
    IReadOnlyList<RenderGraphResourceDefinition> resources,
    IReadOnlyList<RenderGraphPassDefinition> passes,
    IReadOnlySet<int> livePasses,
    IReadOnlyList<HashSet<int>> dependencies)
    {
        foreach (var resource in resources)
        {
            if (resource.Kind == RenderGraphResourceKind.Transient)
            {
                continue;
            }

            var liveReadersOfCurrentVersion = new List<int>();

            foreach (var pass in passes)
            {
                var passIndex = pass.Handle.Index;

                var access = pass.ResourceAccesses.FirstOrDefault(
                    item => item.Resource == resource.Handle);

                if (access.Resource != resource.Handle)
                {
                    continue;
                }

                if (access.Kind == RenderGraphResourceAccessKind.Read)
                {
                    if (livePasses.Contains(passIndex))
                    {
                        liveReadersOfCurrentVersion.Add(passIndex);
                    }

                    continue;
                }

                // Dead writers do not execute, so they must not
                // force ordering against live readers.
                if (!livePasses.Contains(passIndex))
                {
                    continue;
                }

                // Every live reader of the current version must
                // finish before this live writer overwrites it.
                foreach (var readerIndex in liveReadersOfCurrentVersion)
                {
                    AddDependency(
                        dependencies,
                        readerIndex,
                        passIndex);
                }

                liveReadersOfCurrentVersion.Clear();
            }
        }
    }

    private static void BuildExplicitDependencies(
        IReadOnlyList<RenderGraphPassDefinition> passes,
        IReadOnlyList<HashSet<int>> dependencies)
    {
        foreach (var pass in passes)
        {
            foreach (var dependency in pass.ExplicitDependencies)
            {
                if (!dependency.IsValid ||
                    dependency.GraphId != pass.Handle.GraphId ||
                    dependency.Index < 0 ||
                    dependency.Index >= pass.Handle.Index)
                {
                    throw new InvalidOperationException(
                        $"Pass '{pass.Name}' contains an invalid explicit dependency.");
                }

                AddDependency(
                    dependencies,
                    dependency.Index,
                    pass.Handle.Index);
            }
        }
    }

    private static void AddDependency(
        IReadOnlyList<HashSet<int>> dependencies,
        int prerequisiteIndex,
        int passIndex)
    {
        if (prerequisiteIndex == passIndex)
        {
            throw new InvalidOperationException(
                $"Pass '{passIndex}' depends on itself.");
        }

        dependencies[passIndex].Add(prerequisiteIndex);
    }

    private static IReadOnlyList<int> TopologicalSort(
        IReadOnlyList<RenderGraphPassDefinition> passes,
        IReadOnlyList<HashSet<int>> dependencies)
    {
        if (dependencies.Count != passes.Count)
        {
            throw new InvalidOperationException(
                "Render graph dependency count does not match pass count.");
        }

        var outgoing = new List<List<int>>(passes.Count);
        var indegree = new int[passes.Count];

        for (var i = 0; i < passes.Count; i++)
        {
            outgoing.Add(new List<int>());
            indegree[i] = dependencies[i].Count;
        }

        for (var i = 0; i < passes.Count; i++)
        {
            foreach (var prerequisite in dependencies[i])
            {
                if (prerequisite < 0 || prerequisite >= passes.Count)
                {
                    throw new InvalidOperationException(
                        $"Pass '{passes[i].Name}' references invalid dependency index '{prerequisite}'.");
                }

                outgoing[prerequisite].Add(i);
            }
        }

        // Preserve declaration order for independent passes.
        var ready = new PriorityQueue<int, int>();

        for (var i = 0; i < indegree.Length; i++)
        {
            if (indegree[i] == 0)
            {
                ready.Enqueue(i, i);
            }
        }

        var result = new List<int>(passes.Count);

        while (ready.TryDequeue(out var current, out _))
        {
            result.Add(current);

            foreach (var dependent in outgoing[current])
            {
                indegree[dependent]--;

                if (indegree[dependent] == 0)
                {
                    ready.Enqueue(dependent, dependent);
                }
            }
        }

        if (result.Count != passes.Count)
        {
            var cyclePasses = Enumerable.Range(0, passes.Count)
                .Where(index => indegree[index] > 0)
                .Select(index => passes[index].Name);

            throw new InvalidOperationException(
                "Render graph contains a dependency cycle involving: " +
                string.Join(", ", cyclePasses) + ".");
        }

        return result;
    }

    private static HashSet<int> FindLivePasses(
        IReadOnlyList<RenderGraphPassDefinition> passes,
        IReadOnlyList<RenderGraphResourceHandle> outputs,
        IReadOnlyList<RenderGraphResourceDefinition> resources,
        IReadOnlyList<int>[] writers,
        IReadOnlyList<HashSet<int>> dependencies)
    {
        var live = new HashSet<int>();
        var pending = new Stack<int>();

        foreach (var output in outputs)
        {
            var outputResource = resources[output.Index];
            var outputWriters = writers[output.Index];

            if (outputResource.Kind == RenderGraphResourceKind.Transient &&
                outputWriters.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Transient output '{outputResource.Name}' must have exactly one producer.");
            }

            if (outputWriters.Count > 0)
            {
                // For imported resources the final declared writer produces
                // the version visible at the graph output.
                pending.Push(outputWriters[^1]);
            }
        }
        // Passes with externally observable effects are always live.
        foreach (var pass in passes)
        {
            if (pass.HasSideEffects)
            {
                pending.Push(pass.Handle.Index);
            }
        }
        while (pending.Count > 0)
        {
            var current = pending.Pop();

            if (!live.Add(current))
            {
                continue;
            }

            foreach (var prerequisite in dependencies[current])
            {
                pending.Push(prerequisite);
            }
        }

        return live;
    }

    private static IReadOnlyList<RenderGraphCompiledPass> BuildCompiledPasses(
        IReadOnlyList<RenderGraphPassDefinition> passes,
        IReadOnlyList<HashSet<int>> dependencies,
        IReadOnlyList<int> topologicalOrder,
        IReadOnlySet<int> livePasses)
    {
        var result = new List<RenderGraphCompiledPass>(livePasses.Count);

        foreach (var passIndex in topologicalOrder)
        {
            if (!livePasses.Contains(passIndex))
            {
                continue;
            }

            var pass = passes[passIndex];
            var directDependencies = dependencies[passIndex]
                .Where(livePasses.Contains)
                .OrderBy(index => index)
                .Select(index => passes[index].Handle)
                .ToArray();

            result.Add(
                new RenderGraphCompiledPass(
                    pass.Handle,
                    pass.Name,
                    pass.State,
                    pass.Layers,
                    pass.ResourceAccesses,
                    directDependencies,
                    pass.Execute,
                    pass.ExecuteQueuedCommands,
                    pass.IsPostProcessSource,
                    pass.ContextData, pass.ClearDepth));
        }

        return result;
    }

    private static IReadOnlyList<RenderGraphCompiledResource> BuildCompiledResources(
        IReadOnlyList<RenderGraphResourceDefinition> resources,
        IReadOnlyList<RenderGraphResourceHandle> outputs,
        IReadOnlyList<RenderGraphCompiledPass> passes)
    {
        var outputSet = outputs.ToHashSet();
        var firstUse = Enumerable.Repeat(-1, resources.Count).ToArray();
        var lastUse = Enumerable.Repeat(-1, resources.Count).ToArray();

        for (var passPosition = 0; passPosition < passes.Count; passPosition++)
        {
            foreach (var access in passes[passPosition].ResourceAccesses)
            {
                var resourceIndex = access.Resource.Index;

                if (firstUse[resourceIndex] < 0)
                {
                    firstUse[resourceIndex] = passPosition;
                }

                lastUse[resourceIndex] = passPosition;
            }
        }

        var usedResourceIndices = new SortedSet<int>();

        foreach (var output in outputs)
        {
            usedResourceIndices.Add(output.Index);
        }

        foreach (var pass in passes)
        {
            foreach (var access in pass.ResourceAccesses)
            {
                usedResourceIndices.Add(access.Resource.Index);
            }
        }

        var result = new List<RenderGraphCompiledResource>(usedResourceIndices.Count);

        foreach (var resourceIndex in usedResourceIndices)
        {
            var resource = resources[resourceIndex];

            result.Add(
                new RenderGraphCompiledResource(
                    resource.Handle,
                    resource.Name,
                    resource.Kind,
                    resource.Descriptor,
                    resource.ImportedRenderTarget,
                    firstUse[resourceIndex],
                    lastUse[resourceIndex],
                    outputSet.Contains(resource.Handle)));
        }

        return result;
    }
}
