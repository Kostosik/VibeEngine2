using Engine.Core.Math;
using Engine.Physics.Components;
using Engine.Physics.Materials;
using Engine.Physics.Shapes;
using Engine.Serialization.Binary;

namespace Engine.Serialization.Types;

public sealed class Collider2DSerializer :
    IBinarySerializer<Collider2D>
{
    public void Serialize(
        ref SerializationWriter writer,
        Collider2D value)
    {
        writer.WriteInt32(
            (int)value.Shape.Type);

        switch (value.Shape.Type)
        {
            case PhysicsShapeType.Aabb:
                WriteFixedVector2(
                    ref writer,
                    value.Shape.Aabb.Size);
                break;

            case PhysicsShapeType.Circle:
                WriteFixed32(
                    ref writer,
                    value.Shape.Circle.Radius);
                break;

            case PhysicsShapeType.Polygon:
                {
                    var polygon =
                        value.Shape.Polygon;

                    writer.WriteInt32(
                        polygon.VertexCount);

                    for (var i = 0;
                         i < polygon.VertexCount;
                         i++)
                    {
                        WriteFixedVector2(
                            ref writer,
                            polygon.GetVertex(i));
                    }

                    break;
                }

            default:
                throw new InvalidOperationException(
                    $"Unsupported physics shape type '{value.Shape.Type}'.");
        }

        WriteFixedVector2(
            ref writer,
            value.Offset);

        WriteFixed32(
            ref writer,
            value.Material.Friction);

        WriteFixed32(
            ref writer,
            value.Material.Restitution);

        writer.WriteBoolean(
            value.IsTrigger);

        writer.WriteBoolean(
            value.Enabled);

        writer.WriteUInt32(
            value.CollisionLayer);

        writer.WriteUInt32(
            value.CollisionMask);
    }

    public Collider2D Deserialize(
    ref SerializationReader reader)
    {
        var shapeType =
            (PhysicsShapeType)reader.ReadInt32();

        Collider2D collider;

        switch (shapeType)
        {
            case PhysicsShapeType.Aabb:
                collider =
                    new Collider2D(
                        new AabbShape2D(
                            ReadFixedVector2(
                                ref reader)));
                break;

            case PhysicsShapeType.Circle:
                collider =
                    new Collider2D(
                        new CircleShape2D(
                            ReadFixed32(
                                ref reader)));
                break;

            case PhysicsShapeType.Polygon:
                collider =
                    new Collider2D(
                        ReadPolygonShape(
                            ref reader));
                break;

            default:
                throw new InvalidDataException(
                    $"Invalid physics shape type '{shapeType}'.");
        }

        collider.Offset =
            ReadFixedVector2(
                ref reader);

        collider.Material =
            new PhysicsMaterial2D(
                ReadFixed32(
                    ref reader),
                ReadFixed32(
                    ref reader));

        collider.IsTrigger =
            reader.ReadBoolean();

        collider.Enabled =
            reader.ReadBoolean();

        collider.CollisionLayer =
            reader.ReadUInt32();

        collider.CollisionMask =
            reader.ReadUInt32();

        return collider;
    }

    private static PolygonShape2D ReadPolygonShape(
    ref SerializationReader reader)
    {
        var count =
            reader.ReadInt32();

        if (count < 3)
        {
            throw new InvalidDataException(
                "Serialized polygon must contain at least three vertices.");
        }

        if (count >
            reader.Context.MaxCollectionLength)
        {
            throw new InvalidDataException(
                $"Serialized polygon vertex count '{count}' exceeds " +
                $"the maximum allowed length '{reader.Context.MaxCollectionLength}'.");
        }

        var vertices =
            new FixedVector2[count];

        for (var i = 0;
             i < count;
             i++)
        {
            vertices[i] =
                ReadFixedVector2(
                    ref reader);
        }

        return new PolygonShape2D(
            vertices);
    }

    private static PhysicsShape2D ReadPolygon(
        ref SerializationReader reader)
    {
        var count =
            reader.ReadInt32();

        if (count < 3)
        {
            throw new InvalidDataException(
                "Serialized polygon must contain at least three vertices.");
        }

        if (count >
            reader.Context.MaxCollectionLength)
        {
            throw new InvalidDataException(
                $"Serialized polygon vertex count '{count}' exceeds " +
                $"the maximum allowed length '{reader.Context.MaxCollectionLength}'.");
        }

        var vertices =
            new FixedVector2[count];

        for (var i = 0;
             i < count;
             i++)
        {
            vertices[i] =
                ReadFixedVector2(
                    ref reader);
        }

        return PhysicsShape2D.FromPolygon(
            new PolygonShape2D(
                vertices));
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