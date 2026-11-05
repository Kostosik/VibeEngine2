using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.ECS.Prefabs;
using Engine.Editor.Commands;
using Engine.Serialization.SaveLoad.Prefabs;

namespace Engine.Editor.Documents;

public sealed class PrefabDocument : IEditorDocument
{
    public const string TypeId = "vibeengine.prefab";

    private readonly PrefabAssetSaveLoadService _saveLoad;
    public bool IsDirty => CommandHistory.IsDirty;

    private PrefabDocument(
        string filePath,
        PrefabAsset asset,
        PrefabAssetSaveLoadService saveLoad)
    {
        FilePath = filePath;
        Asset = asset;
        _saveLoad = saveLoad;
    }

    public string DocumentTypeId => TypeId;

    public string FilePath { get; }

    public string DisplayName => Path.GetFileName(FilePath);

    public PrefabAsset Asset { get; private set; }

    public static PrefabDocument Open(
        string path,
        PrefabAssetSaveLoadService saveLoad)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(saveLoad);

        var fullPath = Path.GetFullPath(path);

        if (!fullPath.EndsWith(
                ".prefab",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The file must have a .prefab extension.",
                nameof(path));
        }

        return new PrefabDocument(
            fullPath,
            saveLoad.Load(fullPath),
            saveLoad);
    }

    public void AddComponent(
    PrefabEntityId entityId,
    PrefabComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);

        var entity = FindEntity(entityId);

        if (entity.Components.Any(
                existing => existing.ComponentType == component.ComponentType))
        {
            throw new InvalidOperationException(
                $"Entity '{entityId.Value}' already contains " +
                $"component '{component.ComponentType.Name}'.");
        }

        var before = entity.Components.ToArray();
        var after = before.Append(component).ToArray();

        CommandHistory.Execute(
            new SetPrefabEntityComponentsCommand(
                this,
                entityId,
                before,
                after));
    }

    public void RemoveComponent(
        PrefabEntityId entityId,
        Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        var entity = FindEntity(entityId);
        var before = entity.Components.ToArray();

        if (!before.Any(
                component => component.ComponentType == componentType))
        {
            throw new InvalidOperationException(
                $"Component '{componentType.Name}' was not found.");
        }

        var after = before
            .Where(component => component.ComponentType != componentType)
            .ToArray();

        CommandHistory.Execute(
            new SetPrefabEntityComponentsCommand(
                this,
                entityId,
                before,
                after));
    }

    private PrefabEntity FindEntity(PrefabEntityId entityId)
    {
        return Asset.Entities.FirstOrDefault(
            entity => entity.Id == entityId)
            ?? throw new InvalidOperationException(
                $"Prefab entity '{entityId.Value}' was not found.");
    }

    internal void ReplaceEntityComponents(
        PrefabEntityId entityId,
        IReadOnlyList<PrefabComponent> components)
    {
        ArgumentNullException.ThrowIfNull(components);

        var updatedEntities = Asset.Entities.ToArray();
        var entityIndex = Array.FindIndex(
            updatedEntities,
            entity => entity.Id == entityId);

        if (entityIndex < 0)
        {
            throw new InvalidOperationException(
                $"Prefab entity '{entityId.Value}' was not found.");
        }

        var entity = updatedEntities[entityIndex];

        updatedEntities[entityIndex] = new PrefabEntity(
            entityId,
            components);

        // Проверяем весь PrefabAsset до замены текущего.
        var updatedAsset = new PrefabAsset(
            Asset.Name,
            Asset.RootEntity,
            updatedEntities);

        Asset = updatedAsset;
    }

    public PrefabEntityId CreateChildEntity(PrefabEntityId parentId)
    {
        var parent = FindEntity(parentId);

        if (!parent.Components.Any(
                component => component.ComponentType == typeof(WorldTransform2D)))
        {
            throw new InvalidOperationException(
                "A parent entity must have WorldTransform2D.");
        }

        if (parent.Components.Any(
                component => component.ComponentType == typeof(TransformParent2D)) &&
            !parent.Components.Any(
                component => component.ComponentType == typeof(LocalTransform2D)))
        {
            throw new InvalidOperationException(
                "A parent with TransformParent2D must also have LocalTransform2D.");
        }

        var maxId = Asset.Entities.Max(entity => entity.Id.Value);
        var newId = new PrefabEntityId(checked(maxId + 1));

        var components = new PrefabComponent[]
        {
        PrefabComponent.Create(
            typeof(TransformParent2D),
            new TransformParent2D(
                new EntityId(parentId.Value, 0))),

        PrefabComponent.Create(
            typeof(LocalTransform2D),
            new LocalTransform2D()),

        PrefabComponent.Create(
            typeof(WorldTransform2D),
            new WorldTransform2D())
        };

        var before = Asset.Entities.ToArray();

        var after = before
            .Append(new PrefabEntity(newId, components))
            .ToArray();

        CommandHistory.Execute(
            new SetPrefabEntitiesCommand(this, before, after));

        return newId;
    }

    public void DeleteEntity(PrefabEntityId entityId)
    {
        if (entityId == Asset.RootEntity)
        {
            throw new InvalidOperationException(
                "The root entity cannot be deleted.");
        }

        FindEntity(entityId);

        var deletedIds = new HashSet<PrefabEntityId>
    {
        entityId
    };

        // Собираем всех потомков удаляемой Entity.
        bool changed;

        do
        {
            changed = false;

            foreach (var entity in Asset.Entities)
            {
                if (deletedIds.Contains(entity.Id))
                {
                    continue;
                }

                var parentComponent = entity.Components.FirstOrDefault(
                    component =>
                        component.ComponentType == typeof(TransformParent2D));

                if (parentComponent?.BoxedValue is not TransformParent2D parent)
                {
                    continue;
                }

                if (parent.Parent.Generation != 0)
                {
                    continue;
                }

                if (deletedIds.Contains(
                        new PrefabEntityId(parent.Parent.Index)))
                {
                    deletedIds.Add(entity.Id);
                    changed = true;
                }
            }
        }
        while (changed);

        // Не разрешаем оставшимся Entity ссылаться на удаляемые.
        foreach (var entity in Asset.Entities)
        {
            if (deletedIds.Contains(entity.Id))
            {
                continue;
            }

            foreach (var component in entity.Components)
            {
                component.RemapEntityReferences(reference =>
                {
                    if (reference.IsValid &&
                        reference.Generation == 0 &&
                        deletedIds.Contains(
                            new PrefabEntityId(reference.Index)))
                    {
                        throw new InvalidOperationException(
                            $"Cannot delete entity '{entityId.Value}': " +
                            $"entity '{entity.Id.Value}' contains a reference " +
                            "to the deleted hierarchy.");
                    }

                    return reference;
                });
            }
        }

        var before = Asset.Entities.ToArray();

        var after = before
            .Where(entity => !deletedIds.Contains(entity.Id))
            .ToArray();

        CommandHistory.Execute(
            new SetPrefabEntitiesCommand(this, before, after));
    }

    internal void ReplaceEntities(
        IReadOnlyList<PrefabEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var updatedAsset = new PrefabAsset(
            Asset.Name,
            Asset.RootEntity,
            entities);

        Asset = updatedAsset;
    }

    public PrefabComponent GetComponent(
        PrefabEntityId entityId,
        Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        var entity = Asset.Entities.FirstOrDefault(
            item => item.Id == entityId)
            ?? throw new InvalidOperationException(
                $"Prefab entity '{entityId.Value}' was not found.");

        return entity.Components.FirstOrDefault(
            component => component.ComponentType == componentType)
            ?? throw new InvalidOperationException(
                $"Component '{componentType.Name}' was not found " +
                $"on prefab entity '{entityId.Value}'.");
    }

    public void SetComponentValue(
    PrefabEntityId entityId,
    Type componentType,
    object value)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        ArgumentNullException.ThrowIfNull(value);

        if (value.GetType() != componentType)
        {
            throw new ArgumentException(
                $"Value must have type '{componentType.FullName}'.",
                nameof(value));
        }

        var oldValue = GetComponent(
            entityId,
            componentType).BoxedValue;

        if (Equals(oldValue, value))
        {
            return;
        }

        CommandHistory.Execute(
            new SetPrefabComponentValueCommand(
                this,
                entityId,
                componentType,
                oldValue,
                value));
    }

    internal void ReplaceComponentValue(
    PrefabEntityId entityId,
    Type componentType,
    object value)
    {
        var entityIndex = -1;

        for (var i = 0; i < Asset.Entities.Count; i++)
        {
            if (Asset.Entities[i].Id == entityId)
            {
                entityIndex = i;
                break;
            }
        }

        if (entityIndex < 0)
        {
            throw new InvalidOperationException(
                $"Prefab entity '{entityId.Value}' was not found.");
        }

        var entity = Asset.Entities[entityIndex];
        var components = entity.Components.ToArray();

        var componentIndex = Array.FindIndex(
            components,
            component => component.ComponentType == componentType);

        if (componentIndex < 0)
        {
            throw new InvalidOperationException(
                $"Component '{componentType.Name}' was not found.");
        }

        components[componentIndex] =
            PrefabComponent.Create(componentType, value);

        var updatedEntities = Asset.Entities.ToArray();

        updatedEntities[entityIndex] = new PrefabEntity(
            entity.Id,
            components);

        var updatedAsset = new PrefabAsset(
            Asset.Name,
            Asset.RootEntity,
            updatedEntities);

        Asset = updatedAsset;
    }

    public void Save()
    {
        _saveLoad.Save(FilePath, Asset);
        MarkSaved();
    }

    public void MarkSaved()
    {
        CommandHistory.MarkSaved();
    }

    public EditorCommandHistory CommandHistory { get; } = new();

    public void Undo()
    {
        CommandHistory.Undo();
    }

    public void Redo()
    {
        CommandHistory.Redo();
    }
}