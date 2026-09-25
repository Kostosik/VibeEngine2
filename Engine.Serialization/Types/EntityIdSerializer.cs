using Engine.ECS.Entities;

namespace Engine.Serialization.Types;

public sealed class EntityIdSerializer :
    Binary.IBinarySerializer<EntityId>
{
    public void Serialize(
        ref Binary.SerializationWriter writer,
        EntityId value)
    {
        writer.WriteUInt32(
            value.Index);

        writer.WriteUInt32(
            value.Generation);
    }

    public EntityId Deserialize(
        ref Binary.SerializationReader reader)
    {
        return new EntityId(
            reader.ReadUInt32(),
            reader.ReadUInt32());
    }
}