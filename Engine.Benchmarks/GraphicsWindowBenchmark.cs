
using Engine.Camera;
using Engine.Core.Application;
using Engine.Core.Math;
using Engine.Core.Time;
using Engine.Graphics.Commands;
using Engine.Graphics.OpenGL;
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;
using Engine.Graphics2D.Sprites;
using System.Diagnostics;

namespace Engine.Benchmarks;

internal static class GraphicsWindowBenchmark
{
    public static void Run()
    {
        Console.WriteLine("VibeEngine Graphics Window Benchmark");
        Console.WriteLine("Resolution: 1280x720");
        Console.WriteLine("VSync: disabled by OpenGLWindow");
        Console.WriteLine();

        using var window = new OpenGLWindow(
            1280,
            720,
            "VibeEngine Graphics Benchmark");

        window.Initialize();

        var application = new BenchmarkApplication(window);
        var gameLoop = new GameLoop(
            application,
            new SimulationRate(60));

        gameLoop.Initialize();
        window.Run(gameLoop);
    }

    private sealed class BenchmarkApplication : IApplication
    {
        private const int WarmupFrames = 60;
        private const int MeasuredFrames = 180;
        private const int Columns = 160;
        private const float SpriteSize = 8.0f;

        private readonly OpenGLWindow _window;

        private enum TextureOrder
        {
            Single,
            Grouped,
            Alternating
        }

        private readonly Stage[] _stages =
        {
            new(100, 1, TextureOrder.Single),
            new(1_000, 1, TextureOrder.Single),
            new(10_000, 1, TextureOrder.Single),

            new(10_000, 16, TextureOrder.Grouped),
            new(10_000, 16, TextureOrder.Alternating)
        };

        private TextureHandle[] _textures = Array.Empty<TextureHandle>();
        private Sprite[] _sprites = Array.Empty<Sprite>();
        private SpriteRenderer? _spriteRenderer;
        private int _stageIndex;
        private int _frameIndex;

        private bool _advanceOnNextRender;
        private bool _finished;

        private long _lastRenderTimestamp;
        private int _lastStageIndex = -1;
        private int _lastFrameIndex = -1;

        public BenchmarkApplication(OpenGLWindow window)
        {
            _window = window;
        }

        public void Initialize()
        {
            var pixels = new byte[]
            {
        255, 255, 255, 255
            };

            var textureCount = _stages.Max(
                static stage => stage.TextureCount);

            _textures = new TextureHandle[textureCount];

            for (var i = 0; i < _textures.Length; i++)
            {
                _textures[i] =
                    _window.GraphicsDevice.Textures.Create(
                        new TextureData(
                            1,
                            1,
                            TextureFormat.Rgba8,
                            pixels));
            }
            _spriteRenderer =
    new SpriteRenderer(_window.GraphicsDevice);

            _sprites = new Sprite[_textures.Length];

            for (var i = 0; i < _textures.Length; i++)
            {
                _sprites[i] = new Sprite(
                    _textures[i],
                    new Rectangle(0, 0, 1, 1),
                    new Vector2(SpriteSize, SpriteSize),
                    RenderLayers.World);
            }
        }

        public void Update(TimeSnapshot time)
        {
        }

        public void FixedUpdate(SimulationTime time)
        {
        }

        public void Render(double interpolationAlpha)
        {
            // Время между входами в Render включает завершение
            // предыдущего кадра и работу оконного цикла.
            if (_lastRenderTimestamp != 0)
            {
                var elapsedMs = Stopwatch.GetElapsedTime(
                    _lastRenderTimestamp).TotalMilliseconds;

                if (_lastStageIndex >= 0 &&
                    _lastFrameIndex >= WarmupFrames)
                {
                    var previousStage = _stages[_lastStageIndex];
                    var sampleIndex =
                        _lastFrameIndex - WarmupFrames;

                    previousStage.FrameTimesMs[sampleIndex] =
                        elapsedMs;
                }

                // Не учитываем один и тот же интервал повторно.
                _lastRenderTimestamp = 0;
                _lastStageIndex = -1;
                _lastFrameIndex = -1;
            }

            if (_finished)
            {
                return;
            }

            // Переходим к следующей нагрузке после того,
            // как получен интервал последнего измеряемого кадра.
            if (_advanceOnNextRender)
            {
                PrintStage(_stages[_stageIndex]);

                _stageIndex++;

                if (_stageIndex >= _stages.Length)
                {
                    _finished = true;

                    Console.WriteLine(
                        "Graphics benchmark complete.");

                    Console.WriteLine(
                        "Close the benchmark window to exit.");

                    return;
                }

                _frameIndex = 0;
                _advanceOnNextRender = false;
            }

            var stage = _stages[_stageIndex];
            var frameIndex = _frameIndex;

            _lastRenderTimestamp = Stopwatch.GetTimestamp();
            _lastStageIndex = _stageIndex;
            _lastFrameIndex = frameIndex;

            var allocationBefore =
                GC.GetAllocatedBytesForCurrentThread();

            var submitStart = Stopwatch.GetTimestamp();

            for (var i = 0; i < stage.SpriteCount; i++)
            {
                var textureIndex = stage.TextureOrder switch
                {
                    TextureOrder.Single => 0,

                    TextureOrder.Grouped =>
                        (int)((long)i * stage.TextureCount / stage.SpriteCount),

                    TextureOrder.Alternating =>
                        i % stage.TextureCount,

                    _ => throw new InvalidOperationException(
                        "Unsupported texture order.")
                };

                var position = new Vector2(
                    (i % Columns) * SpriteSize,
                    (i / Columns) * SpriteSize);

                _spriteRenderer!.DrawWorld(
                    _sprites[textureIndex],
                    position);
            }

            var submitTimeMs =
                Stopwatch.GetElapsedTime(submitStart).TotalMilliseconds;

            var allocatedBytes =
                GC.GetAllocatedBytesForCurrentThread()
                - allocationBefore;

            if (frameIndex >= WarmupFrames)
            {
                var sampleIndex = frameIndex - WarmupFrames;

                stage.SubmitTimesMs[sampleIndex] = submitTimeMs;
                stage.AllocatedBytes[sampleIndex] = allocatedBytes;
            }

            _frameIndex++;

            if (_frameIndex >= WarmupFrames + MeasuredFrames)
            {
                _advanceOnNextRender = true;
            }
        }

        public void Shutdown()
        {
            foreach (var texture in _textures)
            {
                if (texture.IsValid)
                {
                    _window.GraphicsDevice.Textures.Destroy(texture);
                }
            }

            _textures = Array.Empty<TextureHandle>();
        }

        private static void PrintStage(Stage stage)
        {
            Console.WriteLine(
                $"Graphics workload: {stage.SpriteCount:N0} sprites, " +
                $"textures={stage.TextureCount}, order={stage.TextureOrder}");

            PrintStatistics(
                "Frame interval",
                stage.FrameTimesMs,
                "ms",
                showApproxFps: true);

            PrintStatistics(
                "Command submission",
                stage.SubmitTimesMs,
                "ms");

            PrintStatistics(
                "Command submission allocations",
                stage.AllocatedBytes,
                "bytes/frame");

            Console.WriteLine();
        }

        private static void PrintStatistics(
            string name,
            double[] values,
            string unit,
            bool showApproxFps = false)
        {
            var sorted = (double[])values.Clone();
            Array.Sort(sorted);

            var median = sorted.Length % 2 == 0
                ? (sorted[sorted.Length / 2 - 1]
                    + sorted[sorted.Length / 2]) / 2.0
                : sorted[sorted.Length / 2];

            var p95Index = (int)Math.Ceiling(
                sorted.Length * 0.95) - 1;

            var p95 = sorted[Math.Clamp(
                p95Index, 0, sorted.Length - 1)];

            Console.WriteLine($"  {name}:");
            Console.WriteLine($"    Median: {median:F3} {unit}");
            Console.WriteLine($"    P95:    {p95:F3} {unit}");
            Console.WriteLine($"    Max:    {sorted[^1]:F3} {unit}");

            if (showApproxFps && unit == "ms" && median > 0)
            {
                Console.WriteLine(
                    $"    Approx. FPS from median: {1000.0 / median:F1}");
            }
        }

        private sealed class Stage
        {
            public Stage(
                int spriteCount,
                int textureCount,
                TextureOrder textureOrder)
            {
                SpriteCount = spriteCount;
                TextureCount = textureCount;
                TextureOrder = textureOrder;

                FrameTimesMs = new double[MeasuredFrames];
                SubmitTimesMs = new double[MeasuredFrames];
                AllocatedBytes = new double[MeasuredFrames];
            }

            public int SpriteCount { get; }

            public int TextureCount { get; }

            public TextureOrder TextureOrder { get; }

            public double[] FrameTimesMs { get; }

            public double[] SubmitTimesMs { get; }

            public double[] AllocatedBytes { get; }
        }
    }
}
