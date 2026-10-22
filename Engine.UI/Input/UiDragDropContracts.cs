using Engine.Core.Math;
using Engine.UI.Core;

namespace Engine.UI.Input;

public abstract record UiDragPayload;

public sealed record UiDragPayload<T>(
    T Value) : UiDragPayload;

public enum UiDragEndReason
{
    Dropped,
    Rejected,
    Cancelled
}

public sealed record UiDragContext(
    UiDragPayload Payload,
    UiWidget Source,
    UiWidget? Target,
    Vector2 StartPosition,
    Vector2 Position);

public interface IUiDragSource
{
    UiDragPayload? CreateDragPayload(
        Vector2 startPosition);

    void OnDragStarted(
        UiDragContext context)
    {
    }

    void OnDragMoved(
        UiDragContext context)
    {
    }

    void OnDragEnded(
        UiDragContext context,
        UiDragEndReason reason)
    {
    }
}

public interface IUiDropTarget
{
    bool CanAcceptDrop(
        UiDragPayload payload);

    void OnDragEnter(
        UiDragContext context)
    {
    }

    void OnDragOver(
        UiDragContext context)
    {
    }

    void OnDragLeave(
        UiDragContext context)
    {
    }

    bool OnDrop(
        UiDragContext context);
}