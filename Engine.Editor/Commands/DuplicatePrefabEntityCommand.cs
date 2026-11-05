
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.ECS.Prefabs;
using Engine.Editor.Documents;

namespace Engine.Editor.Commands;

public sealed class DuplicatePrefabEntityCommand : IEditorCommand
{
    private readonly PrefabDocument _document;
    private readonly IReadOnlyList<PrefabEntity> _before;
    private readonly IReadOnlyList<PrefabEntity> _after;

    public DuplicatePrefabEntityCommand(
        PrefabDocument document,
        PrefabEntityId entityId)
    {
        ArgumentNullException.ThrowIfNull(document);

        _document = document;

        var asset = document.Asset;

        var source = asset.Entities.FirstOrDefault(
            entity => entity.Id == entityId)
            ?? throw new ArgumentException(
                $"Prefab entity '{entityId.Value}' was not found.",
                nameof(entityId));

        if (entityId == asset.RootEntity)
        {
            throw new InvalidOperationException(
                "The prefab root entity cannot be duplicated.");
        }

        var subtree = CollectSubtree(asset, source);

        // Создаём новые ID для каждой Entity копируемого поддерева.
        var entityMap = new Dictionary<
            PrefabEntityId,
            PrefabEntityId>();

        var nextId = asset.Entities.Max(
            entity => entity.Id.Value);

        foreach (var entity in subtree)
        {
            nextId = checked(nextId + 1);

            entityMap.Add(
                entity.Id,
                new PrefabEntityId(nextId));
        }

        var duplicatedEntities = new List<PrefabEntity>(
            subtree.Count);

        foreach (var entity in subtree)
        {
            var newId = entityMap[entity.Id];

            var components = entity.Components
                .Select(component =>
                    component.RemapEntityReferences(reference =>
                    {
                        if (!reference.IsValid ||
                            reference.Generation != 0)
                        {
                            return reference;
                        }

                        var referencedId =
                            new PrefabEntityId(reference.Index);

                        // Внутренние ссылки должны вести на копии.
                        if (entityMap.TryGetValue(
                                referencedId,
                                out var remappedId))
                        {
                            return new EntityId(
                                remappedId.Value,
                                0);
                        }

                        // Внешние ссылки остаются неизменными.
                        return reference;
                    }))
                .ToList();

            // Отличаем новую корневую Entity от оригинала.
            if (entity.Id == entityId)
            {
                UpdateDuplicateName(
                    asset,
                    entity,
                    components);
            }

            duplicatedEntities.Add(
                new PrefabEntity(
                    newId,
                    components));
        }

        _before = Array.AsReadOnly(
            asset.Entities.ToArray());

        var after = asset.Entities
            .Concat(duplicatedEntities)
            .ToArray();

        // Проверяем целостность ссылок до выполнения команды.
        _ = new PrefabAsset(
            asset.Name,
            asset.RootEntity,
            after);

        _after = Array.AsReadOnly(after);

        DuplicatedRootId = entityMap[entityId];
    }

    public PrefabEntityId DuplicatedRootId { get; }

    public void Execute()
    {
        _document.ReplaceEntities(_after);
    }

    public void Undo()
    {
        _document.ReplaceEntities(_before);
    }

    private static List<PrefabEntity> CollectSubtree(
        PrefabAsset asset,
        PrefabEntity root)
    {
        var children = asset.Entities.ToDictionary(
            entity => entity.Id,
            _ => new List<PrefabEntity>());

        foreach (var entity in asset.Entities)
        {
            if (!TryGetParentId(entity, out var parentId))
            {
                continue;
            }

            if (children.TryGetValue(parentId, out var list))
            {
                list.Add(entity);
            }
        }

        var result = new List<PrefabEntity>();
        var visited = new HashSet<PrefabEntityId>();

        void Visit(PrefabEntity entity)
        {
            if (!visited.Add(entity.Id))
            {
                throw new InvalidOperationException(
                    "Cannot duplicate a cyclic prefab hierarchy.");
            }

            result.Add(entity);

            foreach (var child in children[entity.Id])
            {
                Visit(child);
            }
        }

        Visit(root);

        return result;
    }

    private static bool TryGetParentId(
        PrefabEntity entity,
        out PrefabEntityId parentId)
    {
        var component = entity.Components
            .OfType<PrefabComponent<TransformParent2D>>()
            .FirstOrDefault();

        if (component is null ||
            !component.Component.Parent.IsValid)
        {
            parentId = PrefabEntityId.Invalid;
            return false;
        }

        var reference = component.Component.Parent;

        if (reference.Generation != 0)
        {
            throw new InvalidOperationException(
                $"Entity '{entity.Id.Value}' contains an invalid prefab parent reference.");
        }

        parentId = new PrefabEntityId(reference.Index);
        return true;
    }

    private static void UpdateDuplicateName(
        PrefabAsset asset,
        PrefabEntity source,
        List<PrefabComponent> components)
    {
        var sourceName = source.Components
            .OfType<PrefabComponent<EntityName>>()
            .FirstOrDefault();

        if (sourceName is null ||
            !sourceName.Component.IsValid)
        {
            return;
        }

        var baseName = $"{sourceName.Component.Value} Copy";

        var existingNames = new HashSet<string>(
            asset.Entities
                .SelectMany(entity => entity.Components)
                .OfType<PrefabComponent<EntityName>>()
                .Where(component => component.Component.IsValid)
                .Select(component => component.Component.Value),
            StringComparer.Ordinal);

        var uniqueName = baseName;
        var suffix = 2;

        while (!existingNames.Add(uniqueName))
        {
            uniqueName = $"{baseName} {suffix}";
            suffix++;
        }

        var componentIndex = components.FindIndex(
            component =>
                component.ComponentType == typeof(EntityName));

        if (componentIndex >= 0)
        {
            components[componentIndex] =
                PrefabComponent.Create(
                    typeof(EntityName),
                    new EntityName(uniqueName));
        }
    }
}
