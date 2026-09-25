namespace Engine.Input;

public sealed class InputSnapshot
{
    private readonly Dictionary<InputBinding, float> _values;

    internal InputSnapshot()
    {
        _values = new Dictionary<InputBinding, float>();
    }

    public float GetValue(InputBinding binding)
    {
        return _values.TryGetValue(
            binding,
            out var value)
            ? value
            : 0.0f;
    }

    public bool IsDown(InputBinding binding)
    {
        return GetValue(binding) != 0.0f;
    }

    internal void Set(
        InputBinding binding,
        float value)
    {
        _values[binding] = value;
    }

    internal void Clear()
    {
        _values.Clear();
    }

    internal void CopyFrom(InputSnapshot source)
    {
        _values.Clear();

        foreach (var pair in source._values)
        {
            _values[pair.Key] = pair.Value;
        }
    }
}