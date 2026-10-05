using Engine.Core.Math;
using Engine.ECS.Entities;
using Engine.Physics.Joints;
using Engine.Serialization.Binary;

namespace Engine.Serialization.Types;

public sealed class DistanceJoint2DSerializer :
    IBinarySerializer<DistanceJoint2D>
{
    public void Serialize(
        ref SerializationWriter writer,
        DistanceJoint2D value)
    {
        writer.WriteUInt32(
            value.First.Index);

        writer.WriteUInt32(
            value.First.Generation);

        writer.WriteUInt32(
            value.Second.Index);

        writer.WriteUInt32(
            value.Second.Generation);

        writer.WriteInt32(
            value.Length.RawValue);

        writer.WriteBoolean(
            value.Enabled);
    }

    public DistanceJoint2D Deserialize(
        ref SerializationReader reader)
    {
        var first =
            new EntityId(
                reader.ReadUInt32(),
                reader.ReadUInt32());

        var second =
            new EntityId(
                reader.ReadUInt32(),
                reader.ReadUInt32());

        var length =
            Fixed32.FromRatio(
                reader.ReadInt32(),
                1 << 16);

        var enabled =
            reader.ReadBoolean();

        var joint =
            new DistanceJoint2D(
                first,
                second,
                length);

        joint.Enabled =
            enabled;

        return joint;
    }
}