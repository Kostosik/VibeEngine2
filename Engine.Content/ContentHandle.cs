namespace Engine.Content;

public readonly record struct ContentHandle<T>(
    int Id)
    where T : class
{
    public bool IsValid =>
        Id != 0;
}