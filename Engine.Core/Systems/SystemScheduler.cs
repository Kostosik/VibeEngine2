namespace Engine.Core.Systems;

public sealed class SystemScheduler
{
    private readonly List<SystemRegistration> _registrations = new();
    private readonly ISystemExecutionStrategy _executionStrategy;
    private readonly Dictionary<
        Type,
        SystemRegistration> _registrationsByType = new();

    private SystemExecutionPlan<IUpdateSystem>? _updatePlan;

    private SystemExecutionPlan<IFixedUpdateSystem>? _fixedUpdatePlan;

    private bool _built;

    public int SystemCount =>
        _registrations.Count;

    public bool IsBuilt =>
        _built;

    public event Action<SystemRegistration>? SystemExecuting;

    public event Action<SystemRegistration>? SystemExecuted;

    public SystemScheduler(
    ISystemExecutionStrategy? executionStrategy = null)
    {
        _executionStrategy =
            executionStrategy ??
            new SequentialSystemExecutionStrategy();
    }

    public SystemRegistration Add(
        object system,
        SystemPhase phase)
    {
        ArgumentNullException.ThrowIfNull(
            system);

        EnsureNotBuilt();

        var systemType =
            system.GetType();

        if (_registrations.Any(
                registration =>
                    registration.SystemType == systemType))
        {
            throw new InvalidOperationException(
                $"System '{systemType.Name}' is already registered.");
        }

        ValidatePhase(
            system,
            phase);

        var registration =
            new SystemRegistration(
                systemType,
                system,
                phase,
                _registrations.Count);

        _registrations.Add(
            registration);

        _registrationsByType.Add(
            systemType,
            registration);

        return registration;
    }

    public void Build()
    {
        EnsureNotBuilt();

        ValidateDependencies();

        var updateRegistrations =
            _registrations
                .Where(registration =>
                    registration.Phase == SystemPhase.Update)
                .ToList();

        var fixedRegistrations =
            _registrations
                .Where(registration =>
                    registration.Phase == SystemPhase.FixedUpdate)
                .ToList();

        _updatePlan =
            SystemExecutionPlan<IUpdateSystem>.Build(
                updateRegistrations);

        _fixedUpdatePlan =
            SystemExecutionPlan<IFixedUpdateSystem>.Build(
                fixedRegistrations);

        foreach (var registration in _registrations)
        {
            registration.Lock();
        }

        _built = true;
    }

    public void Update(
        SystemContext context)
    {
        EnsureBuilt();

        _updatePlan!.Execute(
            _executionStrategy,
            system =>
                ExecuteUpdateSystem(
                    system,
                    context));
    }

    public void FixedUpdate(
        FixedSystemContext context)
    {
        EnsureBuilt();

        _fixedUpdatePlan!.Execute(
            _executionStrategy,
            system =>
                ExecuteFixedUpdateSystem(
                    system,
                    context));
    }

    private void ExecuteUpdateSystem(
        IUpdateSystem system,
        SystemContext context)
    {
        var registration =
            GetRegistration(system);

        SystemExecuting?.Invoke(
            registration);

        try
        {
            system.Update(
                context);
        }
        finally
        {
            SystemExecuted?.Invoke(
                registration);
        }
    }

    private void ExecuteFixedUpdateSystem(
        IFixedUpdateSystem system,
        FixedSystemContext context)
    {
        var registration =
            GetRegistration(system);

        SystemExecuting?.Invoke(
            registration);

        try
        {
            system.FixedUpdate(
                context);
        }
        finally
        {
            SystemExecuted?.Invoke(
                registration);
        }
    }

    private SystemRegistration GetRegistration(
        object system)
    {
        var systemType =
            system.GetType();

        if (_registrationsByType.TryGetValue(
                systemType,
                out var registration))
        {
            return registration;
        }

        throw new InvalidOperationException(
            $"System '{systemType.Name}' is not registered.");
    }

    private void ValidatePhase(
        object system,
        SystemPhase phase)
    {
        switch (phase)
        {
            case SystemPhase.Update
                when system is not IUpdateSystem:

                throw new ArgumentException(
                    $"System '{system.GetType().Name}' " +
                    "must implement IUpdateSystem.",
                    nameof(system));

            case SystemPhase.FixedUpdate
                when system is not IFixedUpdateSystem:

                throw new ArgumentException(
                    $"System '{system.GetType().Name}' " +
                    "must implement IFixedUpdateSystem.",
                    nameof(system));

            case SystemPhase.Update:
            case SystemPhase.FixedUpdate:
                return;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(phase),
                    phase,
                    "Unknown system phase.");
        }
    }

    private void ValidateDependencies()
    {
        var registrations =
            _registrations.ToDictionary(
                registration =>
                    registration.SystemType);

        foreach (var registration in _registrations)
        {
            foreach (var dependency in registration.AfterTypes)
            {
                ValidateDependency(
                    registration,
                    dependency,
                    registrations);
            }

            foreach (var dependency in registration.BeforeTypes)
            {
                ValidateDependency(
                    registration,
                    dependency,
                    registrations);
            }
        }
    }

    private static void ValidateDependency(
        SystemRegistration registration,
        Type dependency,
        Dictionary<Type, SystemRegistration> registrations)
    {
        if (!registrations.TryGetValue(
                dependency,
                out var dependencyRegistration))
        {
            throw new InvalidOperationException(
                $"System '{registration.SystemType.Name}' " +
                $"references '{dependency.Name}', " +
                "but that system is not registered.");
        }

        if (dependencyRegistration.Phase != registration.Phase)
        {
            throw new InvalidOperationException(
                $"System '{registration.SystemType.Name}' " +
                $"cannot depend on '{dependency.Name}' " +
                "because they belong to different phases.");
        }
    }

    private void EnsureNotBuilt()
    {
        if (_built)
        {
            throw new InvalidOperationException(
                "Scheduler has already been built.");
        }
    }

    private void EnsureBuilt()
    {
        if (!_built)
        {
            throw new InvalidOperationException(
                "Scheduler must be built before execution.");
        }
    }
}