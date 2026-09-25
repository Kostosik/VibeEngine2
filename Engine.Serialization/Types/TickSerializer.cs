using Engine.Core.Time;

namespace Engine.Serialization.Types;

public sealed class TickSerializer :
    Binary.IBinarySerializer<Tick>
{
    public void Serialize(
        ref Binary.SerializationWriter writer,
        Tick value)
    {
        writer.WriteUInt64(
            value.Value);
    }

    public Tick Deserialize(
        ref Binary.SerializationReader reader)
    {
        return new Tick(
            reader.ReadUInt64());
    }
}