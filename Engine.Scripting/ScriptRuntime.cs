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
        EnsureNotDisposed();

        ArgumentException.ThrowIfNullOrWhiteSpace(scriptId);

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
                    scriptId,
                    StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"Script '{scriptId}' is already attached " +
                $"to entity '{entity}'.");
        }

        var script = _registry.Create(scriptId);

        if (_instances.Any(instance =>
                instance.IsAttached &&
                ReferenceEquals(instance.Script, script)))
        {
            throw new InvalidOperationException(
                $"Script factory for '{scriptId}' returned " +
                "an instance that is already attached.");
        }

        var context = new ScriptContext(
            _world,
            entity);

        var instance = new AttachedScript(
            entity,
            scriptId,
            script,
            context);

        _instances.Add(instance);

        try
        {
            script.OnAttach(context);
        }
        catch
        {
            instance.IsAttached = false;
            _instances.Remove(instance);
            throw;
        }
    }

    public bool Detach(
        EntityId entity,
        string scriptId)
    {
        EnsureNotDisposed();

        ArgumentException.ThrowIfNullOrWhiteSpace(scriptId);

        var instance = _instances.FirstOrDefault(
            item =>
                item.IsAttached &&
                item.Entity == entity &&
                string.Equals(
                    item.ScriptId,
                    scriptId,
                    StringComparison.Ordinal));

        return instance is not null &&
               DetachInstance(instance);
    }

    public int DetachAll(EntityId entity)
    {
        EnsureNotDisposed();

        var snapshot = _instances
            .Where(instance =>
                instance.IsAttached &&
                instance.Entity == entity)
            .ToArray();

        foreach (var instance in snapshot)
        {
            DetachInstance(instance);
        }

        return snapshot.Length;
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
        if (_executionSnapshot.Count != 0)
        {
            throw new InvalidOperationException(
                "Script execution is already in progress.");
        }

        _executionSnapshot.AddRange(_instances);
    }

    private void EndExecution()
    {
        _executionSnapshot.Clear();
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
        ScriptFaulted?.Invoke(
            new ScriptFault(
                instance.Entity,
                instance.ScriptId,
                phase,
                exception));
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