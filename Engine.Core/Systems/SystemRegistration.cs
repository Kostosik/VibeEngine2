namespace Engine.Core.Systems;

public sealed class SystemRegistration
{
    private readonly HashSet<Type> _before = new();
    private readonly HashSet<Type> _after = new();

    private bool _locked;

    internal SystemRegistration(
        Type systemType,
        object system,
        SystemPhase phase,
        int registrationOrder)
    {
        SystemType = systemType;
        System = system;
        Phase = phase;
        RegistrationOrder = registrationOrder;
    }

    public Type SystemType { get; }

    public object System { get; }

    public SystemPhase Phase { get; }

    internal int RegistrationOrder { get; }

    internal IReadOnlySet<Type> BeforeTypes =>
        _before;

    internal IReadOnlySet<Type> AfterTypes =>
        _after;

    public SystemRegistration Before<TSystem>()
    {
        EnsureUnlocked();

        _before.Add(
            typeof(TSystem));

        return this;
    }

    public SystemRegistration After<TSystem>()
    {
        EnsureUnlocked();

        _after.Add(
            typeof(TSystem));

        return this;
    }

    internal void Lock()
    {
        _locked = true;
    }

    private void EnsureUnlocked()
    {
        if (_locked)
        {
            throw new InvalidOperationException(
                "System registration has already been locked.");
        }
    }
}