namespace Engine.Scripting;

public sealed class ScriptRegistry
{
    private readonly Dictionary<string, Func<IEntityScript>> _factories =
        new(StringComparer.Ordinal);

    public int Count =>
        _factories.Count;

    public IReadOnlyList<string> RegisteredIds =>
        _factories.Keys
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

    public void Register<TScript>(string id)
        where TScript : class, IEntityScript, new()
    {
        Register(id, static () => new TScript());
    }

    public void Register(
        string id,
        Func<IEntityScript> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(factory);

        if (!_factories.TryAdd(id, factory))
        {
            throw new InvalidOperationException(
                $"Script '{id}' is already registered.");
        }
    }

    public void Register(ScriptDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (!definition.HasImplementation)
        {
            throw new InvalidOperationException(
                $"Script definition '{definition.ScriptId}' " +
                "has no C# implementation assigned.");
        }

        Type? implementationType = null;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!string.Equals(
                    assembly.GetName().Name,
                    definition.ImplementationAssemblyName,
                    StringComparison.Ordinal))
            {
                continue;
            }

            implementationType =
                assembly.GetType(
                    definition.ImplementationTypeName!,
                    throwOnError: false,
                    ignoreCase: false);

            if (implementationType is not null)
            {
                break;
            }
        }

        if (implementationType is null)
        {
            throw new InvalidOperationException(
                $"C# implementation " +
                $"'{definition.ImplementationTypeName}' was not found " +
                $"in loaded assembly '{definition.ImplementationAssemblyName}'.");
        }

        Register(definition, implementationType);
    }

    public void Register(
        ScriptDefinition definition,
        Type implementationType)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(implementationType);

        if (!definition.HasImplementation)
        {
            throw new ArgumentException(
                "Script definition has no C# implementation.",
                nameof(definition));
        }

        if (!IsSupportedImplementationType(implementationType))
        {
            throw new ArgumentException(
                $"Type '{implementationType.FullName}' must be a concrete " +
                "IEntityScript class with a public parameterless constructor.",
                nameof(implementationType));
        }

        var assemblyName =
            implementationType.Assembly.GetName().Name;

        if (!string.Equals(
                definition.ImplementationAssemblyName,
                assemblyName,
                StringComparison.Ordinal) ||
            !string.Equals(
                definition.ImplementationTypeName,
                implementationType.FullName,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The supplied C# type does not match the script definition.",
                nameof(implementationType));
        }

        Register(
            definition.ScriptId,
            () => (IEntityScript)(
                Activator.CreateInstance(implementationType)
                ?? throw new InvalidOperationException(
                    $"Could not create script '{implementationType.FullName}'.")));
    }

    public static bool IsSupportedImplementationType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.IsClass &&
               !type.IsAbstract &&
               !type.ContainsGenericParameters &&
               typeof(IEntityScript).IsAssignableFrom(type) &&
               type.GetConstructor(Type.EmptyTypes) is not null;
    }

    public bool Contains(string id)
    {
        return _factories.ContainsKey(id);
    }

    public IEntityScript Create(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (!_factories.TryGetValue(id, out var factory))
        {
            throw new KeyNotFoundException(
                $"Script '{id}' is not registered.");
        }

        return factory()
            ?? throw new InvalidOperationException(
                $"Factory for script '{id}' returned null.");
    }
}