using System.Text;
using Engine.Animation;

namespace Engine.Editor.Documents;

public sealed class AnimationClipDocument : IEditorDocument
{
    public const string TypeId = "vibeengine.animation-clip";

    private readonly AnimationClipJsonSerializer _serializer;
    private string _savedSnapshot;

    private AnimationClipDocument(
        string filePath,
        AnimationClip clip,
        AnimationClipJsonSerializer serializer)
    {
        FilePath = filePath;
        Clip = clip;
        _serializer = serializer;
        _savedSnapshot = _serializer.Serialize(clip);
    }

    public string DocumentTypeId => TypeId;

    public string FilePath { get; }

    public string DisplayName =>
        Path.GetFileName(FilePath);

    public AnimationClip Clip { get; private set; }

    public bool IsDirty =>
        !string.Equals(
            _serializer.Serialize(Clip),
            _savedSnapshot,
            StringComparison.Ordinal);

    public event Action? Changed;

    public static AnimationClipDocument Open(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);

        if (!fullPath.EndsWith(
                ".animation.json",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The file must have a .animation.json extension.",
                nameof(path));
        }

        var serializer = new AnimationClipJsonSerializer();

        var clip = serializer.Deserialize(
            File.ReadAllText(fullPath));

        return new AnimationClipDocument(
            fullPath,
            clip,
            serializer);
    }

    public void UpdateClip(AnimationClip clip)
    {
        ArgumentNullException.ThrowIfNull(clip);

        var contents = _serializer.Serialize(clip);

        // Проверяем, что полученный ресурс можно восстановить.
        _ = _serializer.Deserialize(contents);

        if (string.Equals(
                contents,
                _serializer.Serialize(Clip),
                StringComparison.Ordinal))
        {
            return;
        }

        Clip = clip;
        Changed?.Invoke();
    }

    public void Save()
    {
        var contents = _serializer.Serialize(Clip);

        // Не записываем некорректный ресурс.
        _ = _serializer.Deserialize(contents);

        var directory = Path.GetDirectoryName(FilePath)
            ?? throw new InvalidOperationException(
                "Could not determine the animation directory.");

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
        _savedSnapshot = _serializer.Serialize(Clip);
        Changed?.Invoke();
    }
}