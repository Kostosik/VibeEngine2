using System.Text;
using Engine.Graphics2D.Sprites;

namespace Engine.Editor.Documents;

public sealed class SpriteAnimationDocument : IEditorDocument
{
    public const string TypeId = "vibeengine.sprite-animation";

    private readonly SpriteAnimationDefinitionJsonSerializer _serializer;
    private string _savedSnapshot;

    private SpriteAnimationDocument(
        string filePath,
        SpriteAnimationDefinition definition,
        SpriteAnimationDefinitionJsonSerializer serializer)
    {
        FilePath = filePath;
        Definition = definition;
        _serializer = serializer;
        _savedSnapshot = _serializer.Serialize(definition);
    }

    public string DocumentTypeId => TypeId;

    public string FilePath { get; }

    public string DisplayName => Path.GetFileName(FilePath);

    public SpriteAnimationDefinition Definition { get; private set; }

    public bool IsDirty =>
        !string.Equals(
            _serializer.Serialize(Definition),
            _savedSnapshot,
            StringComparison.Ordinal);

    public event Action? Changed;

    public static SpriteAnimationDocument Open(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);

        if (!fullPath.EndsWith(
                ".sprite-animation.json",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The file must have a .sprite-animation.json extension.",
                nameof(path));
        }

        var serializer = new SpriteAnimationDefinitionJsonSerializer();

        var definition = serializer.Deserialize(
            File.ReadAllText(fullPath));

        return new SpriteAnimationDocument(
            fullPath,
            definition,
            serializer);
    }

    public void UpdateDefinition(
        SpriteAnimationDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var contents = _serializer.Serialize(definition);

        // Убеждаемся, что изменённый ресурс можно десериализовать.
        _ = _serializer.Deserialize(contents);

        if (string.Equals(
                contents,
                _serializer.Serialize(Definition),
                StringComparison.Ordinal))
        {
            return;
        }

        Definition = definition;
        Changed?.Invoke();
    }

    public void Save()
    {
        var contents = _serializer.Serialize(Definition);

        _ = _serializer.Deserialize(contents);

        var directory = Path.GetDirectoryName(FilePath)
            ?? throw new InvalidOperationException(
                "Could not determine the sprite animation directory.");

        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(FilePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(
                temporaryPath,
                contents,
                new UTF8Encoding(false));

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
        _savedSnapshot = _serializer.Serialize(Definition);
        Changed?.Invoke();
    }
}