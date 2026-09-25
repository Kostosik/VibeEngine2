namespace Engine.Input;

public readonly record struct InputBinding(string Path)
{
    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Path);

    public override string ToString()
    {
        return Path;
    }
}