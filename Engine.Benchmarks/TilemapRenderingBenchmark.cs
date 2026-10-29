using System.Diagnostics;
using Engine.Camera;
using Engine.Core.Math;
using Engine.Graphics;
using Engine.Graphics.Commands;
using Engine.Graphics.Fonts;
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;
using Engine.Graphics.Tilemaps;

namespace Engine.Benchmarks;

internal static class TilemapRenderingBenchmark
{
    private const int WarmupFrames = 5;
    private const int SampleCount = 9;

    private static readonly int[] MapSizes =
    [
        64,
        128,
        256
    ];

    public static void Run()
    {
        Console.WriteLine();
        Console.WriteLine("Tilemap Rendering Baseline");
        Console.WriteLine(
            "Measures visible-tile command generation; no GPU.");

        foreach (var size in MapSizes)
        {
            Measure(size);
        }

        Console.WriteLine();
        Console.WriteLine("Tilemap rendering benchmark complete.");
    }

    private static void Measure(int size)
    {
        var tileCount = checked(size * size);

        var tiles = new uint[tileCount];
        Array.Fill(tiles, 1u);

        // Создание данных карты не входит в измерение.
        var definition = new TilemapDefinition(
            TilemapDefinition.CurrentFormatVersion,
            "TileSets/benchmark.tileset.json",
            size,
            size,
            tiles);

        var tileSet = new TileSet();

        tileSet.Set(
            1u,
            new TextureHandle(1),
            new Rectangle(0, 0, 1, 1));

        // Камера видит всю карту целиком.
        var camera = new Camera2D(
            new Vector2(size, size));

        camera.Position = new Vector2(
            size * 0.5f,
            size * 0.5f);

        var graphics = new CountingGraphicsDevice();

        var renderer = new TilemapRenderer(
            graphics,
            camera,
            tileSet,
            tileSize: 1.0f);

        void RenderFrame()
        {
            graphics.ResetCount();

            renderer.RenderMap(
                definition,
                new Vector2(0, 0),
                new Vector2(1, 1),
                rotationRadians: 0);
        }

        for (var i = 0; i < WarmupFrames; i++)
        {
            RenderFrame();
        }

        if (graphics.SubmittedCount != tileCount)
        {
            throw new InvalidOperationException(
                $"Expected {tileCount} visible tiles, " +
                $"but rendered {graphics.SubmittedCount}.");
        }

        var times = new double[SampleCount];
        var allocations = new long[SampleCount];

        for (var i = 0; i < SampleCount; i++)
        {
            var allocatedBefore =
                GC.GetAllocatedBytesForCurrentThread();

            var started = Stopwatch.GetTimestamp();

            RenderFrame();

            times[i] =
                Stopwatch.GetElapsedTime(started)
                    .TotalMilliseconds;

            allocations[i] =
                GC.GetAllocatedBytesForCurrentThread()
                - allocatedBefore;
        }

        Array.Sort(times);
        Array.Sort(allocations);

        var medianTime = times[SampleCount / 2];
        var medianAllocations = allocations[SampleCount / 2];

        Console.WriteLine(
            $"  Map={size,3}x{size,-3} " +
            $"tiles={graphics.SubmittedCount,7:N0} " +
            $"median={medianTime,8:F3} ms/frame " +
            $"alloc={medianAllocations,10:N0} B/frame " +
            $"alloc/tile={(double)medianAllocations / tileCount,7:F2} B");
    }

    private sealed class CountingGraphicsDevice : IGraphicsDevice
    {
        public int SubmittedCount { get; private set; }

        public void ResetCount()
        {
            SubmittedCount = 0;
        }

        public void Submit(IRenderCommand command)
        {
            SubmittedCount++;
        }

        public ITextureManager Textures =>
            throw new NotSupportedException();

        public IFontManager Fonts =>
            throw new NotSupportedException();

        public IGraphicsBufferManager Buffers =>
            throw new NotSupportedException();

        public IShaderManager Shaders =>
            throw new NotSupportedException();

        public IRenderTargetManager RenderTargets =>
            throw new NotSupportedException();

        public RenderPipeline Pipeline =>
            throw new NotSupportedException();

        public void BeginFrame()
        {
        }

        public void EndFrame()
        {
        }
    }
}