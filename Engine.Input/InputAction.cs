namespace Engine.Input;

public readonly record struct InputAction(string Name)
{
    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Name);

    public override string ToString()
    {
        return Name;
    }
}