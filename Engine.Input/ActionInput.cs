namespace Engine.Input;

public sealed class ActionInput : IInput
{
    private readonly IInputBackend _backend;
    private readonly InputActionMap _actionMap;

    private readonly Dictionary<InputAction, float> _currentValues = new();
    private readonly Dictionary<InputAction, float> _previousValues = new();

    public ActionInput(
        IInputBackend backend,
        InputActionMap actionMap)
    {
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(actionMap);

        _backend = backend;
        _actionMap = actionMap;
    }

    public void Update()
    {
        _previousValues.Clear();

        foreach (var pair in _currentValues)
        {
            _previousValues[pair.Key] = pair.Value;
        }

        _backend.Update();

        _currentValues.Clear();

        foreach (var action in _actionMap.Actions)
        {
            _currentValues[action] =
                _actionMap.GetValue(
                    action,
                    _backend);
        }
    }

    public bool IsDown(
        InputAction action)
    {
        return GetCurrentValue(action) != 0.0f;
    }

    public bool IsPressed(
        InputAction action)
    {
        var current =
            GetCurrentValue(action);

        var previous =
            GetPreviousValue(action);

        return current != 0.0f &&
               previous == 0.0f;
    }

    public bool IsReleased(
        InputAction action)
    {
        var current =
            GetCurrentValue(action);

        var previous =
            GetPreviousValue(action);

        return current == 0.0f &&
               previous != 0.0f;
    }

    public float GetValue(
        InputAction action)
    {
        return GetCurrentValue(action);
    }

    private float GetCurrentValue(
        InputAction action)
    {
        return _currentValues.TryGetValue(
                action,
                out var value)
            ? value
            : 0.0f;
    }

    private float GetPreviousValue(
        InputAction action)
    {
        return _previousValues.TryGetValue(
                action,
                out var value)
            ? value
            : 0.0f;
    }
}