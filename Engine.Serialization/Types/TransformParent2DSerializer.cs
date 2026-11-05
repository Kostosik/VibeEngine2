using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Serialization.Binary;

namespace Engine.Serialization.Types;

public sealed class TransformParent2DSerializer :
    IBinarySerializer<TransformParent2D>
{
    public void Serialize(
        ref SerializationWriter writer,
        TransformParent2D value)
    {
        writer.WriteUInt32(
            value.Parent.Index);

        writer.WriteUInt32(
            value.Parent.Generation);
    }

    public TransformParent2D Deserialize(
        ref SerializationReader reader)
    {
        var parent = new EntityId(
            reader.ReadUInt32(),
            reader.ReadUInt32());

        return new TransformParent2D(parent);
    }
}