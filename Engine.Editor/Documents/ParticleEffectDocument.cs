
using Engine.Core.Assets;
using Engine.Particles;
using Engine.Serialization.Content;

namespace Engine.Editor.Documents;

public sealed class ParticleEffectDocument : IEditorDocument
{
    public const string TypeId = "vibeengine.particle-effect";

    private byte[] _savedSnapshot;

    private ParticleEffectDocument(
        string filePath,
        ParticleEffectDefinition definition)
    {
        FilePath = filePath;
        Definition = definition;
        _savedSnapshot =
            JsonParticleEffectDefinitionSerializer.Serialize(definition);
    }

    public string DocumentTypeId => TypeId;

    public string FilePath { get; }

    public string DisplayName =>
        Path.GetFileName(FilePath);

    public ParticleEffectDefinition Definition { get; private set; }

    public bool IsDirty =>
        !JsonParticleEffectDefinitionSerializer
            .Serialize(Definition)
            .AsSpan()
            .SequenceEqual(_savedSnapshot);

    public event Action? Changed;

    public static ParticleEffectDocument Open(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);

        if (!fullPath.EndsWith(
                ".particle.json",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The file must have a .particle.json extension.",
                nameof(path));
        }

        var contents = File.ReadAllBytes(fullPath);

        var definition =
            JsonParticleEffectDefinitionSerializer.Deserialize(
                GetAssetPath(fullPath),
                contents);

        return new ParticleEffectDocument(
            fullPath,
            definition);
    }

    public void UpdateDefinition(
        ParticleEffectDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var contents =
            JsonParticleEffectDefinitionSerializer.Serialize(
                definition);

        // Проверяем, что изменённое определение можно прочитать
        // тем же парсером, который используется для загрузки ресурса.
        _ = JsonParticleEffectDefinitionSerializer.Deserialize(
            GetAssetPath(FilePath),
            contents);

        if (contents.AsSpan().SequenceEqual(
                JsonParticleEffectDefinitionSerializer
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
            JsonParticleEffectDefinitionSerializer.Serialize(
                Definition);

        _ = JsonParticleEffectDefinitionSerializer.Deserialize(
            GetAssetPath(FilePath),
            contents);

        var directory = Path.GetDirectoryName(FilePath)
            ?? throw new InvalidOperationException(
                "Could not determine the particle effect directory.");

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
            // Не оставляем временный файл, если запись или замена
            // исходного файла завершилась ошибкой.
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
            JsonParticleEffectDefinitionSerializer.Serialize(
                Definition);

        Changed?.Invoke();
    }

    private static AssetPath GetAssetPath(string fullPath) =>
        new(Path.GetFileName(fullPath));
}
