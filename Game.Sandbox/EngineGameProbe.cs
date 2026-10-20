using Engine.Core.Time;
using Engine.Graphics.Commands;
using Engine.Graphics.DebugGraphics;
using Engine.Runtime;
using Engine.Core.Math;

namespace Game.Sandbox;

/// <summary>
/// Small diagnostic application used to verify the EngineGame
/// lifecycle independently from SandboxState.
/// </summary>
internal sealed class EngineGameProbe : EngineGame
{
    private bool _reportedUpdate;
    private bool _reportedFixedUpdate;

    public EngineGameProbe(EngineRuntime runtime)
        : base(runtime)
    {
    }

    protected override void OnInitialize()
    {
        var entity = Runtime.EcsWorld.CreateEntity();

        Console.WriteLine(
            $"EngineGame initialized. Probe entity: {entity}");
    }

    protected override void OnUpdate(TimeSnapshot time)
    {
        if (_reportedUpdate)
        {
            return;
        }

        _reportedUpdate = true;

        Console.WriteLine(
            $"EngineGame update received. Delta: " +
            $"{time.Delta.TotalMilliseconds:F2} ms");
    }

    protected override void OnRender(double interpolationAlpha)
    {
        Runtime.Services.Graphics.Submit(
            new DrawDebugTextCommand(
                "EngineGame is running",
                new Vector2(24.0f, 24.0f),
                DebugColor.White,
                2.0f,
                DebugRenderSpace.Screen));
    }

    protected override void OnFixedUpdate(SimulationTime time)
    {
        if (_reportedFixedUpdate)
        {
            return;
        }

        _reportedFixedUpdate = true;

        Console.WriteLine(
            $"EngineGame fixed update received. Tick: {time.Tick}");
    }

    protected override void OnShutdown()
    {
        Console.WriteLine("EngineGame shutdown.");
    }
}