
namespace Engine.Graphics.Resources;

/// <summary>
/// Source code for one shader language.
/// Fragment stage maps to a pixel shader on Direct3D.
/// </summary>
public readonly record struct ShaderVariant
{
    public ShaderVariant(
        ShaderLanguage language,
        string vertexSource,
        string fragmentSource,
        string vertexEntryPoint = "main",
        string fragmentEntryPoint = "main")
    {
        if (!Enum.IsDefined(language))
        {
            throw new ArgumentOutOfRangeException(
                nameof(language));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(vertexSource);
        ArgumentException.ThrowIfNullOrWhiteSpace(fragmentSource);
        ArgumentException.ThrowIfNullOrWhiteSpace(vertexEntryPoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(fragmentEntryPoint);

        Language = language;
        VertexSource = vertexSource;
        FragmentSource = fragmentSource;
        VertexEntryPoint = vertexEntryPoint;
        FragmentEntryPoint = fragmentEntryPoint;
    }

    public ShaderLanguage Language { get; }

    public string VertexSource { get; }

    public string FragmentSource { get; }

    public string VertexEntryPoint { get; }

    public string FragmentEntryPoint { get; }

    internal void Validate()
    {
        if (!Enum.IsDefined(Language))
        {
            throw new ArgumentOutOfRangeException(
                nameof(Language));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(VertexSource);
        ArgumentException.ThrowIfNullOrWhiteSpace(FragmentSource);
        ArgumentException.ThrowIfNullOrWhiteSpace(VertexEntryPoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(FragmentEntryPoint);
    }
}
