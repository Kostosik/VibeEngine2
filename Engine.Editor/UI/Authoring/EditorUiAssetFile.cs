using Engine.Serialization.UI;

namespace Engine.Editor.UI.Authoring;

public static class EditorUiAssetFile
{
    public static void Save(
        EditorUiDocument document,
        string path)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            path);

        var asset =
            EditorUiAssetConverter.Convert(
                document);

        UiAssetFileSerializer.Save(
            path,
            asset);
    }
}