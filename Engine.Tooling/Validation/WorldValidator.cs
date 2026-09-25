using Engine.ECS;
using Engine.ECS.Inspection;
using Engine.ECS.Entities;

namespace Engine.Tooling.Validation;

public sealed class WorldValidator : IValidator
{
    private readonly World _world;
    private readonly IWorldInspector _inspector;

    public WorldValidator(
        World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        _world = world;
        _inspector = world.Inspector;
    }

    public ValidationResult Validate(
        ValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var result =
            new ValidationResult();

        ValidateEntities(result);

        return result;
    }

    private void ValidateEntities(
        ValidationResult result)
    {
        var entities =
            _inspector.GetEntities();

        var knownEntities =
            new HashSet<EntityId>();

        foreach (var entity in entities)
        {
            if (!entity.IsValid)
            {
                result.Add(
                    ValidationSeverity.Error,
                    "ECS_INVALID_ENTITY_ID",
                    $"World inspector returned invalid entity '{entity}'.");
            }

            if (!knownEntities.Add(entity))
            {
                result.Add(
                    ValidationSeverity.Error,
                    "ECS_DUPLICATE_ENTITY",
                    $"World inspector returned duplicate entity '{entity}'.");
            }

            if (!_world.Exists(entity))
            {
                result.Add(
                    ValidationSeverity.Error,
                    "ECS_UNKNOWN_ENTITY",
                    $"World inspector returned entity '{entity}' that does not exist.");
            }

            ValidateComponents(
                entity,
                result);
        }

        if (_world.EntityCount != entities.Count)
        {
            result.Add(
                ValidationSeverity.Error,
                "ECS_ENTITY_COUNT_MISMATCH",
                $"World entity count is {_world.EntityCount}, " +
                $"but inspector returned {entities.Count} entities.");
        }
    }

    private void ValidateComponents(
        EntityId entity,
        ValidationResult result)
    {
        var componentTypes =
            _inspector.GetComponentTypes(entity);

        var knownTypes =
            new HashSet<Type>();

        foreach (var componentType in componentTypes)
        {
            if (!knownTypes.Add(componentType))
            {
                result.Add(
                    ValidationSeverity.Error,
                    "ECS_DUPLICATE_COMPONENT_TYPE",
                    $"Entity '{entity}' contains duplicate component type " +
                    $"'{componentType.FullName ?? componentType.Name}'.");
            }

            if (!componentType.IsValueType)
            {
                result.Add(
                    ValidationSeverity.Error,
                    "ECS_INVALID_COMPONENT_TYPE",
                    $"Entity '{entity}' contains component type " +
                    $"'{componentType.FullName ?? componentType.Name}', " +
                    "which is not a value type.");
            }

            if (!_inspector.TryGetComponent(
                    entity,
                    componentType,
                    out var component))
            {
                result.Add(
                    ValidationSeverity.Error,
                    "ECS_MISSING_COMPONENT",
                    $"Entity '{entity}' reports component type " +
                    $"'{componentType.FullName ?? componentType.Name}', " +
                    "but the component cannot be retrieved.");

                continue;
            }

            if (component is null)
            {
                result.Add(
                    ValidationSeverity.Error,
                    "ECS_NULL_COMPONENT",
                    $"Entity '{entity}' returned null for component type " +
                    $"'{componentType.FullName ?? componentType.Name}.");

                continue;
            }

            if (component.GetType() != componentType)
            {
                result.Add(
                    ValidationSeverity.Error,
                    "ECS_COMPONENT_TYPE_MISMATCH",
                    $"Entity '{entity}' reports component type " +
                    $"'{componentType.FullName ?? componentType.Name}', " +
                    $"but returned '{component.GetType().FullName ?? component.GetType().Name}'.");
            }
        }
    }
}