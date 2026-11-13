namespace Engine.Graphics.Resources;

public readonly record struct ShaderStageSource
{
    public ShaderStageSource(
        ShaderStage stage,
        string source,
        string entryPoint = "main")
    {
        if (!Enum.IsDefined(stage))
        {
            throw new ArgumentOutOfRangeException(nameof(stage));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(entryPoint);

        Stage = stage;
        Source = source;
        EntryPoint = entryPoint;
    }

    public ShaderStage Stage { get; }

    public string Source { get; }

    public string EntryPoint { get; }

    internal void Validate()
    {
        if (!Enum.IsDefined(Stage))
        {
            throw new ArgumentOutOfRangeException(nameof(Stage));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(Source);
        ArgumentException.ThrowIfNullOrWhiteSpace(EntryPoint);
    }
}