using Engine.Camera;
using Engine.Core.Diagnostics.Metrics;
using Engine.Core.Math;
using Engine.Core.Time;
using Engine.Graphics;
using Engine.Graphics.Commands;
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;
using Engine.Graphics2D.Rendering;
using Engine.Input;
using Engine.Runtime;
using Engine.Serialization.SaveLoad.Game;
using Engine.Simulations;
using Engine.Tooling.Debugging;
using Engine.UI.Core;
using Engine.Worlds.Chunks;
using Game.Sandbox.Scenarios;

namespace Game.Sandbox;

public sealed class SandboxState :
    SimulationState
{
    private enum SandboxMode
    {
        TopDown,
        Physics
    }

    private readonly EngineRuntime _runtime;
    private readonly IInput _input;
    private readonly Camera2D _camera;
    private readonly IGraphicsDevice _graphics;
    private readonly UiSystem _ui;
    private readonly DebugConsoleOverlay? _console;

    private readonly InputAction _switchModeAction;
    private readonly InputAction _resetAction;
    private readonly InputAction _spawnAction;
    private readonly InputAction _clearAction;
    private readonly InputAction _forceAction;
    private readonly InputAction _triggerAction;
    private readonly InputAction _rotateAction;

    private readonly InputAction _moveUp;
    private readonly InputAction _moveDown;
    private readonly InputAction _moveLeft;
    private readonly InputAction _moveRight;

    private readonly RenderTargetHandle _renderTarget;
    private readonly SandboxHud _hud;

    private TopDownScenario? _topDown;
    private PhysicsScenario? _physics;

    private readonly Engine.Graphics.Resources.TextureAtlas _tileAtlas;

    private SandboxMode _mode =
        SandboxMode.TopDown;

    private int _width = 1280;
    private int _height = 720;

    private readonly GameSaveLoadService _saveLoad;

    private readonly InputAction _saveAction;
    private readonly InputAction _loadAction;

    private readonly string _savePath;

    private string _serializationStatus =
        "READY";

    public SandboxState(
        EngineRuntime runtime,
        IInput input,
        Camera2D camera,
        IGraphicsDevice graphics,
        UiSystem ui,
        Engine.Graphics.Resources.TextureAtlas tileAtlas,
        InputAction moveUp,
        InputAction moveDown,
        InputAction moveLeft,
        InputAction moveRight,
        InputAction switchModeAction,
        InputAction resetAction,
        InputAction spawnAction,
        InputAction clearAction,
        InputAction forceAction,
        InputAction triggerAction,
        InputAction rotateAction, GameSaveLoadService saveLoad,
InputAction saveAction,
InputAction loadAction,
        DebugConsoleOverlay? console = null)
        : base(runtime.Simulation)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(ui);
        ArgumentNullException.ThrowIfNull(tileAtlas);
        ArgumentNullException.ThrowIfNull(
    saveLoad);

        ArgumentNullException.ThrowIfNull(
            saveAction);

        ArgumentNullException.ThrowIfNull(
            loadAction);

        _saveLoad =
    saveLoad;

        _saveAction =
            saveAction;

        _loadAction =
            loadAction;

        _savePath =
            Path.Combine(
                AppContext.BaseDirectory,
                "Saves",
                "sandbox.vbe");

        _runtime = runtime;
        _input = input;
        _camera = camera;
        _graphics = graphics;
        _ui = ui;
        _console = console;

        _switchModeAction = switchModeAction;
        _resetAction = resetAction;
        _spawnAction = spawnAction;
        _clearAction = clearAction;
        _forceAction = forceAction;
        _triggerAction = triggerAction;
        _rotateAction = rotateAction;
        _tileAtlas = tileAtlas;
        _renderTarget =
            _graphics.RenderTargets.Create(
                new RenderTargetDescription(
                    _width,
                    _height));

        _graphics.Pipeline.Clear();

        _graphics.Pipeline.AddPass(
            new RenderPass(
                "World",
                _renderTarget,
                RenderState.Default,
                true,
                new RenderPassLayerRange(
                    RenderLayers.World,
                    RenderLayers.Present - 1)));

        _graphics.Pipeline.AddPass(
            RenderPass2D.Present);

        _graphics.Pipeline.AddPass(
            RenderPass2D.Ui);

        _graphics.Pipeline.AddPass(
            RenderPass2D.Debug);

        _hud =
            new SandboxHud();

        _ui.Root.AddChild(
            _hud);

        _topDown =
            new TopDownScenario(
                runtime,
                camera,
                input,
                tileAtlas,
                moveUp,
                moveDown,
                moveLeft,
                moveRight);

        _topDown.InteractionChanged +=
    _hud.SetInteraction;

        _physics = null;

        _moveUp = moveUp;
        _moveDown = moveDown;
        _moveLeft = moveLeft;
        _moveRight = moveRight;

        _runtime.PhysicsDebugVisualizer!.Enabled =
            true;
    }

    public override void Update(
    TimeSnapshot time)
    {
        _runtime.Profiler?.RecordFrame();

        using var profiler =
            _runtime.Profiler?.BeginScope(
                "Sandbox.Update");

        _input.Update();

        _console?.Update();

        if (_console?.IsOpen == true)
        {
            _topDown?.StopMovement();

            _ui.Update(
                time.Delta.TotalSeconds);

            base.Update(time);

            return;
        }

        if (_input.IsPressed(
                _switchModeAction))
        {
            SwitchMode();

            return;
        }

        if (_input.IsPressed(
                _resetAction))
        {
            ResetCurrentScenario();
        }

        if (_input.IsPressed(
                _spawnAction))
        {
            SpawnStressBatch();
        }

        if (_input.IsPressed(
                _clearAction))
        {
            ClearStressBatch();
        }

        if (_input.IsPressed(
                _triggerAction))
        {
            ToggleTrigger();
        }

        if (_input.IsPressed(
        _saveAction))
        {
            SaveGame();
        }

        if (_input.IsPressed(
                _loadAction))
        {
            LoadGame();
        }

        if (_input.IsPressed(
                _rotateAction))
        {
            RotateObstacle();
        }

        if (_input.IsPressed(
                _forceAction))
        {
            ApplyForce();
        }

        if (_mode ==
            SandboxMode.TopDown)
        {
            if (!_ui.ConsumesKeyboardInput)
            {
                _topDown?.Update();
            }
            else
            {
                _topDown?.StopMovement();
            }
        }
        else
        {
            _topDown?.StopMovement();
        }

        _ui.Update(
            time.Delta.TotalSeconds);

        base.Update(time);

        UpdateHud();
    }

    private void SaveGame()
    {
        if (_mode !=
            SandboxMode.TopDown)
        {
            _serializationStatus =
                "SAVE: TOP-DOWN ONLY";

            return;
        }

        try
        {
            var directory =
                Path.GetDirectoryName(
                    _savePath);

            if (!string.IsNullOrWhiteSpace(
                    directory))
            {
                Directory.CreateDirectory(
                    directory);
            }

            _saveLoad.Save(
                _savePath,
                _runtime.World);

            _serializationStatus =
                $"SAVE OK  {_runtime.World.ChunkCount} chunks";
        }
        catch (Exception exception)
        {
            _serializationStatus =
                $"SAVE FAILED  {exception.GetType().Name}";

            Console.WriteLine(
                exception);
        }
    }

    private void LoadGame()
    {
        if (_mode !=
            SandboxMode.TopDown)
        {
            _serializationStatus =
                "LOAD: TOP-DOWN ONLY";

            return;
        }

        try
        {
            PrepareWorldForLoad();

            _runtime.Physics.ResetContactState();

            _saveLoad.Load(
                _savePath,
                _runtime.World);

            _topDown?.RefreshAfterWorldRestore();

            _serializationStatus =
                $"LOAD OK  {_runtime.World.ChunkCount} chunks";
        }
        catch (Exception exception)
        {
            _serializationStatus =
                $"LOAD FAILED  {exception.GetType().Name}";

            Console.WriteLine(
                exception);
        }
    }

    private void PrepareWorldForLoad()
    {
        _runtime.World.SpatialIndex.Clear();

        foreach (var record
                 in _runtime.World
                     .GetChunkRecords()
                     .ToArray())
        {
            if (record.Chunk is not null &&
                record.Lifecycle.Simulation !=
                    ChunkSimulationState.Suspended)
            {
                record.Lifecycle.SetSimulation(
                    ChunkSimulationState.Suspended);
            }

            _runtime.World.RemoveChunk(
                record.Lifecycle.Position);
        }
    }

    public override void Render(
    double interpolationAlpha)
    {
        using var profiler =
            _runtime.Profiler?.BeginScope(
                "Sandbox.Render");

        if (_mode ==
            SandboxMode.TopDown)
        {
            _topDown?.Render(
    interpolationAlpha);
        }
        else
        {
            _camera.Position =
                new Vector2(
                    120.0f,
                    12.0f);

            _camera.Zoom =
                28.0f;
        }

        _graphics.Submit(
            new DrawRenderTargetCommand(
                _renderTarget,
                new Vector2(
                    0.0f,
                    0.0f),
                new Vector2(
                    _width,
                    _height),
                new Rectangle(
                    0.0f,
                    0.0f,
                    1.0f,
                    1.0f)));

        _runtime.RenderDebugVisualization();

        _ui.Render();

        _console?.Render();
    }

    public override void FixedUpdate(
        SimulationTime time)
    {
        if (_mode ==
            SandboxMode.TopDown)
        {
            _topDown?.BeginFixedStep();
            _topDown?.FixedUpdate();
        }

        base.FixedUpdate(
            time);
    }

    private void DisposeCurrentScenario()
    {
        if (_mode ==
            SandboxMode.TopDown)
        {
            if (_topDown is not null)
            {
                _topDown.InteractionChanged -=
                    _hud.SetInteraction;

                _topDown.Dispose();
                _topDown = null;
            }
        }
        else
        {
            _physics?.Dispose();
            _physics = null;
        }

        _runtime.Simulation.CommandDispatcher.ClearPending();
    }

    private void SwitchMode()
    {
        DisposeCurrentScenario();

        if (_mode ==
            SandboxMode.TopDown)
        {
            _physics =
                new PhysicsScenario(
                    _runtime);

            _mode =
                SandboxMode.Physics;

            _camera.Position =
                new Vector2(
                    120.0f,
                    12.0f);

            _camera.Zoom =
                28.0f;

            return;
        }

        _topDown =
            CreateTopDownScenario();

        _mode =
            SandboxMode.TopDown;

        _camera.Position =
            new Vector2(
                0.0f,
                0.0f);

        _camera.Zoom =
            24.0f;
    }

    private TopDownScenario CreateTopDownScenario()
    {
        var scenario =
            new TopDownScenario(
                _runtime,
                _camera,
                _input,
                _tileAtlas,
                _moveUp,
                _moveDown,
                _moveLeft,
                _moveRight);

        scenario.InteractionChanged +=
            _hud.SetInteraction;

        return scenario;
    }

    private void ResetCurrentScenario()
    {
        if (_mode ==
            SandboxMode.TopDown)
        {
            _topDown.Reset();
        }
        else
        {
            _physics.Reset();
        }
    }

    private void SpawnStressBatch()
    {
        if (_mode ==
            SandboxMode.TopDown)
        {
            _topDown.SpawnStressBatch();
        }
        else
        {
            _physics.SpawnStressBatch();
        }
    }

    private void ClearStressBatch()
    {
        if (_mode ==
            SandboxMode.TopDown)
        {
            _topDown.ClearSpawnedEntities();
        }
        else
        {
            _physics.ClearSpawnedEntities();
        }
    }

    private void ApplyForce()
    {
        if (_mode ==
            SandboxMode.Physics)
        {
            _physics.ApplyForce();
        }
    }

    private void ToggleTrigger()
    {
        if (_mode ==
            SandboxMode.TopDown)
        {
            _topDown.ToggleTrigger();
        }
        else
        {
            _physics.ToggleTrigger();
        }
    }

    private void RotateObstacle()
    {
        if (_mode ==
            SandboxMode.TopDown)
        {
            _topDown.RotateObstacle();
        }
        else
        {
            _physics.RotatePolygon();
        }
    }

    private void UpdateHud()
    {
        var modeText =
            _mode ==
            SandboxMode.TopDown
                ? "TOP-DOWN"
                : "PHYSICS";

        _hud.SetMode(
            modeText);

        var status =
            _mode ==
            SandboxMode.TopDown
                ? _topDown?.GetStatus() ?? "TOP-DOWN NOT ACTIVE"
                : _physics?.GetStatus() ?? "PHYSICS NOT ACTIVE";

        _hud.SetStatus(
            $"{status}\n" +
            $"Save/Load      {_serializationStatus}");



        var openGlGraphics =
            _graphics as Engine.Graphics.OpenGL.OpenGLGraphicsDevice;

        if (openGlGraphics is null)
        {
            return;
        }
        Metric updateMetric = new Metric();
        Metric renderMetric = new Metric();

        _runtime.Profiler?.TryGetMetric(
            "Sandbox.Update",
            out updateMetric);

        _runtime.Profiler?.TryGetMetric(
            "Sandbox.Render",
            out renderMetric);

        _hud.SetPerformance(
            $"FPS: {_runtime.Profiler?.FramesPerSecond ?? 0}\n" +
            $"Frame: {_runtime.Profiler?.LastFrameTime.TotalMilliseconds:F2} ms\n" +
            $"Update: {updateMetric.Value:F2} ms\n" +
            $"Render Submit: {renderMetric.Value:F2} ms\n" +
            $"EndFrame: {openGlGraphics.LastEndFrameTime.TotalMilliseconds:F2} ms");
    }

    public void Resize(
        int width,
        int height)
    {
        if (width <= 0 ||
            height <= 0)
        {
            return;
        }

        _width = width;
        _height = height;

        _graphics.RenderTargets.Resize(
            _renderTarget,
            width,
            height);
    }

    public override void Shutdown()
    {
        DisposeCurrentScenario();

        _ui.Root.RemoveChild(
            _hud);

        _graphics.RenderTargets.Destroy(
            _renderTarget);

        base.Shutdown();
    }
}