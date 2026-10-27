namespace Engine.Animation;

public sealed class AnimationClipFileService
{
    private readonly AnimationClipJsonSerializer _serializer;

    public AnimationClipFileService(
        AnimationClipJsonSerializer? serializer = null)
    {
        _serializer =
            serializer ?? new AnimationClipJsonSerializer();
    }

    public void Save(
        string path,
        AnimationClip clip)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(clip);

        var json = _serializer.Serialize(clip);

        File.WriteAllText(path, json);
    }

    public AnimationClip Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var json = File.ReadAllText(path);

        return _serializer.Deserialize(json);
    }
}