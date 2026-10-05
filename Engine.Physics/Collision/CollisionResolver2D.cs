using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.Physics.Components;
using Engine.Physics.Materials;
using Engine.Physics.Shapes;

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
            ref world.Get<WorldTransform2D>(
                firstEntity);

        ref var secondTransform =
            ref world.Get<WorldTransform2D>(
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

        var firstTransform =
            world.Get<WorldTransform2D>(
                firstEntity);

        var secondTransform =
            world.Get<WorldTransform2D>(
                secondEntity);

        var firstInverseMass =
            firstBody.InverseMass;

        var secondInverseMass =
            secondBody.InverseMass;

        var firstInverseInertia =
            firstBody.InverseInertia;

        var secondInverseInertia =
            secondBody.InverseInertia;

        var normal =
            manifold.Contact.Normal;

        var firstRadius =
            manifold.Contact.Position -
            firstTransform.Position;

        var secondRadius =
            manifold.Contact.Position -
            secondTransform.Position;

        var firstPointVelocity =
            firstBody.Velocity +
            Cross(
                firstBody.AngularVelocity,
                firstRadius);

        var secondPointVelocity =
            secondBody.Velocity +
            Cross(
                secondBody.AngularVelocity,
                secondRadius);

        var relativeVelocity =
            secondPointVelocity -
            firstPointVelocity;

        var velocityAlongNormal =
            relativeVelocity.Dot(
                normal);

        var firstShape =
            world.Get<Collider2D>(
                firstEntity)
                .Shape
                .Type;

        var secondShape =
            world.Get<Collider2D>(
                secondEntity)
                .Shape
                .Type;

        var isCircleAabb =
            (firstShape == PhysicsShapeType.Circle &&
             secondShape == PhysicsShapeType.Aabb) ||
            (firstShape == PhysicsShapeType.Aabb &&
             secondShape == PhysicsShapeType.Circle);

        if (velocityAlongNormal > Fixed32.Zero)
        {
            return;
        }

        var firstRadiusCrossNormal =
            Cross(
                firstRadius,
                normal);

        var secondRadiusCrossNormal =
            Cross(
                secondRadius,
                normal);

        var denominator =
            firstInverseMass +
            secondInverseMass +
            firstRadiusCrossNormal *
            firstRadiusCrossNormal *
            firstInverseInertia +
            secondRadiusCrossNormal *
            secondRadiusCrossNormal *
            secondInverseInertia;

        if (denominator == Fixed32.Zero)
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
            denominator;

        var normalImpulse =
            normal *
            impulseMagnitude;

        firstBody.Velocity -=
            normalImpulse *
            firstInverseMass;

        secondBody.Velocity +=
            normalImpulse *
            secondInverseMass;

        firstBody.AngularVelocity -=
            firstRadiusCrossNormal *
            impulseMagnitude *
            firstInverseInertia;

        secondBody.AngularVelocity +=
            secondRadiusCrossNormal *
            impulseMagnitude *
            secondInverseInertia;

        ResolveFriction(
            ref firstBody,
            ref secondBody,
            firstRadius,
            secondRadius,
            normal,
            impulseMagnitude,
            firstInverseMass,
            secondInverseMass,
            firstInverseInertia,
            secondInverseInertia,
            material.Friction);
    }

    private static void ResolveFriction(
    ref PhysicsBody2D firstBody,
    ref PhysicsBody2D secondBody,
    FixedVector2 firstRadius,
    FixedVector2 secondRadius,
    FixedVector2 normal,
    Fixed32 normalImpulse,
    Fixed32 firstInverseMass,
    Fixed32 secondInverseMass,
    Fixed32 firstInverseInertia,
    Fixed32 secondInverseInertia,
    Fixed32 friction)
    {
        var firstPointVelocity =
            firstBody.Velocity +
            Cross(
                firstBody.AngularVelocity,
                firstRadius);

        var secondPointVelocity =
            secondBody.Velocity +
            Cross(
                secondBody.AngularVelocity,
                secondRadius);

        var relativeVelocity =
            secondPointVelocity -
            firstPointVelocity;

        var tangentVelocity =
            relativeVelocity -
            normal *
            relativeVelocity.Dot(
                normal);

        var tangentLength =
            tangentVelocity.Length();

        if (tangentLength == Fixed32.Zero)
        {
            return;
        }

        var tangent =
            tangentVelocity /
            tangentLength;

        var firstRadiusCrossTangent =
            Cross(
                firstRadius,
                tangent);

        var secondRadiusCrossTangent =
            Cross(
                secondRadius,
                tangent);

        var denominator =
            firstInverseMass +
            secondInverseMass +
            firstRadiusCrossTangent *
            firstRadiusCrossTangent *
            firstInverseInertia +
            secondRadiusCrossTangent *
            secondRadiusCrossTangent *
            secondInverseInertia;

        if (denominator == Fixed32.Zero)
        {
            return;
        }

        var tangentImpulse =
            -relativeVelocity.Dot(
                tangent) /
            denominator;

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

        firstBody.AngularVelocity -=
            firstRadiusCrossTangent *
            tangentImpulse *
            firstInverseInertia;

        secondBody.AngularVelocity +=
            secondRadiusCrossTangent *
            tangentImpulse *
            secondInverseInertia;
    }

    private static Fixed32 Cross(
    FixedVector2 left,
    FixedVector2 right)
    {
        return
            left.X * right.Y -
            left.Y * right.X;
    }

    private static FixedVector2 Cross(
        Fixed32 scalar,
        FixedVector2 vector)
    {
        return new FixedVector2(
            -scalar * vector.Y,
            scalar * vector.X);
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