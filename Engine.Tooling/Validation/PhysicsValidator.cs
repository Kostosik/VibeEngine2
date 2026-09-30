using Engine.ECS;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Physics.Components;

namespace Engine.Tooling.Validation;

public sealed class PhysicsValidator : IValidator
{
    private const string SimulatedBodyHasParentCode =
        "PHYSICS_SIMULATED_BODY_HAS_PARENT";

    private readonly World _world;

    public PhysicsValidator(
        World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        _world = world;
    }

    public ValidationResult Validate(
        ValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var result =
            new ValidationResult();

        ValidateParentedBodies(
            result);

        return result;
    }

    private void ValidateParentedBodies(
        ValidationResult result)
    {
        foreach (var entity in
                 _world.Inspector.GetEntities())
        {
            if (!_world.Has<PhysicsBody2D>(
                    entity) ||
                !_world.Has<TransformParent2D>(
                    entity))
            {
                continue;
            }

            var body =
                _world.Get<PhysicsBody2D>(
                    entity);

            if (body.BodyType != PhysicsBodyType.Dynamic &&
                body.BodyType != PhysicsBodyType.Kinematic)
            {
                continue;
            }

            result.Add(
                ValidationSeverity.Error,
                SimulatedBodyHasParentCode,
                $"Entity '{entity}' has a {body.BodyType} " +
                "PhysicsBody2D together with TransformParent2D. " +
                "WorldTransform2D is simulation-authoritative for " +
                "dynamic and kinematic bodies, so hierarchy and physics " +
                "would both attempt to control the world transform.");
        }
    }
}