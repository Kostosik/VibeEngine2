
using Engine.AI.Perception.Core;
using Engine.Core.Math;
using Engine.ECS.Entities;

namespace Engine.AI.Perception.Sensors;

/// <summary>
/// Returns true when the target is visible from the observer.
/// The implementation may perform a raycast or another obstruction check.
/// </summary>
public delegate bool PerceptionVisibilityTest<TComponent>(
    in PerceptionContext context,
    EntityId target,
    TComponent component,
    Vector2 observerPosition,
    Vector2 targetPosition)
    where TComponent : struct;
