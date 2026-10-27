namespace Engine.Graphics.Rendering;

public readonly record struct RenderPassLayerRange
{
    public RenderPassLayerRange(
        int Minimum,
        int Maximum)
    {
        if (Minimum > Maximum)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Maximum),
                "Maximum layer must be greater than or equal to minimum layer.");
        }

        this.Minimum = Minimum;
        this.Maximum = Maximum;
    }

    public int Minimum { get; }

    public int Maximum { get; }

    public bool Contains(int layer)
    {
        return layer >= Minimum &&
               layer <= Maximum;
    }

    public static RenderPassLayerRange All =>
        new(
            int.MinValue,
            int.MaxValue);

    public void Deconstruct(
        out int Minimum,
        out int Maximum)
    {
        Minimum = this.Minimum;
        Maximum = this.Maximum;
    }
}