using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Tooling.Inspection;

namespace Engine.Tooling.Debugging;

internal static class EntityDebugLabel
{
    public static string Format(
        EntityId entity,
        IReadOnlyList<ComponentInspection> components)
    {
        foreach (var component in components)
        {
            if (component.Type != typeof(EntityName))
            {
                continue;
            }

            if (component.Value is not EntityName name ||
                !name.IsValid)
            {
                continue;
            }

            return
                $"Entity \"{name.Value}\" " +
                $"({entity.Index}:{entity.Generation})";
        }

        return
            $"Entity {entity.Index}:{entity.Generation}";
    }
}