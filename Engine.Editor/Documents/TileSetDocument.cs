using Engine.Core.Assets;
using Engine.Graphics.Tilemaps;
using Engine.Serialization.Content;

namespace Engine.Editor.Documents;

public sealed class TileSetDocument : IEditorDocument
{
    public const string TypeId = "vibeengine.tile-set";

    private byte[] _savedSnapshot;

    private TileSetDocument(
        string filePath,
        TileSetDefinition definition)
    {
        FilePath = filePath;
        Definition = definition;

        _savedSnapshot =
            JsonTileSetDefinitionSerializer.Serialize(definition);
    }

    public string DocumentTypeId => TypeId;

    public string FilePath { get; }

    public string DisplayName =>
        Path.GetFileName(FilePath);

    public TileSetDefinition Definition { get; private set; }

    public bool IsDirty =>
        !JsonTileSetDefinitionSerializer
            .Serialize(Definition)
            .AsSpan()
            .SequenceEqual(_savedSnapshot);

    public event Action? Changed;

    public static TileSetDocument Open(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);

        if (!fullPath.EndsWith(
                ".tileset.json",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The file must have a .tileset.json extension.",
                nameof(path));
        }

        var definition =
            JsonTileSetDefinitionSerializer.Deserialize(
                GetAssetPath(fullPath),
                File.ReadAllBytes(fullPath));

        return new TileSetDocument(
            fullPath,
            definition);
    }

    public void UpdateDefinition(
        TileSetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var contents =
            JsonTileSetDefinitionSerializer.Serialize(definition);

        // Проверяем, что изменённое определение можно прочитать
        // тем же сериализатором, который используется при загрузке.
        _ = JsonTileSetDefinitionSerializer.Deserialize(
            GetAssetPath(FilePath),
            contents);

        if (contents.AsSpan().SequenceEqual(
                JsonTileSetDefinitionSerializer
                    .Serialize(Definition)))
        {
            return;
        }

        Definition = definition;
        Changed?.Invoke();
    }

    public void Save()
    {
        var contents =
            JsonTileSetDefinitionSerializer.Serialize(Definition);

        _ = JsonTileSetDefinitionSerializer.Deserialize(
            GetAssetPath(FilePath),
            contents);

        var directory = Path.GetDirectoryName(FilePath)
            ?? throw new InvalidOperationException(
                "Could not determine the TileSet directory.");

        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(FilePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllBytes(temporaryPath, contents);

            File.Move(
                temporaryPath,
                FilePath,
                overwrite: true);

            _savedSnapshot = contents;
        }
        finally
        {
            // Убираем временный файл при ошибке сохранения.
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch
            {
                // Не маскируем исходную ошибку сохранения.
            }
        }

        Changed?.Invoke();
    }

    public void MarkSaved()
    {
        _savedSnapshot =
            JsonTileSetDefinitionSerializer.Serialize(Definition);

        Changed?.Invoke();
    }

    private static AssetPath GetAssetPath(string fullPath) =>
        new(Path.GetFileName(fullPath));
}