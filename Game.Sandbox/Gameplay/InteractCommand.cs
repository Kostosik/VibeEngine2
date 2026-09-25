using Engine.Core.Commands;
using Engine.ECS.Entities;

namespace Game.Sandbox.Gameplay;

public readonly record struct InteractCommand(
    EntityId Player,
    EntityId Target) :
    ICommand;