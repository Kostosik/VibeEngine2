namespace Engine.Input;

public sealed class InputActionMap
{
    private readonly Dictionary<InputAction, List<InputBinding>> _bindings = new();

    internal IReadOnlyCollection<InputAction> Actions =>
        _bindings.Keys;

    public void Bind(
        InputAction action,
        InputBinding binding)
    {
        if (!action.IsValid)
        {
            throw new ArgumentException(
                "Input action is invalid.",
                nameof(action));
        }

        if (!binding.IsValid)
        {
            throw new ArgumentException(
                "Input binding is invalid.",
                nameof(binding));
        }

        if (!_bindings.TryGetValue(
                action,
                out var bindings))
        {
            bindings = new List<InputBinding>();

            _bindings.Add(action, bindings);
        }

        if (!bindings.Contains(binding))
        {
            bindings.Add(binding);
        }
    }

    public void Unbind(
        InputAction action)
    {
        _bindings.Remove(action);
    }

    public void Clear()
    {
        _bindings.Clear();
    }

    public bool IsBound(
        InputAction action)
    {
        return _bindings.ContainsKey(action);
    }

    internal float GetValue(
        InputAction action,
        IInputBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);

        if (!_bindings.TryGetValue(
                action,
                out var bindings))
        {
            return 0.0f;
        }

        var value = 0.0f;

        foreach (var binding in bindings)
        {
            var bindingValue =
                backend.GetValue(binding);

            if (MathF.Abs(bindingValue) >
                MathF.Abs(value))
            {
                value = bindingValue;
            }
        }

        return value;
    }
}