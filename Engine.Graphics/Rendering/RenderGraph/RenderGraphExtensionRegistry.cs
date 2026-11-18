namespace Engine.Graphics.Rendering.RenderGraph;

public readonly record struct RenderGraphExtensionSnapshot(
    long Revision,
    IReadOnlyList<IRenderGraphExtension> Extensions);

public interface IRenderGraphExtensionRegistry
{
    long Revision { get; }

    void Register(IRenderGraphExtension extension);

    bool Unregister(string name);

    RenderGraphExtensionSnapshot CaptureSnapshot();
}

/// <summary>
/// Stores registered render-graph extensions.
/// </summary>
public sealed class RenderGraphExtensionRegistry
    : IRenderGraphExtensionRegistry
{
    private readonly object _sync = new();

    private readonly Dictionary<string, IRenderGraphExtension> _extensions =
        new(StringComparer.Ordinal);

    private long _revision;

    public long Revision
    {
        get
        {
            lock (_sync)
                return _revision;
        }
    }

    public void Register(IRenderGraphExtension extension)
    {
        ArgumentNullException.ThrowIfNull(extension);

        ArgumentException.ThrowIfNullOrWhiteSpace(extension.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(
            extension.AfterPipelinePassName);

        lock (_sync)
        {
            if (_extensions.ContainsKey(extension.Name))
            {
                throw new InvalidOperationException(
                    $"Render graph extension '{extension.Name}' is already registered.");
            }

            _extensions.Add(extension.Name, extension);
            _revision++;
        }
    }

    public bool Unregister(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        lock (_sync)
        {
            if (!_extensions.Remove(name))
                return false;

            _revision++;
            return true;
        }
    }

    public RenderGraphExtensionSnapshot CaptureSnapshot()
    {
        lock (_sync)
        {
            var extensions = _extensions.Values
                .OrderBy(
                    extension => extension.AfterPipelinePassName,
                    StringComparer.Ordinal)
                .ThenBy(extension => extension.Order)
                .ThenBy(
                    extension => extension.Name,
                    StringComparer.Ordinal)
                .ToArray();

            return new RenderGraphExtensionSnapshot(
                _revision,
                Array.AsReadOnly(extensions));
        }
    }
}