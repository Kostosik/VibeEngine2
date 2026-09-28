using Engine.Serialization.Binary;
using Engine.UI.Assets;

namespace Engine.Serialization.UI;

public static class UiAssetFileSerializer
{
    private static readonly UiAssetSerializer Serializer =
        new();

    public static void Save(
        string path,
        UiAsset asset)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(asset);

        var directory =
            Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(
                directory);
        }

        BinaryFileSerializer.Save(
            path,
            asset,
            Serializer);
    }

    public static UiAsset Load(
        string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return BinaryFileSerializer.Load(
            path,
            Serializer);
    }
}