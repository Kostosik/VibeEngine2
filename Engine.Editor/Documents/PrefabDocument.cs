using Engine.ECS.Prefabs;
using Engine.Serialization.SaveLoad.Prefabs;

namespace Engine.Editor.Documents;

public sealed class PrefabDocument : IEditorDocument
{
    public const string TypeId = "vibeengine.prefab";

    private readonly PrefabAssetSaveLoadService _saveLoad;

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

    public string DisplayName =>
        Path.GetFileName(FilePath);

    public PrefabAsset Asset { get; }

    // Первая версия документа предназначена только для просмотра.
    public bool IsDirty => false;

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

        var asset = saveLoad.Load(fullPath);

        return new PrefabDocument(
            fullPath,
            asset,
            saveLoad);
    }

    public void Save()
    {
        _saveLoad.Save(FilePath, Asset);
    }

    public void MarkSaved()
    {
        // Документ пока неизменяемый.
    }
}