using Engine.Core.Commands;
using Engine.Core.Math;
using Engine.ECS.Entities;

namespace Game.Sandbox.Gameplay;

public readonly record struct PlayerMoveCommand(
    EntityId Player,
    FixedVector2 Direction) :
    ICommand;