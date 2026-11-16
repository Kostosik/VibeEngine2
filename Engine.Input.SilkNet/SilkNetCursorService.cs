using Engine.Input.Cursors;
using Silk.NET.Input;
using SilkCursorMode = Silk.NET.Input.CursorMode;

namespace Engine.Input.SilkNet;

public sealed class SilkNetCursorService :
    ICursorService
{
    private readonly ICursor _cursor;

    private PointerMode _mode = PointerMode.Normal;

    public PointerMode Mode => _mode;

    public bool IsModeSupported(PointerMode mode)
    {
        return mode switch
        {
            PointerMode.Normal =>
                _cursor.IsSupported(SilkCursorMode.Normal),

            PointerMode.Hidden =>
                _cursor.IsSupported(SilkCursorMode.Hidden),

            PointerMode.Locked =>
                _cursor.IsSupported(SilkCursorMode.Raw) ||
                _cursor.IsSupported(SilkCursorMode.Disabled),

            _ => false
        };
    }

    public void SetMode(PointerMode mode)
    {
        var nativeMode = mode switch
        {
            PointerMode.Normal =>
                SilkCursorMode.Normal,

            PointerMode.Hidden =>
                SilkCursorMode.Hidden,

            PointerMode.Locked =>
                _cursor.IsSupported(SilkCursorMode.Raw)
                    ? SilkCursorMode.Raw
                    : SilkCursorMode.Disabled,

            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };

        if (!_cursor.IsSupported(nativeMode))
        {
            throw new NotSupportedException(
                $"Cursor mode '{mode}' is not supported.");
        }

        _cursor.CursorMode = nativeMode;
        _mode = mode;
    }

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