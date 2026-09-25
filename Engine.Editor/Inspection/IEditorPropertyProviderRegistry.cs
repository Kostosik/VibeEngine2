namespace Engine.Editor.Inspection;

public interface IEditorPropertyProviderRegistry
{
    void Register(
        IEditorPropertyProvider provider);

    bool Unregister(
        IEditorPropertyProvider provider);

    bool TryGetProvider(
        Type type,
        out IEditorPropertyProvider? provider);
}