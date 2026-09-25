using Engine.Input.Cursors;
using Silk.NET.Input;

namespace Engine.Input.SilkNet;

public sealed class SilkNetCursorService :
    ICursorService
{
    private readonly ICursor _cursor;

    public SilkNetCursorService(
        ICursor cursor)
    {
        ArgumentNullException.ThrowIfNull(
            cursor);

        _cursor =
            cursor;

        Current =
            CursorShape.Default;
    }

    public CursorShape Current { get; private set; }

    public void Set(
        CursorShape shape)
    {
        var standardCursor =
            MapCursor(
                shape);

        if (!_cursor.IsSupported(
                standardCursor))
        {
            standardCursor =
                StandardCursor.Arrow;
        }

        if (!_cursor.IsSupported(
                standardCursor))
        {
            return;
        }

        _cursor.Type =
            CursorType.Standard;

        _cursor.StandardCursor =
            standardCursor;

        Current =
            shape;
    }

    private static StandardCursor MapCursor(
        CursorShape shape)
    {
        return shape switch
        {
            CursorShape.Default =>
                StandardCursor.Default,

            CursorShape.Hand =>
                StandardCursor.Hand,

            CursorShape.Text =>
                StandardCursor.IBeam,

            CursorShape.ResizeHorizontal =>
                StandardCursor.HResize,

            CursorShape.ResizeVertical =>
                StandardCursor.VResize,

            CursorShape.ResizeDiagonal =>
                StandardCursor.NwseResize,

            CursorShape.ResizeDiagonalReverse =>
                StandardCursor.NeswResize,

            _ =>
                StandardCursor.Default
        };
    }
}