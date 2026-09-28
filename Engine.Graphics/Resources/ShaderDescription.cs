namespace Engine.Graphics.Resources;

public readonly record struct ShaderDescription
{
    public ShaderDescription(
        string vertexSource,
        string fragmentSource)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            vertexSource);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            fragmentSource);

        VertexSource =
            vertexSource;

        FragmentSource =
            fragmentSource;
    }

    public string VertexSource { get; }

    public string FragmentSource { get; }
}