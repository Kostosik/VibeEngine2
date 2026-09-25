namespace Engine.Tooling.Validation;

public sealed class ValidationContext
{
    public ValidationContext(
        bool developmentMode = true)
    {
        DevelopmentMode =
            developmentMode;
    }

    public bool DevelopmentMode { get; }
}