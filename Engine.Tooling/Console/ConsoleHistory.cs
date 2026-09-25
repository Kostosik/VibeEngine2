namespace Engine.Tooling.Console;

public sealed class ConsoleHistory
{
    private readonly int _capacity;
    private readonly List<string> _entries = new();

    public ConsoleHistory(
        int capacity = 100)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                "History capacity must be greater than zero.");
        }

        _capacity = capacity;
    }

    public IReadOnlyList<string> Entries =>
        _entries;

    public void Add(
        string command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            command);

        if (_entries.Count >= _capacity)
        {
            _entries.RemoveAt(0);
        }

        _entries.Add(command);
    }

    public void Clear()
    {
        _entries.Clear();
    }
}