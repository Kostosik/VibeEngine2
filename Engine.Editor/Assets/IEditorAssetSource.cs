namespace Engine.Editor.Assets;

public interface IEditorAssetSource
{
    IReadOnlyList<EditorAssetEntry> GetEntries(
        string path);
}