using Engine.Core.Math;

namespace Engine.Editor.UI.Authoring;

public readonly record struct EditorUiLayout
{
    public EditorUiLayout(
        float x,
        float y,
        float width,
        float height)
    {
        Anchor =
            Vector2.Zero;

        Offset =
            new Vector2(
                x,
                y);

        Size =
            new Vector2(
                width,
                height);
    }

    public EditorUiLayout(
        Vector2 anchor,
        Vector2 offset,
        Vector2 size)
    {
        ValidateAnchor(
            anchor);

        if (size.X < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                "UI element width cannot be negative.");
        }

        if (size.Y < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                "UI element height cannot be negative.");
        }

        Anchor = anchor;
        Offset = offset;
        Size = size;
    }

    public Vector2 Anchor { get; init; }

    public Vector2 Offset { get; init; }

    public Vector2 Size { get; init; }

    public static EditorUiLayout Default =>
        new(
            Vector2.Zero,
            Vector2.Zero,
            new Vector2(
                200.0f,
                40.0f));

    public Rectangle Resolve(
        Vector2 parentSize)
    {
        if (parentSize.X < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(parentSize));
        }

        if (parentSize.Y < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(parentSize));
        }

        var position =
            new Vector2(
                parentSize.X *
                    Anchor.X +
                    Offset.X -
                    Size.X *
                    Anchor.X,

                parentSize.Y *
                    Anchor.Y +
                    Offset.Y -
                    Size.Y *
                    Anchor.Y);

        return new Rectangle(
            position.X,
            position.Y,
            Size.X,
            Size.Y);
    }

    private static void ValidateAnchor(
        Vector2 anchor)
    {
        if (anchor.X < 0.0f ||
            anchor.X > 1.0f ||
            anchor.Y < 0.0f ||
            anchor.Y > 1.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(anchor),
                "UI anchor values must be between 0 and 1.");
        }
    }
}