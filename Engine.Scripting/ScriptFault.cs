using Engine.ECS.Entities;

namespace Engine.Scripting;

public enum ScriptExecutionPhase
{
    Update,
    FixedUpdate,
    Event,
    Detach,
    Attach
}

public readonly record struct ScriptFault(
    EntityId Entity,
    string ScriptId,
    ScriptExecutionPhase Phase,
    Exception Exception);