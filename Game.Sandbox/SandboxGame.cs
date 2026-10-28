
using Engine.Core.Assets;
using Engine.Core.Time;
using Engine.Graphics.Fonts;
using Engine.Graphics.Resources;
using Engine.Hosting.Desktop;
using Engine.Runtime;
using Engine.Scripting;
using Engine.Serialization.Content;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Game;
using Engine.Serialization.UI;
using Engine.Tooling.Debugging;
using Engine.UI.Assets;
using Engine.UI.Core;
using Engine.UI.Styling;
using Game.Sandbox.Serialization;
using Engine.Core.Events;
using Engine.UI.Actions;

namespace Game.Sandbox;

internal sealed class SandboxGame : EngineGame, IDisposable
{
    private readonly DesktopGameHostContext _context;
    private readonly SandboxInputActions _sandboxActions;
    private readonly SandboxNetworkRoleOptions? _networkRoleOptions;
    private TextureResourceManager? _resources;
    private SandboxState? _sandbox;
    private SandboxScriptProbe? _scriptProbe;
    private bool _sandboxShutdown;
    private bool _disposed;
    private UiSystem? _ui;
    private UiActionRuntime? _uiActionRuntime;
    private UiAssetInstance? _mainMenuInstance;
    private EventSubscription? _toggleShowcaseSubscription;
    private sealed record ToggleUiShowcaseRequestedEvent : IEvent;
    public SandboxGame(
        DesktopGameHostContext context,
        SandboxInputActions sandboxActions,
        SandboxNetworkRoleOptions? networkRoleOptions = null)
        : base(
            (context ?? throw new ArgumentNullException(nameof(context)))
            .Runtime)
    {
        ArgumentNullException.ThrowIfNull(sandboxActions);

        _context = context;
        _sandboxActions = sandboxActions;
        _networkRoleOptions = networkRoleOptions;
    }

    protected override void OnInitialize()
    {
        var mainMenuAsset =
            _context.Content.Load<UiAsset>(
                new AssetPath("UI/MainMenu.ui"));

        var scriptDefinition =
            _context.Content.Load<ScriptDefinition>(
                new AssetPath("Scripts/ScriptProbe.script.json"));

        Console.WriteLine(
            $"Content loaded: {mainMenuAsset.Name} " +
            $"({mainMenuAsset.Elements.Count} elements)");

        var fontData =
            _context.AssetSource.Load(
                new AssetPath("Fonts/ARIAL.ttf"));

        var defaultFont =
            _context.Graphics.Fonts.Create(
                fontData,
                new FontDescription(24));

        var resources =
            new TextureResourceManager(
                _context.Content,
                _context.Graphics.Textures);

        _resources = resources;

        try
        {
            var tileAtlas =
                resources.LoadAtlas(
                    new AssetPath("Textures/test.png"),
                    32,
                    32);

            var uiTheme = new UiTheme
            {
                DefaultFont = defaultFont
            };

            var pointerInput =
                _context.PointerInput
                ?? throw new InvalidOperationException(
                    "The selected host does not support pointer input.");

            var ui = new UiSystem(
                _context.Graphics,
                1280,
                720,
                pointerInput,
                _context.TextInput,
                _context.Cursor,
                uiTheme);

            _ui = ui;

            _uiActionRuntime = new UiActionRuntime(
                Runtime.Events,
                resources);

            _uiActionRuntime.Register(
                "Sandbox.ToggleUiShowcase",
                () => new ToggleUiShowcaseRequestedEvent());

            _toggleShowcaseSubscription =
                Runtime.Events.Subscribe<ToggleUiShowcaseRequestedEvent>(
                    _ => _sandbox?.ToggleUiShowcase());

            _mainMenuInstance =
                _uiActionRuntime.Load(mainMenuAsset);

            _context.Resized += ui.Resize;

            DebugConsoleOverlay? console = null;

            if (Runtime.Console is not null)
            {
                console = new DebugConsoleOverlay(
                    Runtime.Console,
                    _context.TextInput,
                    _context.Input,
                    _sandboxActions.ConsoleScroll,
                    _context.Graphics,
                    1280,
                    720);

                _context.Resized += console.Resize;
            }

            // Sandbox-only script registration.
            Runtime.ScriptRegistry.Register(
                scriptDefinition.ScriptId,
                () =>
                {
                    var instance = new SandboxScriptProbe();
                    _scriptProbe = instance;
                    return instance;
                });

            var scriptProbeEntity =
                Runtime.EcsWorld.CreateEntity();

            Runtime.Scripts.Attach(
                scriptProbeEntity,
                scriptDefinition);

            Console.WriteLine(
                $"Script attached: {scriptDefinition.ScriptId}");

            // Sandbox-only save/load configuration.
            var ecsSerializers =
                new EcsComponentSerializerRegistry();

            ecsSerializers.RegisterEngineComponents();

            ecsSerializers.Register(
    "game.sandbox.resource-inventory",
    new ResourceInventoryComponentSerializer());

            ecsSerializers.Register(
                "game.sandbox.resource-node",
                new ResourceNodeComponentSerializer());

            var persistentStates =
                new PersistentStateRegistry();

            var saveLoad =
                new GameSaveLoadService(
                    ecsSerializers,
                    persistentStates);

            _sandbox = new SandboxState(
                Runtime,
                ui,
                tileAtlas,
                _sandboxActions,
                saveLoad,
                console,
                () => _scriptProbe,
                _networkRoleOptions);
            ui.Root.AddChild(
    _mainMenuInstance.Root);
            _context.Resized += _sandbox.Resize;
        }
        catch
        {
            resources.Dispose();
            _resources = null;
            throw;
        }
    }

    protected override void OnUpdate(TimeSnapshot time)
    {
        _sandbox!.Update(time);
    }

    protected override void OnFixedUpdate(SimulationTime time)
    {
        _sandbox!.FixedUpdate(time);
    }

    protected override void OnRender(double interpolationAlpha)
    {
        _sandbox!.Render(interpolationAlpha);
    }

    protected override void OnShutdown()
    {
        ShutdownSandbox();

        Console.WriteLine("VibeEngine2 Sandbox game stopped.");
    }

    private void ShutdownSandbox()
    {
        if (_sandboxShutdown)
        {
            return;
        }

        _sandboxShutdown = true;

        try
        {
            _sandbox?.Shutdown();
        }
        finally
        {
            _toggleShowcaseSubscription?.Dispose();
            _toggleShowcaseSubscription = null;

            if (_ui is not null &&
                _mainMenuInstance is not null)
            {
                _ui.Root.RemoveChild(
                    _mainMenuInstance.Root);
            }

            _mainMenuInstance = null;

            _uiActionRuntime?.Dispose();
            _uiActionRuntime = null;

            _ui = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            // Also clean up if initialization failed before
            // the normal game-loop shutdown sequence.
            ShutdownSandbox();
        }
        finally
        {
            _resources?.Dispose();
            _resources = null;
        }
    }
}
