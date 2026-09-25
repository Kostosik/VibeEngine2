namespace Engine.Editor.Inspection;

public interface IEditorPropertyProvider
{
    int Priority { get; }
    bool CanInspect(
        Type type);

    IReadOnlyList<EditorProperty> GetProperties(
        object target);
}