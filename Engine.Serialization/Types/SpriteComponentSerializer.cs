using Engine.Graphics2D.Sprites;
using Engine.Serialization.Binary;

namespace Engine.Serialization.Types;

public sealed class SpriteComponentSerializer :
    IBinarySerializer<SpriteComponent>
{
    public void Serialize(
        ref SerializationWriter writer,
        SpriteComponent value)
    {
        writer.WriteString(value.TexturePath);
        writer.WriteSingle(value.Width);
        writer.WriteSingle(value.Height);
        writer.WriteInt32(value.Layer);
    }

    public SpriteComponent Deserialize(
        ref SerializationReader reader)
    {
        var texturePath =
            reader.ReadString() ?? string.Empty;

        var width =
            reader.ReadSingle();

        var height =
            reader.ReadSingle();

        var layer =
            reader.ReadInt32();

        return new SpriteComponent(
            texturePath,
            width,
            height,
            layer);
    }
}