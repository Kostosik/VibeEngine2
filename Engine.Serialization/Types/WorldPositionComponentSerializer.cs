using Engine.Serialization.Binary;
using Engine.Worlds.Spatial;

namespace Engine.Serialization.Types;

public sealed class WorldPositionComponentSerializer :
    IBinarySerializer<WorldPositionComponent>
{
    public void Serialize(
        ref SerializationWriter writer,
        WorldPositionComponent value)
    {
        writer.WriteInt32(
            value.Position.X);

        writer.WriteInt32(
            value.Position.Y);
    }

    public WorldPositionComponent Deserialize(
        ref SerializationReader reader)
    {
        return new WorldPositionComponent(
            new WorldPosition(
                reader.ReadInt32(),
                reader.ReadInt32()));
    }
}