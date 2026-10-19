namespace Engine.Editor.Assets;

public enum EditorAssetKind
{
    Directory,
    Image,
    Audio,
    Video,
    Font,
    Script,
    Model,
    Data,
    Other
}

public static class EditorAssetTypeClassifier
{
    public static EditorAssetKind Classify(
        EditorAssetEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.IsDirectory)
        {
            return EditorAssetKind.Directory;
        }

        // Например, Scripts/MyScript.script.json.
        if (entry.Name.EndsWith(
                ".script.json",
                StringComparison.OrdinalIgnoreCase))
        {
            return EditorAssetKind.Script;
        }

        var extension = Path.GetExtension(entry.Name);

        return extension.ToLowerInvariant() switch
        {
            ".png" or ".jpg" or ".jpeg" or ".bmp"
                or ".tga" or ".gif" or ".webp"
                => EditorAssetKind.Image,

            ".wav" or ".ogg" or ".mp3" or ".flac"
                or ".aac" or ".m4a"
                => EditorAssetKind.Audio,

            ".mp4" or ".mov" or ".webm" or ".avi"
                => EditorAssetKind.Video,

            ".ttf" or ".otf" or ".woff" or ".woff2"
                => EditorAssetKind.Font,

            ".cs" or ".csx"
                => EditorAssetKind.Script,

            ".obj" or ".fbx" or ".gltf" or ".glb"
                or ".stl" or ".dae"
                => EditorAssetKind.Model,

            ".json" or ".xml" or ".yaml" or ".yml"
                or ".csv" or ".toml"
                => EditorAssetKind.Data,

            _ => EditorAssetKind.Other
        };
    }
}