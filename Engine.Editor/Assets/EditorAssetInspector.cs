namespace Engine.Editor.Assets;

public sealed class EditorAssetInspector
{
    public EditorAssetMetadata Inspect(
        EditorAssetEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.IsDirectory)
        {
            throw new InvalidOperationException(
                "Directories cannot be inspected as file assets.");
        }

        var file =
            new FileInfo(
                entry.Path);

        if (!file.Exists)
        {
            throw new FileNotFoundException(
                $"Asset '{entry.Path}' was not found.",
                entry.Path);
        }

        var extension =
            file.Extension;

        var previewKind =
            EditorAssetTypeClassifier.Classify(entry) ==
            EditorAssetKind.Image
                ? EditorAssetPreviewKind.Image
                : EditorAssetPreviewKind.None;

        return new EditorAssetMetadata(
            file.FullName,
            file.Name,
            extension,
            file.Length,
            previewKind);
    }
}