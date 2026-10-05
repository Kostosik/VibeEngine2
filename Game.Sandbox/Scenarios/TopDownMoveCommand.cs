using Engine.Core.Commands;
using Engine.Core.Math;
using Engine.ECS.Entities;

namespace Game.Sandbox.Scenarios;

public readonly record struct TopDownMoveCommand(
    EntityId Entity,
    FixedVector2 Direction) : ICommand;