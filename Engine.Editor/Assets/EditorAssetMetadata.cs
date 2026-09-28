namespace Engine.Editor.Assets;

public enum EditorAssetPreviewKind
{
    None,
    Image
}

public sealed record EditorAssetMetadata(
    string Path,
    string Name,
    string Extension,
    long SizeBytes,
    EditorAssetPreviewKind PreviewKind);