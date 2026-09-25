using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.Physics.Components;
using Engine.Physics.Materials;

namespace Engine.Physics.Collision;

public sealed class CollisionResolver2D
{
    public void ResolvePosition(
        World world,
        CollisionManifold manifold,
        Fixed32 penetrationSlop,
        Fixed32 correctionPercent)
    {
        var firstEntity =
            manifold.Pair.First;

        var secondEntity =
            manifold.Pair.Second;

        if (!world.Exists(firstEntity) ||
            !world.Exists(secondEntity))
        {
            return;
        }

        ref var firstBody =
            ref world.Get<PhysicsBody2D>(
                firstEntity);

        ref var secondBody =
            ref world.Get<PhysicsBody2D>(
                secondEntity);

        ref var firstTransform =
            ref world.Get<Transform2D>(
                firstEntity);

        ref var secondTransform =
            ref world.Get<Transform2D>(
                secondEntity);

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

        var error =
            manifold.Contact.Penetration -
            penetrationSlop;

        if (error <= Fixed32.Zero)
        {
            return;
        }

        var correction =
            error *
            correctionPercent /
            inverseMassSum;

        var firstCorrection =
            manifold.Contact.Normal *
            (correction * firstInverseMass);

        var secondCorrection =
            manifold.Contact.Normal *
            (correction * secondInverseMass);

        firstTransform.Position -=
            firstCorrection;

        secondTransform.Position +=
            secondCorrection;
    }

    public void ResolveVelocity(
        World world,
        CollisionManifold manifold)
    {
        var firstEntity =
            manifold.Pair.First;

        var secondEntity =
            manifold.Pair.Second;

        if (!world.Exists(firstEntity) ||
            !world.Exists(secondEntity))
        {
            return;
        }

        ref var firstBody =
            ref world.Get<PhysicsBody2D>(
                firstEntity);

        ref var secondBody =
            ref world.Get<PhysicsBody2D>(
                secondEntity);

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

        var relativeVelocity =
            secondBody.Velocity -
            firstBody.Velocity;

        var velocityAlongNormal =
            relativeVelocity.Dot(
                manifold.Contact.Normal);

        if (velocityAlongNormal > Fixed32.Zero)
        {
            return;
        }

        var material =
            GetCombinedMaterial(
                world,
                manifold.Pair);

        var impulseMagnitude =
            -(
                Fixed32.One +
                material.Restitution) *
            velocityAlongNormal /
            inverseMassSum;

        var normalImpulse =
            manifold.Contact.Normal *
            impulseMagnitude;

        firstBody.Velocity -=
            normalImpulse *
            firstInverseMass;

        secondBody.Velocity +=
            normalImpulse *
            secondInverseMass;

        ResolveFriction(
            ref firstBody,
            ref secondBody,
            relativeVelocity,
            manifold.Contact.Normal,
            impulseMagnitude,
            firstInverseMass,
            secondInverseMass,
            inverseMassSum,
            material.Friction);
    }

    private static void ResolveFriction(
        ref PhysicsBody2D firstBody,
        ref PhysicsBody2D secondBody,
        FixedVector2 relativeVelocity,
        FixedVector2 normal,
        Fixed32 normalImpulse,
        Fixed32 firstInverseMass,
        Fixed32 secondInverseMass,
        Fixed32 inverseMassSum,
        Fixed32 friction)
    {
        var tangentVelocity =
            relativeVelocity -
            normal *
            relativeVelocity.Dot(normal);

        var tangentLength =
            tangentVelocity.Length();

        if (tangentLength == Fixed32.Zero)
        {
            return;
        }

        var tangent =
            tangentVelocity /
            tangentLength;

        var tangentImpulse =
            -relativeVelocity.Dot(tangent) /
            inverseMassSum;

        var maxFriction =
            normalImpulse *
            friction;

        tangentImpulse =
            Fixed32.Clamp(
                tangentImpulse,
                -maxFriction,
                maxFriction);

        var frictionImpulse =
            tangent *
            tangentImpulse;

        firstBody.Velocity -=
            frictionImpulse *
            firstInverseMass;

        secondBody.Velocity +=
            frictionImpulse *
            secondInverseMass;
    }

    private static PhysicsMaterial2D GetCombinedMaterial(
        World world,
        CollisionPair pair)
    {
        ref var firstCollider =
            ref world.Get<Collider2D>(
                pair.First);

        ref var secondCollider =
            ref world.Get<Collider2D>(
                pair.Second);

        var friction =
            Fixed32.Sqrt(
                firstCollider.Material.Friction *
                secondCollider.Material.Friction);

        var restitution =
            Fixed32.Min(
                firstCollider.Material.Restitution,
                secondCollider.Material.Restitution);

        return new PhysicsMaterial2D(
            friction,
            restitution);
    }
}