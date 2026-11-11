
using System.Runtime.InteropServices;
using Engine.Graphics.Resources;
using Engine.Graphics3D;
using Engine.Graphics3D.Resources;
using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL.Resources;

internal sealed class OpenGLMeshManager : IMeshManager, IDisposable
{
    private sealed class Entry
    {
        public required uint VertexArray { get; init; }

        public required GraphicsBufferHandle VertexBuffer { get; init; }

        public required GraphicsBufferHandle IndexBuffer { get; init; }

        public required MeshDescription Description { get; init; }
    }

    private readonly GL _gl;
    private readonly OpenGLGraphicsBufferManager _buffers;
    private readonly Dictionary<MeshHandle, Entry> _meshes = new();

    private uint _nextHandle = 1;
    private bool _disposed;

    public OpenGLMeshManager(
        GL gl,
        OpenGLGraphicsBufferManager buffers)
    {
        ArgumentNullException.ThrowIfNull(gl);
        ArgumentNullException.ThrowIfNull(buffers);

        _gl = gl;
        _buffers = buffers;
    }

    public MeshHandle Create(
        MeshData mesh,
        GraphicsBufferUsage usage = GraphicsBufferUsage.Static)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(mesh);

        var description = new MeshDescription(
            mesh.VertexCount,
            mesh.IndexCount,
            mesh.Layout,
            usage);

        var vertexData = MemoryMarshal.AsBytes(
            mesh.Vertices.Span);

        var indexData = MemoryMarshal.AsBytes(
            mesh.Indices.Span);

        if (indexData.Length != checked(mesh.IndexCount * sizeof(uint)))
        {
            throw new ArgumentException(
                "Mesh indices must use 32-bit elements.",
                nameof(mesh));
        }

        var vertexBuffer = GraphicsBufferHandle.Invalid;
        var indexBuffer = GraphicsBufferHandle.Invalid;
        uint vertexArray = 0;

        try
        {
            vertexBuffer = _buffers.Create(
                new GraphicsBufferDescription(
                    GraphicsBufferType.Vertex,
                    vertexData.Length,
                    usage));

            indexBuffer = _buffers.Create(
                new GraphicsBufferDescription(
                    GraphicsBufferType.Index,
                    indexData.Length,
                    usage));

            _buffers.Update(vertexBuffer, vertexData);
            _buffers.Update(indexBuffer, indexData);

            vertexArray = _gl.GenVertexArray();

            if (vertexArray == 0)
            {
                throw new InvalidOperationException(
                    "OpenGL failed to create a vertex array.");
            }

            _gl.BindVertexArray(vertexArray);

            _gl.BindBuffer(
                BufferTargetARB.ArrayBuffer,
                _buffers.GetNativeHandle(vertexBuffer));

            // Index buffer привязывается к текущему VAO.
            _gl.BindBuffer(
                BufferTargetARB.ElementArrayBuffer,
                _buffers.GetNativeHandle(indexBuffer));

            ConfigureAttributes(mesh.Layout);

            _gl.BindVertexArray(0);

            var handle = CreateHandle();

            _meshes.Add(
                handle,
                new Entry
                {
                    VertexArray = vertexArray,
                    VertexBuffer = vertexBuffer,
                    IndexBuffer = indexBuffer,
                    Description = description
                });

            return handle;
        }
        catch (Exception creationError)
        {
            try
            {
                ReleaseResources(
                    vertexArray,
                    vertexBuffer,
                    indexBuffer);
            }
            catch (Exception cleanupError)
            {
                throw new AggregateException(
                    "Mesh creation failed and cleanup was incomplete.",
                    creationError,
                    cleanupError);
            }

            throw;
        }
        finally
        {
            _gl.BindVertexArray(0);

            _gl.BindBuffer(
                BufferTargetARB.ArrayBuffer,
                0);
        }
    }

    public bool Exists(MeshHandle mesh)
    {
        ThrowIfDisposed();

        return mesh.IsValid && _meshes.ContainsKey(mesh);
    }

    public MeshDescription GetDescription(MeshHandle mesh)
    {
        ThrowIfDisposed();

        return GetEntry(mesh).Description;
    }

    public void Destroy(MeshHandle mesh)
    {
        ThrowIfDisposed();

        if (!_meshes.Remove(mesh, out var entry))
        {
            return;
        }

        ReleaseResources(
            entry.VertexArray,
            entry.VertexBuffer,
            entry.IndexBuffer);
    }

    private void ConfigureAttributes(VertexLayout layout)
    {
        foreach (var attribute in layout.Attributes.Span)
        {
            var location = GetLocation(attribute.Semantic);
            var componentCount = GetComponentCount(attribute.Format);

            unsafe
            {
                _gl.VertexAttribPointer(
                    location,
                    componentCount,
                    VertexAttribPointerType.Float,
                    false,
                    (uint)layout.StrideInBytes,
                    (void*)(nint)attribute.OffsetInBytes);
            }

            _gl.EnableVertexAttribArray(location);
        }
    }

    private static uint GetLocation(VertexAttributeSemantic semantic)
    {
        return semantic switch
        {
            VertexAttributeSemantic.Position => 0,
            VertexAttributeSemantic.Normal => 1,
            VertexAttributeSemantic.TextureCoordinate => 2,

            _ => throw new ArgumentOutOfRangeException(
                nameof(semantic),
                semantic,
                null)
        };
    }

    private static int GetComponentCount(VertexAttributeFormat format)
    {
        return format switch
        {
            VertexAttributeFormat.Float2 => 2,
            VertexAttributeFormat.Float3 => 3,
            VertexAttributeFormat.Float4 => 4,

            _ => throw new ArgumentOutOfRangeException(
                nameof(format),
                format,
                null)
        };
    }

    private MeshHandle CreateHandle()
    {
        while (_nextHandle == 0 ||
               _meshes.ContainsKey(new MeshHandle(_nextHandle)))
        {
            _nextHandle++;

            if (_nextHandle == 0)
            {
                throw new InvalidOperationException(
                    "Mesh handle space is exhausted.");
            }
        }

        return new MeshHandle(_nextHandle++);
    }

    internal uint GetVertexArray(MeshHandle mesh)
    {
        ThrowIfDisposed();

        return GetEntry(mesh).VertexArray;
    }

    private Entry GetEntry(MeshHandle mesh)
    {
        if (!mesh.IsValid ||
            !_meshes.TryGetValue(mesh, out var entry))
        {
            throw new KeyNotFoundException(
                $"Mesh '{mesh.Value}' was not found.");
        }

        return entry;
    }

    private void ReleaseResources(
        uint vertexArray,
        GraphicsBufferHandle vertexBuffer,
        GraphicsBufferHandle indexBuffer)
    {
        var errors = new List<Exception>();

        if (vertexArray != 0)
        {
            try
            {
                _gl.DeleteVertexArray(vertexArray);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        if (vertexBuffer.IsValid)
        {
            try
            {
                _buffers.Destroy(vertexBuffer);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        if (indexBuffer.IsValid)
        {
            try
            {
                _buffers.Destroy(indexBuffer);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        if (errors.Count > 0)
        {
            throw new AggregateException(
                "One or more mesh resources could not be released.",
                errors);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        var errors = new List<Exception>();

        foreach (var entry in _meshes.Values)
        {
            try
            {
                ReleaseResources(
                    entry.VertexArray,
                    entry.VertexBuffer,
                    entry.IndexBuffer);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        _meshes.Clear();
        _disposed = true;

        if (errors.Count > 0)
        {
            throw new AggregateException(
                "One or more OpenGL meshes could not be released.",
                errors);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
