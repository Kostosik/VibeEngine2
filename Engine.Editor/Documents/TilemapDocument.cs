using Engine.Core.Assets;
using Engine.Graphics.Tilemaps;
using Engine.Serialization.Content;

namespace Engine.Editor.Documents;

public sealed class TilemapDocument : IEditorDocument
{
    public const string TypeId = "vibeengine.tilemap";

    private byte[] _savedSnapshot;
    private readonly Stack<TilemapDefinition> _undoStack = new();
    private readonly Stack<TilemapDefinition> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    private TilemapDocument(
        string filePath,
        TilemapDefinition definition)
    {
        FilePath = filePath;
        Definition = definition;
        _savedSnapshot = JsonTilemapDefinitionSerializer.Serialize(definition);
    }

    public string DocumentTypeId => TypeId;

    public string FilePath { get; }

    public string DisplayName => Path.GetFileName(FilePath);

    public TilemapDefinition Definition { get; private set; }

    public bool IsDirty =>
        !JsonTilemapDefinitionSerializer
            .Serialize(Definition)
            .AsSpan()
            .SequenceEqual(_savedSnapshot);

    public event Action? Changed;

    public static TilemapDocument Open(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);

        if (!fullPath.EndsWith(
                ".tilemap.json",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The file must have a .tilemap.json extension.",
                nameof(path));
        }

        var definition =
            JsonTilemapDefinitionSerializer.Deserialize(
                new AssetPath(Path.GetFileName(fullPath)),
                File.ReadAllBytes(fullPath));

        return new TilemapDocument(fullPath, definition);
    }

    public void UpdateDefinition(TilemapDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var contents =
            JsonTilemapDefinitionSerializer.Serialize(definition);

        _ = JsonTilemapDefinitionSerializer.Deserialize(
            new AssetPath(Path.GetFileName(FilePath)),
            contents);

        if (contents.AsSpan().SequenceEqual(
                JsonTilemapDefinitionSerializer.Serialize(Definition)))
        {
            return;
        }

        _undoStack.Clear();
        _redoStack.Clear();

        Definition = definition;
        Changed?.Invoke();
    }

    public void SetTile(int x, int y, uint tileId)
    {
        ApplyTileStroke(
            new Dictionary<(int X, int Y), uint>
            {
                [(x, y)] = tileId
            });
    }

    public void ApplyTileStroke(
        IReadOnlyDictionary<(int X, int Y), uint> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);

        var current = Definition;
        var tiles = current.Tiles.ToArray();
        var changed = false;

        foreach (var pair in changes)
        {
            var (x, y) = pair.Key;

            // GetTile проверяет границы карты.
            var oldValue = current.GetTile(x, y);

            if (oldValue == pair.Value)
            {
                continue;
            }

            tiles[y * current.Width + x] = pair.Value;
            changed = true;
        }

        if (!changed)
        {
            return;
        }

        var updated = new TilemapDefinition(
            current.FormatVersion,
            current.TileSetPath,
            current.Width,
            current.Height,
            tiles);

        var contents =
            JsonTilemapDefinitionSerializer.Serialize(updated);

        _ = JsonTilemapDefinitionSerializer.Deserialize(
            new AssetPath(Path.GetFileName(FilePath)),
            contents);

        _undoStack.Push(current);
        _redoStack.Clear();

        Definition = updated;
        Changed?.Invoke();
    }

    public void Undo()
    {
        if (!CanUndo)
        {
            return;
        }

        _redoStack.Push(Definition);
        Definition = _undoStack.Pop();

        Changed?.Invoke();
    }

    public void Redo()
    {
        if (!CanRedo)
        {
            return;
        }

        _undoStack.Push(Definition);
        Definition = _redoStack.Pop();

        Changed?.Invoke();
    }

    public void Save()
    {
        var contents =
            JsonTilemapDefinitionSerializer.Serialize(Definition);

        _ = JsonTilemapDefinitionSerializer.Deserialize(
            new AssetPath(Path.GetFileName(FilePath)),
            contents);

        var directory = Path.GetDirectoryName(FilePath)
            ?? throw new InvalidOperationException(
                "Could not determine the Tilemap directory.");

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
            JsonTilemapDefinitionSerializer.Serialize(Definition);

        Changed?.Invoke();
    }
}