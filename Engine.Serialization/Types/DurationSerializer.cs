using Engine.Core.Time;

namespace Engine.Serialization.Types;

public sealed class DurationSerializer :
    Binary.IBinarySerializer<Duration>
{
    public void Serialize(
        ref Binary.SerializationWriter writer,
        Duration value)
    {
        writer.WriteInt64(
            value.Value.Ticks);
    }

    public Duration Deserialize(
        ref Binary.SerializationReader reader)
    {
        return new Duration(
            new TimeSpan(
                reader.ReadInt64()));
    }
}