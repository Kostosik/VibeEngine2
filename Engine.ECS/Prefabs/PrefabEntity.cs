namespace Engine.ECS.Prefabs;

/// <summary>
/// Describes one entity inside a prefab.
/// </summary>
public sealed class PrefabEntity
{
    public PrefabEntity(
        PrefabEntityId id,
        IReadOnlyList<PrefabComponent> components)
    {
        if (!id.IsValid)
        {
            throw new ArgumentException(
                "Prefab entity ID must be valid.",
                nameof(id));
        }

        ArgumentNullException.ThrowIfNull(components);

        var componentTypes = new HashSet<Type>();

        foreach (var component in components)
        {
            ArgumentNullException.ThrowIfNull(component);

            if (!componentTypes.Add(component.ComponentType))
            {
                throw new InvalidDataException(
                    $"Prefab entity '{id.Value}' contains " +
                    $"duplicate component '{component.ComponentType.FullName}'.");
            }
        }

        Id = id;
        Components = Array.AsReadOnly(components.ToArray());
    }

    public PrefabEntityId Id { get; }

    public IReadOnlyList<PrefabComponent> Components { get; }
}