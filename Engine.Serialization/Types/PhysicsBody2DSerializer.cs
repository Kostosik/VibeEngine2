using Engine.Core.Math;
using Engine.Physics.Components;
using Engine.Serialization.Binary;

namespace Engine.Serialization.Types;

public sealed class PhysicsBody2DSerializer :
    IBinarySerializer<PhysicsBody2D>
{
    public void Serialize(
        ref SerializationWriter writer,
        PhysicsBody2D value)
    {
        writer.WriteInt32(
            (int)value.BodyType);

        WriteFixedVector2(
            ref writer,
            value.Velocity);

        WriteFixedVector2(
            ref writer,
            value.Force);

        WriteFixed32(
            ref writer,
            value.Mass);

        WriteFixed32(
            ref writer,
            value.Inertia);

        WriteFixed32(
            ref writer,
            value.GravityScale);

        WriteFixed32(
            ref writer,
            value.AngularVelocity);

        WriteFixed32(
            ref writer,
            value.Torque);

        WriteFixed32(
            ref writer,
            value.LinearDamping);

        WriteFixed32(
            ref writer,
            value.AngularDamping);

        writer.WriteBoolean(
            value.IsSleeping);

        WriteFixed32(
            ref writer,
            value.SleepTimer);
    }

    public PhysicsBody2D Deserialize(
        ref SerializationReader reader)
    {
        var bodyType =
            ReadBodyType(
                ref reader);

        var velocity =
            ReadFixedVector2(
                ref reader);

        var force =
            ReadFixedVector2(
                ref reader);

        var mass =
            ReadFixed32(
                ref reader);

        var inertia =
            ReadFixed32(
                ref reader);

        var gravityScale =
            ReadFixed32(
                ref reader);

        var angularVelocity =
            ReadFixed32(
                ref reader);

        var torque =
            ReadFixed32(
                ref reader);

        var linearDamping =
            ReadFixed32(
                ref reader);

        var angularDamping =
            ReadFixed32(
                ref reader);

        var sleeping =
            reader.ReadBoolean();

        var sleepTimer =
            ReadFixed32(
                ref reader);

        var body =
            new PhysicsBody2D(
                bodyType,
                mass);

        body.SetInertia(
            inertia);

        body.Velocity =
            velocity;

        body.GravityScale =
            gravityScale;

        body.AngularVelocity =
            angularVelocity;

        body.LinearDamping =
            linearDamping;

        body.AngularDamping =
            angularDamping;

        body.AddForce(
            force);

        body.AddTorque(
            torque);

        body.IsSleeping =
            sleeping;

        body.SleepTimer =
            sleepTimer;

        return body;
    }

    private static PhysicsBodyType ReadBodyType(
        ref SerializationReader reader)
    {
        var value =
            reader.ReadInt32();

        return value switch
        {
            (int)PhysicsBodyType.Static =>
                PhysicsBodyType.Static,

            (int)PhysicsBodyType.Dynamic =>
                PhysicsBodyType.Dynamic,

            (int)PhysicsBodyType.Kinematic =>
                PhysicsBodyType.Kinematic,

            _ =>
                throw new InvalidDataException(
                    $"Invalid physics body type '{value}'.")
        };
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