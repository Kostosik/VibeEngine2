using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.Serialization.Binary;

namespace Engine.Serialization.Types;

public sealed class WorldTransform2DSerializer :
    IBinarySerializer<WorldTransform2D>
{
    public void Serialize(
        ref SerializationWriter writer,
        WorldTransform2D value)
    {
        WriteFixedVector2(
            ref writer,
            value.Position);

        WriteFixed32(
            ref writer,
            value.Rotation);

        WriteFixedVector2(
            ref writer,
            value.Scale);
    }

    public WorldTransform2D Deserialize(
        ref SerializationReader reader)
    {
        var transform =
            new WorldTransform2D(
                ReadFixedVector2(
                    ref reader));

        transform.Rotation =
            ReadFixed32(
                ref reader);

        transform.Scale =
            ReadFixedVector2(
                ref reader);

        return transform;
    }

    private static void WriteFixed32(
        ref SerializationWriter writer,
        Fixed32 value)
    {
        writer.WriteInt32(
            value.RawValue);
    }

    private static Fixed32 ReadFixed32(
        ref SerializationReader reader)
    {
        return Fixed32.FromRatio(
            reader.ReadInt32(),
            1 << 16);
    }

    private static void WriteFixedVector2(
        ref SerializationWriter writer,
        FixedVector2 value)
    {
        WriteFixed32(
            ref writer,
            value.X);

        WriteFixed32(
            ref writer,
            value.Y);
    }

    private static FixedVector2 ReadFixedVector2(
        ref SerializationReader reader)
    {
        return new FixedVector2(
            ReadFixed32(
                ref reader),
            ReadFixed32(
                ref reader));
    }
}