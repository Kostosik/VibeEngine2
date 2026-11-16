using Engine.Core.Math;

namespace Engine.Input;

public interface IPointerInput
{
    Vector2 Position { get; }
    Vector2 RelativeDelta { get; }
    float ScrollDelta { get; }
    bool IsDown(InputMouseButton button);

    bool IsPressed(InputMouseButton button);

    bool IsReleased(InputMouseButton button);
}