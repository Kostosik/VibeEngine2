namespace Engine.Graphics.Rendering;

/// <summary>
/// Stores post-processing effects in execution order.
/// </summary>
public sealed class PostProcessChain
{
    private readonly List<IPostProcessEffect> _effects = new();

    public IReadOnlyList<IPostProcessEffect> Effects =>
        _effects;

    public int Count =>
        _effects.Count;

    public bool HasEnabledEffects
    {
        get
        {
            foreach (var effect in _effects)
            {
                if (effect.Enabled)
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Adds an effect to the end of the chain.
    /// </summary>
    public void Add(IPostProcessEffect effect)
    {
        ValidateEffect(effect);
        _effects.Add(effect);
    }

    /// <summary>
    /// Inserts an effect at the specified position.
    /// </summary>
    public void Insert(
        int index,
        IPostProcessEffect effect)
    {
        ValidateEffect(effect);

        if (index < 0 || index > _effects.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index));
        }

        _effects.Insert(index, effect);
    }

    /// <summary>
    /// Removes the first matching effect.
    /// </summary>
    public bool Remove(IPostProcessEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);

        return _effects.Remove(effect);
    }

    /// <summary>
    /// Removes an effect at the specified position.
    /// </summary>
    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _effects.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index));
        }

        _effects.RemoveAt(index);
    }

    /// <summary>
    /// Moves an effect to another position.
    /// The destination is its final index in the chain.
    /// </summary>
    public void Move(
        int fromIndex,
        int toIndex)
    {
        ValidateExistingIndex(fromIndex, nameof(fromIndex));
        ValidateExistingIndex(toIndex, nameof(toIndex));

        if (fromIndex == toIndex)
        {
            return;
        }

        var effect = _effects[fromIndex];

        _effects.RemoveAt(fromIndex);
        _effects.Insert(toIndex, effect);
    }

    /// <summary>
    /// Removes all effects from the chain.
    /// </summary>
    public void Clear()
    {
        _effects.Clear();
    }

    private void ValidateEffect(IPostProcessEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            effect.Name);
    }

    private void ValidateExistingIndex(
        int index,
        string parameterName)
    {
        if (index < 0 || index >= _effects.Count)
        {
            throw new ArgumentOutOfRangeException(
                parameterName);
        }
    }
}