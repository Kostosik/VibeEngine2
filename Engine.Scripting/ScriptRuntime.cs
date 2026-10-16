using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.ECS;
using Engine.ECS.Entities;

namespace Engine.Scripting;

public sealed class ScriptRuntime : IDisposable
{
    private readonly World _world;
    private readonly ScriptRegistry _registry;

    private readonly List<AttachedScript> _instances = new();

    // Снимок нужен, чтобы безопасно обрабатывать удаление
    // скриптов во время выполнения.
    private readonly List<AttachedScript> _executionSnapshot = new();
    private bool _isExecuting;
    private bool _disposed;

    public ScriptRuntime(
        World world,
        ScriptRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(registry);

        _world = world;
        _registry = registry;

        UpdateSystem = new ScriptUpdateSystem(this);
        FixedUpdateSystem = new ScriptFixedUpdateSystem(this);
    }

    public IUpdateSystem UpdateSystem { get; }

    public IFixedUpdateSystem FixedUpdateSystem { get; }

    public int InstanceCount =>
        _instances.Count;

    public event Action<ScriptFault>? ScriptFaulted;

    public void Attach(
        EntityId entity,
        string scriptId)
    {
        Attach(
            entity,
            scriptId,
            "{}");
    }

    public void Attach(
        EntityId entity,
        string scriptId,
        string configurationJson)
    {
        EnsureNotDisposed();

        var assignment =
            new ScriptAssignment(
                scriptId,
                configurationJson);

        if (!_world.Exists(entity))
        {
            throw new ArgumentException(
                $"Entity '{entity}' does not exist.",
                nameof(entity));
        }

        if (_world.Has<ScriptAssignmentsComponent>(entity) &&
            _world.Get<ScriptAssignmentsComponent>(entity)
                .Contains(scriptId))
        {
            throw new InvalidOperationException(
                $"Script '{scriptId}' is already assigned " +
                $"to entity '{entity}'.");
        }

        AttachInstance(
            entity,
            assignment,
            persistAssignment: true);
    }

    private void AttachInstance(
    EntityId entity,
    ScriptAssignment assignment,
    bool persistAssignment)
    {
        if (!_world.Exists(entity))
        {
            throw new ArgumentException(
                $"Entity '{entity}' does not exist.",
                nameof(entity));
        }

        if (_instances.Any(instance =>
                instance.IsAttached &&
                instance.Entity == entity &&
                string.Equals(
                    instance.ScriptId,
                    assignment.ScriptId,
                    StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"Script '{assignment.ScriptId}' is already attached " +
                $"to entity '{entity}'.");
        }

        var script =
            _registry.Create(assignment.ScriptId);

        if (_instances.Any(instance =>
                instance.IsAttached &&
                ReferenceEquals(instance.Script, script)))
        {
            throw new InvalidOperationException(
                $"Script factory for '{assignment.ScriptId}' returned " +
                "an instance that is already attached.");
        }

        var context =
            new ScriptContext(
                _world,
                entity,
                assignment.ConfigurationJson);

        var instance =
            new AttachedScript(
                entity,
                assignment.ScriptId,
                script,
                context);

        if (persistAssignment)
        {
            AddAssignment(
                entity,
                assignment);
        }

        _instances.Add(instance);

        try
        {
            script.OnAttach(context);
        }
        catch
        {
            instance.IsAttached = false;
            _instances.Remove(instance);

            if (persistAssignment)
            {
                RemoveAssignment(
                    entity,
                    assignment.ScriptId);
            }

            throw;
        }
    }

    private void AddAssignment(
    EntityId entity,
    ScriptAssignment assignment)
    {
        if (_world.Has<ScriptAssignmentsComponent>(entity))
        {
            var current =
                _world.Get<ScriptAssignmentsComponent>(entity);

            _world.Add(
                entity,
                current.WithAdded(assignment));

            return;
        }

        _world.Add(
            entity,
            new ScriptAssignmentsComponent(
                new[] { assignment }));
    }

    private bool RemoveAssignment(
        EntityId entity,
        string scriptId)
    {
        if (!_world.Exists(entity) ||
            !_world.Has<ScriptAssignmentsComponent>(entity))
        {
            return false;
        }

        var current =
            _world.Get<ScriptAssignmentsComponent>(entity);

        if (!current.Contains(scriptId))
        {
            return false;
        }

        var updated =
            current.Without(scriptId);

        if (updated.Count == 0)
        {
            _world.Remove<ScriptAssignmentsComponent>(entity);
        }
        else
        {
            _world.Add(
                entity,
                updated);
        }

        return true;
    }

    public bool Detach(
        EntityId entity,
        string scriptId)
    {
        EnsureNotDisposed();

        ArgumentException.ThrowIfNullOrWhiteSpace(
            scriptId);

        var removedAssignment =
            RemoveAssignment(
                entity,
                scriptId);

        var instance =
            _instances.FirstOrDefault(
                item =>
                    item.IsAttached &&
                    item.Entity == entity &&
                    string.Equals(
                        item.ScriptId,
                        scriptId,
                        StringComparison.Ordinal));

        var detached =
            instance is not null &&
            DetachInstance(instance);

        return detached || removedAssignment;
    }

    public int DetachAll(EntityId entity)
    {
        EnsureNotDisposed();

        var assignedCount = 0;

        if (_world.Exists(entity) &&
            _world.Has<ScriptAssignmentsComponent>(entity))
        {
            assignedCount =
                _world.Get<ScriptAssignmentsComponent>(entity).Count;

            _world.Remove<ScriptAssignmentsComponent>(entity);
        }

        var snapshot = _instances
            .Where(instance =>
                instance.IsAttached &&
                instance.Entity == entity)
            .ToArray();

        foreach (var instance in snapshot)
        {
            DetachInstance(instance);
        }

        return Math.Max(
            assignedCount,
            snapshot.Length);
    }

    public void RebuildInstancesFromWorld()
    {
        EnsureNotDisposed();

        if (_isExecuting)
        {
            throw new InvalidOperationException(
                "Cannot rebuild script instances during execution.");
        }

        _isExecuting = true;

        try
        {
            foreach (var instance in _instances.ToArray())
            {
                DetachInstance(instance);
            }

            ReconcileAssignments();
        }
        finally
        {
            EndExecution();
        }
    }

    public void Attach(
    EntityId entity,
    ScriptDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(
            definition);

        Attach(
            entity,
            definition.ScriptId,
            definition.DefaultConfigurationJson);
    }

    private void Update(SystemContext context)
    {
        EnsureNotDisposed();
        BeginExecution();

        try
        {
            foreach (var instance in _executionSnapshot)
            {
                if (!instance.IsAttached ||
                    !instance.Enabled)
                {
                    continue;
                }

                if (!_world.Exists(instance.Entity))
                {
                    DetachInstance(instance);
                    continue;
                }

                try
                {
                    instance.Script.Update(
                        instance.Context,
                        context.Time);
                }
                catch (Exception exception)
                {
                    DisableAndReport(
                        instance,
                        ScriptExecutionPhase.Update,
                        exception);
                }
            }
        }
        finally
        {
            EndExecution();
        }
    }

    private void FixedUpdate(FixedSystemContext context)
    {
        EnsureNotDisposed();
        BeginExecution();

        try
        {
            foreach (var instance in _executionSnapshot)
            {
                if (!instance.IsAttached ||
                    !instance.Enabled)
                {
                    continue;
                }

                if (!_world.Exists(instance.Entity))
                {
                    DetachInstance(instance);
                    continue;
                }

                try
                {
                    instance.Script.FixedUpdate(
                        instance.Context,
                        context.Time);
                }
                catch (Exception exception)
                {
                    DisableAndReport(
                        instance,
                        ScriptExecutionPhase.FixedUpdate,
                        exception);
                }
            }
        }
        finally
        {
            EndExecution();
        }
    }

    private void BeginExecution()
    {
        if (_isExecuting)
        {
            throw new InvalidOperationException(
                "Script execution is already in progress.");
        }

        _isExecuting = true;

        try
        {
            ReconcileAssignments();

            _executionSnapshot.AddRange(
                _instances);
        }
        catch
        {
            EndExecution();
            throw;
        }
    }

    private void EndExecution()
    {
        _executionSnapshot.Clear();
        _isExecuting = false;
    }

    private void ReconcileAssignments()
    {
        var desired =
            new Dictionary<
                (EntityId Entity, string ScriptId),
                ScriptAssignment>();

        foreach (var item in
                 _world.Query<ScriptAssignmentsComponent>())
        {
            foreach (var assignment in
                     item.Component.Assignments)
            {
                desired.Add(
                    (item.Entity, assignment.ScriptId),
                    assignment);
            }
        }

        foreach (var instance in _instances.ToArray())
        {
            if (!desired.TryGetValue(
                    (instance.Entity, instance.ScriptId),
                    out var assignment) ||
                !string.Equals(
                    assignment.ConfigurationJson,
                    instance.Context.ConfigurationJson,
                    StringComparison.Ordinal))
            {
                DetachInstance(instance);
            }
        }

        var orderedAssignments = desired
            .OrderBy(entry => entry.Key.Entity.Index)
            .ThenBy(entry => entry.Key.Entity.Generation)
            .ThenBy(
                entry => entry.Key.ScriptId,
                StringComparer.Ordinal);

        foreach (var entry in orderedAssignments)
        {
            var entity =
                entry.Key.Entity;

            var assignment =
                entry.Value;

            var isAttached =
                _instances.Any(instance =>
                    instance.IsAttached &&
                    instance.Entity == entity &&
                    string.Equals(
                        instance.ScriptId,
                        assignment.ScriptId,
                        StringComparison.Ordinal) &&
                    string.Equals(
                        instance.Context.ConfigurationJson,
                        assignment.ConfigurationJson,
                        StringComparison.Ordinal));

            if (isAttached)
            {
                continue;
            }

            try
            {
                AttachInstance(
                    entity,
                    assignment,
                    persistAssignment: false);
            }
            catch (Exception exception)
            {
                RemoveAssignment(
                    entity,
                    assignment.ScriptId);

                ReportFault(
                    entity,
                    assignment.ScriptId,
                    ScriptExecutionPhase.Attach,
                    exception);
            }
        }
    }

    private bool DetachInstance(AttachedScript instance)
    {
        if (!instance.IsAttached)
        {
            return false;
        }

        instance.IsAttached = false;
        _instances.Remove(instance);

        try
        {
            instance.Script.OnDetach(instance.Context);
        }
        catch (Exception exception)
        {
            ReportFault(
                instance,
                ScriptExecutionPhase.Detach,
                exception);
        }

        return true;
    }

    private void DisableAndReport(
        AttachedScript instance,
        ScriptExecutionPhase phase,
        Exception exception)
    {
        instance.Enabled = false;

        ReportFault(
            instance,
            phase,
            exception);
    }

    private void ReportFault(
    AttachedScript instance,
    ScriptExecutionPhase phase,
    Exception exception)
    {
        ReportFault(
            instance.Entity,
            instance.ScriptId,
            phase,
            exception);
    }


    private void ReportFault(
        EntityId entity,
        string scriptId,
        ScriptExecutionPhase phase,
        Exception exception)
    {
        var handlers = ScriptFaulted;

        if (handlers is null)
        {
            return;
        }

        var fault = new ScriptFault(
            entity,
            scriptId,
            phase,
            exception);

        foreach (Action<ScriptFault> handler
                 in handlers.GetInvocationList())
        {
            try
            {
                handler(fault);
            }
            catch (Exception)
            {
                // Ошибка обработчика диагностики не должна
                // прерывать выполнение остальных скриптов.
            }
        }
    }


    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        while (_instances.Count > 0)
        {
            DetachInstance(
                _instances[^1]);
        }

        _executionSnapshot.Clear();
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }

    private sealed class AttachedScript
    {
        public AttachedScript(
            EntityId entity,
            string scriptId,
            IEntityScript script,
            ScriptContext context)
        {
            Entity = entity;
            ScriptId = scriptId;
            Script = script;
            Context = context;
        }

        public EntityId Entity { get; }

        public string ScriptId { get; }

        public IEntityScript Script { get; }

        public ScriptContext Context { get; }

        public bool IsAttached { get; set; } = true;

        public bool Enabled { get; set; } = true;
    }

    private sealed class ScriptUpdateSystem : IUpdateSystem
    {
        private readonly ScriptRuntime _runtime;

        public ScriptUpdateSystem(ScriptRuntime runtime)
        {
            _runtime = runtime;
        }

        public void Update(SystemContext context)
        {
            _runtime.Update(context);
        }
    }

    private sealed class ScriptFixedUpdateSystem : IFixedUpdateSystem
    {
        private readonly ScriptRuntime _runtime;

        public ScriptFixedUpdateSystem(ScriptRuntime runtime)
        {
            _runtime = runtime;
        }

        public void FixedUpdate(FixedSystemContext context)
        {
            _runtime.FixedUpdate(context);
        }
    }
}