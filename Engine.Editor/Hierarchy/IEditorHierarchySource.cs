namespace Engine.Editor.Hierarchy;

public interface IEditorHierarchySource
{
    IReadOnlyList<EditorHierarchyNode> GetNodes();
}