using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering;

/// <summary>
/// Owns two render targets used for sequential post-processing.
/// One target is read from while the other is written to.
/// </summary>
public sealed class PingPongRenderTargets : IDisposable
{
    private readonly IRenderTargetManager _manager;

    private RenderTargetHandle _targetA;
    private RenderTargetHandle _targetB;

    private bool _readFromA = true;
    private bool _disposed;

    public PingPongRenderTargets(
        IRenderTargetManager manager,
        RenderTargetDescription description)
    {
        ArgumentNullException.ThrowIfNull(manager);
        description.Validate();

        _manager = manager;

        (_targetA, _targetB) = CreatePair(description);
    }

    /// <summary>
    /// Current source render target.
    /// Its contents are valid only after a successful write and swap.
    /// </summary>
    public RenderTargetHandle ReadTarget
    {
        get
        {
            ThrowIfDisposed();
            return _readFromA ? _targetA : _targetB;
        }
    }

    /// <summary>
    /// Current destination render target.
    /// </summary>
    public RenderTargetHandle WriteTarget
    {
        get
        {
            ThrowIfDisposed();
            return _readFromA ? _targetB : _targetA;
        }
    }

    public TextureHandle ReadTexture =>
        _manager.GetColorTexture(ReadTarget);

    public TextureHandle WriteTexture =>
        _manager.GetColorTexture(WriteTarget);

    public RenderTargetDescription Description
    {
        get
        {
            ThrowIfDisposed();
            return _manager.GetDescription(_targetA);
        }
    }

    /// <summary>
    /// Makes the previous write target the new read target.
    /// Call after successfully rendering into WriteTarget.
    /// </summary>
    public void Swap()
    {
        ThrowIfDisposed();
        _readFromA = !_readFromA;
    }

    /// <summary>
    /// Recreates both targets when their dimensions change.
    /// The replacement is created before the old targets are destroyed.
    /// </summary>
    public void Resize(int width, int height)
    {
        ThrowIfDisposed();

        var current = Description;

        if (current.Width == width &&
            current.Height == height)
        {
            return;
        }

        var description = new RenderTargetDescription(
            width,
            height,
            current.Format);

        description.Validate();

        var (newTargetA, newTargetB) = CreatePair(description);

        var oldTargetA = _targetA;
        var oldTargetB = _targetB;

        _targetA = newTargetA;
        _targetB = newTargetB;
        _readFromA = true;

        DestroyPair(oldTargetA, oldTargetB);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        var targetA = _targetA;
        var targetB = _targetB;

        _targetA = RenderTargetHandle.Invalid;
        _targetB = RenderTargetHandle.Invalid;

        DestroyPair(targetA, targetB);
    }

    private (RenderTargetHandle A, RenderTargetHandle B) CreatePair(
        RenderTargetDescription description)
    {
        var targetA = RenderTargetHandle.Invalid;
        var targetB = RenderTargetHandle.Invalid;

        try
        {
            targetA = _manager.Create(description);

            if (!targetA.IsValid)
            {
                throw new InvalidOperationException(
                    "Failed to create the first ping-pong render target.");
            }

            targetB = _manager.Create(description);

            if (!targetB.IsValid)
            {
                throw new InvalidOperationException(
                    "Failed to create the second ping-pong render target.");
            }

            return (targetA, targetB);
        }
        catch
        {
            TryDestroy(targetB);
            TryDestroy(targetA);
            throw;
        }
    }

    private void DestroyPair(
        RenderTargetHandle targetA,
        RenderTargetHandle targetB)
    {
        var errors = new List<Exception>();

        TryDestroy(targetA, errors);
        TryDestroy(targetB, errors);

        if (errors.Count > 0)
        {
            throw new AggregateException(
                "One or more ping-pong render targets could not be released.",
                errors);
        }
    }

    private void TryDestroy(RenderTargetHandle target)
    {
        if (!target.IsValid)
        {
            return;
        }

        try
        {
            _manager.Destroy(target);
        }
        catch
        {
            // Preserve the original exception during creation rollback.
        }
    }

    private void TryDestroy(
        RenderTargetHandle target,
        List<Exception> errors)
    {
        if (!target.IsValid)
        {
            return;
        }

        try
        {
            _manager.Destroy(target);
        }
        catch (Exception exception)
        {
            errors.Add(exception);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}