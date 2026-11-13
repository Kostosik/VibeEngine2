namespace Engine.Graphics.Resources;

public readonly record struct ShaderVariant
{
    private readonly ShaderStageSource[]? _stages;
    private readonly IReadOnlyList<ShaderStageSource>? _stagesView;

    /// <summary>
    /// Backward-compatible constructor for vertex and fragment shaders.
    /// </summary>
    public ShaderVariant(
        ShaderLanguage language,
        string vertexSource,
        string fragmentSource,
        string vertexEntryPoint = "main",
        string fragmentEntryPoint = "main")
        : this(
            language,
            new ShaderStageSource(
                ShaderStage.Vertex,
                vertexSource,
                vertexEntryPoint),
            new ShaderStageSource(
                ShaderStage.Fragment,
                fragmentSource,
                fragmentEntryPoint))
    {
    }

    /// <summary>
    /// Creates a shader variant containing arbitrary stages.
    /// </summary>
    public ShaderVariant(
        ShaderLanguage language,
        params ShaderStageSource[] stages)
    {
        if (!Enum.IsDefined(language))
        {
            throw new ArgumentOutOfRangeException(nameof(language));
        }

        ArgumentNullException.ThrowIfNull(stages);

        if (stages.Length == 0)
        {
            throw new ArgumentException(
                "At least one shader stage is required.",
                nameof(stages));
        }

        var copy = stages.ToArray();
        var usedStages = new HashSet<ShaderStage>();

        foreach (var stage in copy)
        {
            stage.Validate();

            if (!usedStages.Add(stage.Stage))
            {
                throw new ArgumentException(
                    $"Duplicate shader stage: {stage.Stage}.",
                    nameof(stages));
            }
        }

        Language = language;
        _stages = copy;
        _stagesView = Array.AsReadOnly(copy);
    }

    public ShaderLanguage Language { get; }

    public IReadOnlyList<ShaderStageSource> Stages =>
        _stagesView ?? Array.Empty<ShaderStageSource>();

    public bool TryGetStage(
        ShaderStage stage,
        out ShaderStageSource source)
    {
        if (!Enum.IsDefined(stage))
        {
            throw new ArgumentOutOfRangeException(nameof(stage));
        }

        foreach (var candidate in Stages)
        {
            if (candidate.Stage == stage)
            {
                source = candidate;
                return true;
            }
        }

        source = default;
        return false;
    }

    public ShaderStageSource GetStage(ShaderStage stage)
    {
        if (TryGetStage(stage, out var source))
        {
            return source;
        }

        var available = string.Join(
            ", ",
            Stages.Select(x => x.Stage));

        throw new NotSupportedException(
            $"Shader variant does not contain the '{stage}' stage. " +
            $"Available stages: {available}.");
    }

    // Compatibility accessors for existing OpenGL-based code.

    public string VertexSource =>
        GetStage(ShaderStage.Vertex).Source;

    public string FragmentSource =>
        GetStage(ShaderStage.Fragment).Source;

    public string VertexEntryPoint =>
        GetStage(ShaderStage.Vertex).EntryPoint;

    public string FragmentEntryPoint =>
        GetStage(ShaderStage.Fragment).EntryPoint;

    internal void Validate()
    {
        if (!Enum.IsDefined(Language))
        {
            throw new ArgumentOutOfRangeException(nameof(Language));
        }

        if (_stages is null || _stages.Length == 0)
        {
            throw new ArgumentException(
                "At least one shader stage is required.");
        }

        var usedStages = new HashSet<ShaderStage>();

        foreach (var stage in _stages)
        {
            stage.Validate();

            if (!usedStages.Add(stage.Stage))
            {
                throw new ArgumentException(
                    $"Duplicate shader stage: {stage.Stage}.");
            }
        }
    }
}