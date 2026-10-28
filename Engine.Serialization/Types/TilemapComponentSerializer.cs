using Engine.Graphics.Tilemaps;
using Engine.Serialization.Binary;

namespace Engine.Serialization.Types;

public sealed class TilemapComponentSerializer
    : IBinarySerializer<TilemapComponent>
{
    public void Serialize(
        ref SerializationWriter writer,
        TilemapComponent value)
    {
        writer.WriteString(
            value.TilemapPath ?? string.Empty);

        writer.WriteInt32(value.Layer);
    }

    public TilemapComponent Deserialize(
        ref SerializationReader reader)
    {
        var tilemapPath =
            reader.ReadString() ?? string.Empty;

        var layer = reader.ReadInt32();

        return new TilemapComponent(
            tilemapPath,
            layer);
    }
}