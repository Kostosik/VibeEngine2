namespace Engine.Core.IDs;

public readonly record struct Id(ulong Value)
{
    public bool IsValid => Value != 0;

    public static Id Invalid => default;
}