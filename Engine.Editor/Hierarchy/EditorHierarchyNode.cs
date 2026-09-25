namespace Engine.Editor.Hierarchy;

public sealed record EditorHierarchyNode(
    object Id,
    string Name,
    object? ParentId);