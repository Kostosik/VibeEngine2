using Engine.Audio.Components;
using Engine.Serialization.Binary;

namespace Engine.Serialization.Types;

public sealed class AudioSourceComponentSerializer
    : IBinarySerializer<AudioSourceComponent>
{
    public void Serialize(
        ref SerializationWriter writer,
        AudioSourceComponent value)
    {
        writer.WriteString(value.AudioPath);
        writer.WriteSingle(value.Gain);
        writer.WriteSingle(value.Pitch);
        writer.WriteBoolean(value.Looping);
        writer.WriteBoolean(value.PlayOnStart);
    }

    public AudioSourceComponent Deserialize(
        ref SerializationReader reader)
    {
        var audioPath = reader.ReadString() ?? string.Empty;
        var gain = reader.ReadSingle();
        var pitch = reader.ReadSingle();
        var looping = reader.ReadBoolean();
        var playOnStart = reader.ReadBoolean();

        return new AudioSourceComponent(
            audioPath,
            gain,
            pitch,
            looping,
            playOnStart);
    }
}