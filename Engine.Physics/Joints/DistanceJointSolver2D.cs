using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.Physics.Components;

namespace Engine.Physics.Joints;

public sealed class DistanceJointSolver2D
{
    public void SolvePosition(
        World world,
        DistanceJoint2D joint,
        Fixed32 correctionPercent)
    {
        if (!joint.Enabled ||
            !world.Exists(joint.First) ||
            !world.Exists(joint.Second))
        {
            return;
        }

        if (!world.Has<PhysicsBody2D>(
                joint.First) ||
            !world.Has<PhysicsBody2D>(
                joint.Second) ||
            !world.Has<WorldTransform2D>(
                joint.First) ||
            !world.Has<WorldTransform2D>(
                joint.Second))
        {
            return;
        }

        ref var firstBody =
            ref world.Get<PhysicsBody2D>(
                joint.First);

        ref var secondBody =
            ref world.Get<PhysicsBody2D>(
                joint.Second);

        ref var firstTransform =
            ref world.Get<WorldTransform2D>(
                joint.First);

        ref var secondTransform =
            ref world.Get<WorldTransform2D>(
                joint.Second);

        var firstInverseMass =
            firstBody.InverseMass;

        var secondInverseMass =
            secondBody.InverseMass;

        var inverseMassSum =
            firstInverseMass +
            secondInverseMass;

        if (inverseMassSum == Fixed32.Zero)
        {
            return;
        }

        var delta =
            secondTransform.Position -
            firstTransform.Position;

        var distance =
            delta.Length();

        FixedVector2 normal;

        if (distance == Fixed32.Zero)
        {
            normal =
                new FixedVector2(
                    Fixed32.One,
                    Fixed32.Zero);
        }
        else
        {
            normal =
                delta /
                distance;
        }

        var error =
            distance -
            joint.Length;

        var correction =
            error *
            correctionPercent /
            inverseMassSum;

        firstTransform.Position +=
            normal *
            (correction * firstInverseMass);

        secondTransform.Position -=
            normal *
            (correction * secondInverseMass);
    }

    public void SolveVelocity(
        World world,
        DistanceJoint2D joint)
    {
        if (!joint.Enabled ||
            !world.Exists(joint.First) ||
            !world.Exists(joint.Second))
        {
            return;
        }

        if (!world.Has<PhysicsBody2D>(
                joint.First) ||
            !world.Has<PhysicsBody2D>(
                joint.Second) ||
            !world.Has<WorldTransform2D>(
                joint.First) ||
            !world.Has<WorldTransform2D>(
                joint.Second))
        {
            return;
        }

        ref var firstBody =
            ref world.Get<PhysicsBody2D>(
                joint.First);

        ref var secondBody =
            ref world.Get<PhysicsBody2D>(
                joint.Second);

        var firstTransform =
            world.Get<WorldTransform2D>(
                joint.First);

        var secondTransform =
            world.Get<WorldTransform2D>(
                joint.Second);

        var inverseMassSum =
            firstBody.InverseMass +
            secondBody.InverseMass;

        if (inverseMassSum == Fixed32.Zero)
        {
            return;
        }

        var delta =
            secondTransform.Position -
            firstTransform.Position;

        var distance =
            delta.Length();

        if (distance == Fixed32.Zero)
        {
            return;
        }

        var normal =
            delta /
            distance;

        var relativeVelocity =
            secondBody.Velocity -
            firstBody.Velocity;

        var velocityAlongNormal =
            relativeVelocity.Dot(
                normal);

        var impulseMagnitude =
            -velocityAlongNormal /
            inverseMassSum;

        var impulse =
            normal *
            impulseMagnitude;

        firstBody.Velocity -=
            impulse *
            firstBody.InverseMass;

        secondBody.Velocity +=
            impulse *
            secondBody.InverseMass;
    }
}