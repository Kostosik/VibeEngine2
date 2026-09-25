namespace Engine.Editor.Assets;

public sealed record EditorAssetEntry(
    string Path,
    string Name,
    bool IsDirectory);