using Engine.Editor.Documents;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Worlds;
using Engine.Worlds;

namespace Engine.Editor.Documents.Persistence;

public sealed class BinaryEditorDocumentPersistence :
    IEditorDocumentPersistence
{
    private readonly WorldSaveLoadService _worlds;

    public BinaryEditorDocumentPersistence(
        EcsComponentSerializerRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(
            registry);

        _worlds =
            new WorldSaveLoadService(
                registry);
    }

    public void Save(
        string path,
        EditorDocument document)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            path);

        ArgumentNullException.ThrowIfNull(
            document);

        _worlds.Save(
            path,
            document.World);
    }

    public World Load(
        string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            path);

        return _worlds.LoadWorld(
            path);
    }
}