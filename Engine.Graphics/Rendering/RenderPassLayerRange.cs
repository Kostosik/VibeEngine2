namespace Engine.Graphics.Rendering;

public readonly record struct RenderPassLayerRange(
    int Minimum,
    int Maximum)
{
    public bool Contains(int layer)
    {
        return layer >= Minimum &&
               layer <= Maximum;
    }

    public static RenderPassLayerRange All =>
        new(
            int.MinValue,
            int.MaxValue);
}