using Engine.Core.Math;

namespace Game.Sandbox.Gameplay;

public struct InteractionTarget
{
    public InteractionTarget(
        Fixed32 radius)
    {
        if (radius <= Fixed32.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(radius));
        }

        Radius = radius;
        IsActivated = false;
    }

    public Fixed32 Radius { get; }

    public bool IsActivated { get; set; }
}