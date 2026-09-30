namespace Engine.Animation;

public sealed class AnimationBindingSet
{
    private readonly Dictionary<
        AnimationTrackId,
        IAnimationBinding> _bindings =
        new();

    public IReadOnlyCollection<IAnimationBinding> Bindings =>
        _bindings.Values;

    public int Count =>
        _bindings.Count;

    public void Add<T>(
        AnimationTrackId trackId,
        Action<T> apply)
    {
        ArgumentNullException.ThrowIfNull(
            apply);

        if (!_bindings.TryAdd(
                trackId,
                new AnimationBinding<T>(
                    trackId,
                    apply)))
        {
            throw new InvalidOperationException(
                $"Animation binding for track '{trackId}' is already registered.");
        }
    }

    public bool Remove(
        AnimationTrackId trackId)
    {
        return _bindings.Remove(
            trackId);
    }

    public void Clear()
    {
        _bindings.Clear();
    }

    public void Apply(
        AnimationClip clip,
        double timeSeconds)
    {
        ArgumentNullException.ThrowIfNull(
            clip);

        if (!double.IsFinite(
                timeSeconds))
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeSeconds));
        }

        foreach (var binding in _bindings.Values)
        {
            binding.Apply(
                clip,
                timeSeconds);
        }
    }
}