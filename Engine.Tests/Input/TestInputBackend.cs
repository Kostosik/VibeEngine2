using Engine.Input;

namespace Engine.Tests.Input;

internal sealed class TestInputBackend : IInputBackend
{
    private readonly Dictionary<InputBinding, float> _values = new();

    public void Update()
    {
    }

    public float GetValue(InputBinding binding)
    {
        return _values.TryGetValue(
            binding,
            out var value)
            ? value
            : 0.0f;
    }

    public void Set(
        InputBinding binding,
        float value)
    {
        _values[binding] = value;
    }

    public void Clear()
    {
        _values.Clear();
    }
}