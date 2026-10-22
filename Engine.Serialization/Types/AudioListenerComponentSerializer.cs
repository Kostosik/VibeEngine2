using Engine.Audio.Components;
using Engine.Serialization.Binary;

namespace Engine.Serialization.Types;

public sealed class AudioListenerComponentSerializer
    : IBinarySerializer<AudioListenerComponent>
{
    public void Serialize(
        ref SerializationWriter writer,
        AudioListenerComponent value)
    {
        writer.WriteSingle(value.Gain);
        writer.WriteBoolean(value.Enabled);
    }

    public AudioListenerComponent Deserialize(
        ref SerializationReader reader)
    {
        var gain = reader.ReadSingle();
        var enabled = reader.ReadBoolean();

        return new AudioListenerComponent(gain, enabled);
    }
}