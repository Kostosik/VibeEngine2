using Engine.Core.Math;
using Engine.Core.Systems;

namespace Engine.Tests.Simulations;

internal sealed class MovementTestSystem
    : IFixedUpdateSystem
{
    public FixedVector2 Position { get; private set; }

    public FixedVector2 Velocity { get; set; }

    public void FixedUpdate(
        FixedSystemContext context)
    {
        Position +=
            Velocity *
            context.Time.Delta;
    }
}