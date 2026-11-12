
namespace Engine.Graphics.Resources;

public readonly record struct ShaderDescription
{
    private readonly ShaderVariant[]? _variants;
    private readonly IReadOnlyList<ShaderVariant>? _variantsView;

    /// <summary>
    /// Legacy constructor. Creates a GLSL-only shader description.
    /// </summary>
    public ShaderDescription(
        string vertexSource,
        string fragmentSource)
        : this(
            new ShaderVariant(
                ShaderLanguage.Glsl,
                vertexSource,
                fragmentSource))
    {
    }

    /// <summary>
    /// Creates a shader description with one or more language variants.
    /// </summary>
    public ShaderDescription(params ShaderVariant[] variants)
    {
        ArgumentNullException.ThrowIfNull(variants);

        if (variants.Length == 0)
        {
            throw new ArgumentException(
                "At least one shader variant is required.",
                nameof(variants));
        }

        var copy = variants.ToArray();
        var languages = new HashSet<ShaderLanguage>();

        foreach (var variant in copy)
        {
            variant.Validate();

            if (!languages.Add(variant.Language))
            {
                throw new ArgumentException(
                    $"Duplicate shader language: {variant.Language}.",
                    nameof(variants));
            }
        }

        _variants = copy;
        _variantsView = Array.AsReadOnly(copy);
    }

    public IReadOnlyList<ShaderVariant> Variants =>
        _variantsView ?? Array.Empty<ShaderVariant>();

    public bool TryGetVariant(
        ShaderLanguage language,
        out ShaderVariant variant)
    {
        foreach (var candidate in Variants)
        {
            if (candidate.Language == language)
            {
                variant = candidate;
                return true;
            }
        }

        variant = default;
        return false;
    }

    public ShaderVariant GetVariant(ShaderLanguage language)
    {
        if (TryGetVariant(language, out var variant))
        {
            return variant;
        }

        var available = string.Join(
            ", ",
            Variants.Select(x => x.Language));

        throw new NotSupportedException(
            $"Shader does not contain a '{language}' variant. " +
            $"Available variants: {available}.");
    }

    // Compatibility accessors for existing GLSL-based code.
    public string VertexSource =>
        GetVariant(ShaderLanguage.Glsl).VertexSource;

    public string FragmentSource =>
        GetVariant(ShaderLanguage.Glsl).FragmentSource;
}
