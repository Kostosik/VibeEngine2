using Engine.Audio.Data;
using Engine.Core.Assets;

namespace Engine.Audio.Loading;

public sealed class AudioLoader
{
    public AudioData Load(
        IAssetSource source,
        AssetPath path)
    {
        ArgumentNullException.ThrowIfNull(source);

        var extension = Path.GetExtension(path.Value);

        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new InvalidDataException(
                $"Audio asset '{path}' has no file extension.");
        }

        var data = source.Load(path);

        if (data.IsEmpty)
        {
            throw new InvalidDataException(
                $"Audio asset '{path}' is empty.");
        }

        return extension.ToLowerInvariant() switch
        {
            ".wav" => WavAudioDecoder.Decode(data.Span, path),

            _ => throw new NotSupportedException(
                $"Audio format '{extension}' is not supported.")
        };
    }
}