using Engine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL.Resources;

internal sealed class OpenGLGraphicsBufferManager :
    IGraphicsBufferManager,
    IDisposable
{
    private sealed class Entry
    {
        public required uint Handle { get; init; }

        public required GraphicsBufferDescription Description
        {
            get;
            init;
        }
    }

    private readonly GL _gl;

    private readonly Dictionary<
        GraphicsBufferHandle,
        Entry> _buffers = new();

    private uint _nextHandle = 1;

    private bool _disposed;

    public OpenGLGraphicsBufferManager(
        GL gl)
    {
        ArgumentNullException.ThrowIfNull(
            gl);

        _gl = gl;
    }

    public GraphicsBufferHandle Create(
        GraphicsBufferDescription description)
    {
        ThrowIfDisposed();

        var handle =
            _gl.GenBuffer();

        if (handle == 0)
        {
            throw new InvalidOperationException(
                "OpenGL failed to create a graphics buffer.");
        }

        var target =
            GetTarget(
                description.Type);

        var usage =
            GetUsage(
                description.Usage);

        _gl.BindBuffer(
            target,
            handle);

        unsafe
        {
            _gl.BufferData(
                target,
                (nuint)description.SizeInBytes,
                null,
                usage);
        }

        _gl.BindBuffer(
            target,
            0);

        var resourceHandle =
            CreateHandle();

        _buffers.Add(
            resourceHandle,
            new Entry
            {
                Handle = handle,
                Description = description
            });

        return resourceHandle;
    }

    public void Update(
        GraphicsBufferHandle buffer,
        ReadOnlySpan<byte> data,
        int offsetInBytes = 0)
    {
        ThrowIfDisposed();

        if (data.IsEmpty)
        {
            return;
        }

        var entry =
            GetEntry(
                buffer);

        if (offsetInBytes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offsetInBytes));
        }

        if (offsetInBytes >
            entry.Description.SizeInBytes -
            data.Length)
        {
            throw new ArgumentException(
                "The data does not fit inside the graphics buffer.",
                nameof(data));
        }

        var target =
            GetTarget(
                entry.Description.Type);

        _gl.BindBuffer(
            target,
            entry.Handle);

        unsafe
        {
            fixed (byte* pointer = data)
            {
                _gl.BufferSubData(
                    target,
                    (nint)offsetInBytes,
                    (nuint)data.Length,
                    pointer);
            }
        }

        _gl.BindBuffer(
            target,
            0);
    }

    public bool Exists(
        GraphicsBufferHandle buffer)
    {
        ThrowIfDisposed();

        return buffer.IsValid &&
               _buffers.ContainsKey(
                   buffer);
    }

    public GraphicsBufferDescription GetDescription(
        GraphicsBufferHandle buffer)
    {
        ThrowIfDisposed();

        return GetEntry(
            buffer)
            .Description;
    }

    public void Destroy(
        GraphicsBufferHandle buffer)
    {
        ThrowIfDisposed();

        if (!_buffers.Remove(
                buffer,
                out var entry))
        {
            return;
        }

        _gl.DeleteBuffer(
            entry.Handle);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var entry in _buffers.Values)
        {
            _gl.DeleteBuffer(
                entry.Handle);
        }

        _buffers.Clear();

        _disposed = true;
    }

    internal uint GetNativeHandle(
        GraphicsBufferHandle buffer)
    {
        ThrowIfDisposed();

        return GetEntry(
            buffer)
            .Handle;
    }

    private GraphicsBufferHandle CreateHandle()
    {
        while (_nextHandle == 0 ||
               _buffers.ContainsKey(
                   new GraphicsBufferHandle(
                       _nextHandle)))
        {
            _nextHandle++;

            if (_nextHandle == 0)
            {
                throw new InvalidOperationException(
                    "Graphics buffer handle space is exhausted.");
            }
        }

        return new GraphicsBufferHandle(
            _nextHandle++);
    }

    private Entry GetEntry(
        GraphicsBufferHandle buffer)
    {
        if (!buffer.IsValid ||
            !_buffers.TryGetValue(
                buffer,
                out var entry))
        {
            throw new KeyNotFoundException(
                $"Graphics buffer '{buffer.Value}' was not found.");
        }

        return entry;
    }

    private static BufferTargetARB GetTarget(
        GraphicsBufferType type)
    {
        return type switch
        {
            GraphicsBufferType.Vertex =>
                BufferTargetARB.ArrayBuffer,

            GraphicsBufferType.Index =>
                BufferTargetARB.ElementArrayBuffer,

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(type),
                    type,
                    null)
        };
    }

    private static BufferUsageARB GetUsage(
        GraphicsBufferUsage usage)
    {
        return usage switch
        {
            GraphicsBufferUsage.Static =>
                BufferUsageARB.StaticDraw,

            GraphicsBufferUsage.Dynamic =>
                BufferUsageARB.DynamicDraw,

            GraphicsBufferUsage.Stream =>
                BufferUsageARB.StreamDraw,

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(usage),
                    usage,
                    null)
        };
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}