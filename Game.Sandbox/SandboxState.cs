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
using Engine.Localization;
using Engine.Runtime;
using Engine.Serialization.SaveLoad.Game;
using Engine.Simulations;
using Engine.Tooling.Debugging;
using Engine.UI.Core;
using Game.Sandbox.Scenarios;
using Game.Sandbox.UI;

namespace Game.Sandbox;

public sealed class SandboxState
{
    private enum SandboxMode
    {
        TopDown,
        Physics
    }
    private readonly LocalizationManager _localization;
    private readonly EngineRuntime _runtime;
    private readonly IInput _input;
    private readonly Camera2D _camera;
    private readonly IGraphicsDevice _graphics;
    private readonly UiSystem _ui;
    private readonly DebugConsoleOverlay? _console;
    private readonly SandboxMultiplayerProbe? _multiplayerProbe;
    private readonly SandboxMultiplayerReplicationProbe?
    _multiplayerReplicationProbe;
    private readonly SandboxPlayerReplicationProbe _playerReplicationProbe;
    private readonly UiShowcaseWindow _uiShowcase;

    private readonly RenderTargetHandle _renderTarget;
    private readonly SandboxHud _hud;

    private readonly SandboxNetworkProbe _networkProbe;

    private TopDownScenario? _topDown;
    private PhysicsScenario? _physics;
    private readonly Func<SandboxScriptProbe?>? _scriptProbeProvider;
    private readonly Engine.Graphics.Resources.TextureAtlas _tileAtlas;

    private SandboxMode _mode =
        SandboxMode.TopDown;

    private int _width = 1280;
    private int _height = 720;

    private readonly GameSaveLoadService _saveLoad;
    private readonly string _savePath;
    private readonly SandboxMultiplayerPlayerProbe?
    _multiplayerPlayerProbe;
    private readonly SandboxReplicationProbe _networkReplicationProbe;

    private readonly SandboxInputActions _actions;

    private string _serializationStatus =
        "READY";
    public SandboxState(
    EngineRuntime runtime,
    UiSystem ui,
    Engine.Graphics.Resources.TextureAtlas tileAtlas,
    SandboxInputActions actions,
    GameSaveLoadService saveLoad,
    DebugConsoleOverlay? console = null,
    Func<SandboxScriptProbe?>? scriptProbeProvider = null, SandboxNetworkRoleOptions? networkRoleOptions = null)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(ui);
        ArgumentNullException.ThrowIfNull(tileAtlas);
        ArgumentNullException.ThrowIfNull(actions);
        ArgumentNullException.ThrowIfNull(saveLoad);

        _saveLoad =
    saveLoad;
        _scriptProbeProvider = scriptProbeProvider;
        _localization =
    CreateLocalizationManager();

        _graphics = runtime.Services.Graphics;
        _input = runtime.Services.Input;
        _camera = runtime.Services.Camera;

        _actions = actions;
        _savePath =
            Path.Combine(
                AppContext.BaseDirectory,
                "Saves",
                "sandbox.vbe");

        _runtime = runtime;
        _input = runtime.Services.Input;
        _ui = ui;
        _console = console;

        _uiShowcase =
            new UiShowcaseWindow(
                ui,
                tileAtlas.Texture);

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
                _camera,
                _input,
                tileAtlas,
                _actions.MoveUp,
                _actions.MoveDown,
                _actions.MoveLeft,
                _actions.MoveRight, _actions.CollectResource);

        _topDown.InteractionChanged +=
    _hud.SetInteraction;

        _physics = null;

        _networkProbe =
    new SandboxNetworkProbe();

        _networkProbe.StatusChanged +=
            _hud.SetNetworkStatus;

        _hud.SetNetworkStatus(
            _networkProbe.Status);

        _networkReplicationProbe =
    new SandboxReplicationProbe();

        _networkReplicationProbe.StatusChanged +=
            _hud.SetNetworkReplicationStatus;

        _hud.SetNetworkReplicationStatus(
            _networkReplicationProbe.Status);

        _playerReplicationProbe =
    new SandboxPlayerReplicationProbe();

        _playerReplicationProbe.StatusChanged +=
            _hud.SetPlayerReplicationStatus;

        _hud.SetPlayerReplicationStatus(
            _playerReplicationProbe.Status);

        _multiplayerProbe = networkRoleOptions is null
    ? null
    : new SandboxMultiplayerProbe(networkRoleOptions);

        if (_multiplayerProbe is null)
        {
            _hud.SetMultiplayerStatus("MULTIPLAYER ROLE: OFF");
        }
        else
        {
            _multiplayerProbe.StatusChanged +=
                _hud.SetMultiplayerStatus;

            _multiplayerProbe.Start();
        }

        if (networkRoleOptions is null)
        {
            _multiplayerReplicationProbe = null;

            _hud.SetMultiplayerReplicationStatus(
                "MULTIPLAYER REPLICATION: OFF");
        }
        else
        {
            if (networkRoleOptions.Port > 65515 ||
                networkRoleOptions.LocalPort > 65515)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(networkRoleOptions),
                    "Replication ports exceed the valid UDP port range.");
            }

            var replicationOptions = networkRoleOptions with
            {
                Port = networkRoleOptions.Port + 10,
                LocalPort = networkRoleOptions.LocalPort + 10
            };

            _multiplayerReplicationProbe =
                new SandboxMultiplayerReplicationProbe(
                    replicationOptions);

            _multiplayerReplicationProbe.StatusChanged +=
                _hud.SetMultiplayerReplicationStatus;

            _multiplayerPlayerProbe = networkRoleOptions is null
    ? null
    : new SandboxMultiplayerPlayerProbe(networkRoleOptions);

            if (_multiplayerPlayerProbe is not null)
            {
                _multiplayerPlayerProbe.StatusChanged +=
                    _hud.SetMultiplayerPlayerStatus;

                _multiplayerPlayerProbe.Start(
                    _runtime.EcsWorld,
                    () => _topDown?.Player ?? default);
            }
            else
            {
                _hud.SetMultiplayerPlayerStatus(
                    "PLAYER STREAM: OFF");
            }

            _hud.SetMultiplayerReplicationStatus(
                _multiplayerReplicationProbe.Status);

            _multiplayerReplicationProbe.Start(
                _runtime.EcsWorld);
        }

        _runtime.PhysicsDebugVisualizer!.Enabled =
            true;
    }

    public void Update(
        TimeSnapshot time)
    {
        _runtime.Profiler?.RecordFrame();

        using var profiler =
            _runtime.Profiler?.BeginScope(
                "Sandbox.Update");

        using (_runtime.Profiler?.BeginScope(
                   "Sandbox.NetworkProbe.Update"))
        {
            _networkProbe.Update();
            _multiplayerProbe?.Update();
            _multiplayerReplicationProbe?.Update();
            _multiplayerPlayerProbe?.Update();
        }

        using (_runtime.Profiler?.BeginScope(
                   "Sandbox.ReplicationProbe.Update"))
        {
            _networkReplicationProbe.Update();
        }

        using (_runtime.Profiler?.BeginScope(
                   "Sandbox.PlayerReplicationProbe.Update"))
        {
            _playerReplicationProbe.Update();
        }

        _console?.Update();

        // Сначала UI обрабатывает мышь, фокус
        // и ввод текста.
        _ui.Update(
            time.Delta.TotalSeconds);

        if (_console?.IsOpen == true)
        {
            _topDown?.StopMovement();

            return;
        }

        // F8 остаётся глобальной командой,
        // чтобы окно можно было закрыть и при фокусе в TextBox.
        if (_input.IsPressed(
                _actions.ToggleUiShowcase))
        {
            if (_uiShowcase.IsOpen)
            {
                _uiShowcase.Close();
            }
            else
            {
                _uiShowcase.Show(
                    new Vector2(
                        660.0f,
                        40.0f));
            }
        }

        // Если UI принимает клавиатурный ввод,
        // игровые горячие клавиши не выполняются.
        if (_ui.ConsumesKeyboardInput)
        {
            _topDown?.StopMovement();
            return;
        }
        if (_input.IsPressed(
        _actions.ToggleLocale))
        {
            var nextLocale =
                _localization.CurrentLocale.Language == "ru"
                    ? new Locale("en")
                    : new Locale("ru");

            _localization.SetLocale(
                nextLocale);
        }

        if (_input.IsPressed(
        _actions.PlayerReplicationTest))
        {
            if (_mode == SandboxMode.TopDown &&
                _topDown is not null)
            {
                _playerReplicationProbe.RunTest(
                    _runtime.EcsWorld,
                    _topDown.Player);
            }
            else
            {
                _playerReplicationProbe.ShowUnavailable();
            }
        }
        if (_input.IsPressed(
        _actions.NetworkTest))
        {
            _networkProbe.RunTest();
        }

        if (_input.IsPressed(
                _actions.PlayAudio))
        {
            _topDown?.PlayTestSound();
        }

        if (_input.IsPressed(
                _actions.SwitchMode))
        {
            SwitchMode();

            return;
        }

        if (_input.IsPressed(
        _actions.NetworkReplicationTest))
        {
            _networkReplicationProbe.RunTest();
        }

        if (_input.IsPressed(
                _actions.Reset))
        {
            ResetCurrentScenario();
        }

        if (_input.IsPressed(
                _actions.Spawn))
        {
            SpawnStressBatch();
        }

        if (_input.IsPressed(
                _actions.Clear))
        {
            ClearStressBatch();
        }

        if (_input.IsPressed(
                _actions.ToggleTrigger))
        {
            ToggleTrigger();
        }

        if (_input.IsPressed(
                _actions.Save))
        {
            SaveGame();
        }

        if (_input.IsPressed(
                _actions.Load))
        {
            LoadGame();
        }

        if (_input.IsPressed(
                _actions.Rotate))
        {
            RotateObstacle();
        }

        if (_input.IsPressed(
                _actions.Force))
        {
            ApplyForce();
        }

        if (_mode ==
            SandboxMode.TopDown)
        {
            _topDown?.Update();
        }
        else
        {
            _topDown?.StopMovement();
        }
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
        if (_playerReplicationProbe.IsRunning)
        {
            _playerReplicationProbe.Cancel();
        }

        if (_mode !=
            SandboxMode.TopDown)
        {
            _serializationStatus =
                "LOAD: TOP-DOWN ONLY";

            return;
        }

        try
        {
            _runtime.Physics.ResetContactState();

            _saveLoad.Load(
                _savePath,
                _runtime.World);

            _topDown?.RefreshAfterWorldRestore();
            _runtime.Scripts.RebuildInstancesFromWorld();

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
    public void Render(
    double interpolationAlpha)
    {
        UpdateHud();
        using var profiler =
            _runtime.Profiler?.BeginScope(
                "Sandbox.Render");

        if (_mode ==
            SandboxMode.TopDown)
        {
            _topDown?.Render(
    interpolationAlpha);

            if (_mode == SandboxMode.TopDown &&
    _multiplayerPlayerProbe?.RemotePosition is { } remotePosition)
            {
                _topDown?.RenderRemotePlayer(remotePosition);
            }
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

    public void FixedUpdate(
        SimulationTime time)
    {
        if (_mode ==
            SandboxMode.TopDown)
        {
            _topDown?.BeginFixedStep();
            _topDown?.FixedUpdate(
                time.Delta.ToFloat());
        }
    }

    private void DisposeCurrentScenario()
    {
        if (_playerReplicationProbe.IsRunning)
        {
            _playerReplicationProbe.Cancel();
        }

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
                _actions.MoveUp,
                _actions.MoveDown,
                _actions.MoveLeft,
                _actions.MoveRight, _actions.CollectResource);

        scenario.InteractionChanged +=
            _hud.SetInteraction;

        return scenario;
    }

    private void ResetCurrentScenario()
    {
        if (_playerReplicationProbe.IsRunning)
        {
            _playerReplicationProbe.Cancel();
        }

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

        var navigationText =
    _mode == SandboxMode.TopDown &&
    _topDown is not null &&
    _topDown.HasNavigationPath
        ? _localization.Format(
            "sandbox.navigation.path",
            _topDown.NavigationPathPointCount,
            _topDown.NavigationPathCost)
        : _localization.Get(
            "sandbox.navigation.unavailable");

        var scriptProbe =
            _scriptProbeProvider?.Invoke();

        var scriptStatus =
            scriptProbe is null
                ? "SCRIPT: OFF"
                : $"{scriptProbe.DisplayName} | " +
                  $"U: {scriptProbe.UpdateCount} | " +
                  $"F: {scriptProbe.FixedUpdateCount}";

        _hud.SetIntegrationStatus(
            $"{navigationText} | " +
            _localization.Get("sandbox.locale.hint") +
            $" | {scriptStatus}");

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

    private static LocalizationManager CreateLocalizationManager()
    {
        var localization =
            new LocalizationManager(
                new Locale("en"));

        var english =
            new LocalizationTable(
                new Locale("en"));

        english.Set(
            "sandbox.navigation.path",
            "NAV [EN]: {0} nodes, cost {1:0.0}");

        english.Set(
            "sandbox.navigation.unavailable",
            "NAV [EN]: path unavailable");

        english.Set(
            "sandbox.locale.hint",
            "L: language");

        var russian =
            new LocalizationTable(
                new Locale("ru"));

        russian.Set(
            "sandbox.navigation.path",
            "НАВИГАЦИЯ [RU]: узлов {0}, цена {1:0.0}");

        russian.Set(
            "sandbox.navigation.unavailable",
            "НАВИГАЦИЯ [RU]: путь недоступен");

        russian.Set(
            "sandbox.locale.hint",
            "L: язык");

        localization.AddTable(english);
        localization.AddTable(russian);

        localization.SetFallbackLocale(
            new Locale("en"));

        return localization;
    }

    public void Shutdown()
    {
        var failures = new List<Exception>();

        TryCleanup(failures, DisposeCurrentScenario);

        var multiplayerProbe = _multiplayerProbe;
        if (multiplayerProbe is not null)
        {
            TryCleanup(failures, () =>
                multiplayerProbe.StatusChanged -=
                    _hud.SetMultiplayerStatus);

            TryCleanup(failures, multiplayerProbe.Dispose);
        }

        var multiplayerPlayerProbe = _multiplayerPlayerProbe;
        if (multiplayerPlayerProbe is not null)
        {
            TryCleanup(failures, () =>
                multiplayerPlayerProbe.StatusChanged -=
                    _hud.SetMultiplayerPlayerStatus);

            TryCleanup(failures, multiplayerPlayerProbe.Dispose);
        }

        var multiplayerReplicationProbe =
            _multiplayerReplicationProbe;

        if (multiplayerReplicationProbe is not null)
        {
            TryCleanup(failures, () =>
                multiplayerReplicationProbe.StatusChanged -=
                    _hud.SetMultiplayerReplicationStatus);

            TryCleanup(failures, multiplayerReplicationProbe.Dispose);
        }

        TryCleanup(failures, () =>
            _playerReplicationProbe.StatusChanged -=
                _hud.SetPlayerReplicationStatus);

        TryCleanup(failures, _playerReplicationProbe.Dispose);

        TryCleanup(failures, () =>
            _networkReplicationProbe.StatusChanged -=
                _hud.SetNetworkReplicationStatus);

        TryCleanup(failures, _networkReplicationProbe.Dispose);

        TryCleanup(failures, () =>
            _networkProbe.StatusChanged -=
                _hud.SetNetworkStatus);

        TryCleanup(failures, _networkProbe.Dispose);

        TryCleanup(failures, _uiShowcase.Close);

        TryCleanup(failures, () =>
            _ui.Root.RemoveChild(_hud));

        TryCleanup(failures, () =>
            _graphics.RenderTargets.Destroy(_renderTarget));

        if (failures.Count > 0)
        {
            throw new AggregateException(
                "Sandbox shutdown encountered cleanup failures.",
                failures);
        }
    }

    private static void TryCleanup(
        List<Exception> failures,
        Action cleanup)
    {
        try
        {
            cleanup();
        }
        catch (Exception exception)
        {
            if (exception is AggregateException aggregate)
            {
                failures.AddRange(
                    aggregate.Flatten().InnerExceptions);
            }
            else
            {
                failures.Add(exception);
            }
        }
    }
}