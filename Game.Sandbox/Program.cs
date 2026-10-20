using Engine.Content.Loading;
using Engine.Core.Application;
using Engine.Core.Assets;
using Engine.Graphics.Fonts;
using Engine.Graphics.Resources;
using Engine.Hosting.Desktop;
using Engine.Input;
using Engine.Scripting;
using Engine.Serialization.Content;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Game;
using Engine.Serialization.UI;
using Engine.Tooling.Debugging;
using Engine.UI.Assets;
using Engine.UI.Core;
using Engine.UI.Styling;
using Game.Sandbox;

if (args.Any(
        arg => string.Equals(
            arg,
            "--engine-game-probe",
            StringComparison.OrdinalIgnoreCase)))
{
    DesktopGameHost.Run(
        runtime => new EngineGameProbe(runtime),
        new DesktopGameHostOptions
        {
            Title = "VibeEngine EngineGame Probe",
            Width = 1280,
            Height = 720
        });

    return;
}

var sandboxActions = new SandboxInputActions();

DesktopGameHost.Run(
    (DesktopGameHostContext context) =>
        CreateSandboxApplication(
            context,
            sandboxActions),
    new DesktopGameHostOptions
    {
        Title = "VibeEngine2 Sandbox",
        Width = 1280,
        Height = 720,

        InputActions = sandboxActions.ActionMap,

        ConfigureContentLoaders = loaders =>
        {
            loaders.Register(
                new BinaryContentLoader<UiAsset>(
                    new UiAssetSerializer(),
                    asset => asset.Extension.Equals(
                        ".ui",
                        StringComparison.OrdinalIgnoreCase)));

            loaders.Register(
                new JsonScriptDefinitionContentLoader());
        }
    });

static IApplication CreateSandboxApplication(
    DesktopGameHostContext context,
    SandboxInputActions sandboxActions)
{
    var runtime = context.Runtime;

    // Sandbox-specific content.
    var mainMenuAsset =
        context.Content.Load<UiAsset>(
            new AssetPath("UI/MainMenu.ui"));

    var scriptDefinition =
        context.Content.Load<ScriptDefinition>(
            new AssetPath("Scripts/ScriptProbe.script.json"));

    Console.WriteLine(
        $"Content loaded: {mainMenuAsset.Name} " +
        $"({mainMenuAsset.Elements.Count} elements)");

    var fontData =
        context.AssetSource.Load(
            new AssetPath("Fonts/ARIAL.ttf"));

    var defaultFont =
        context.Graphics.Fonts.Create(
            fontData,
            new FontDescription(24));

    var resources =
        new TextureResourceManager(
            context.Content,
            context.Graphics.Textures);

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
            context.PointerInput
            ?? throw new InvalidOperationException(
                "The selected host does not support pointer input.");

        var ui = new UiSystem(
            context.Graphics,
            1280,
            720,
            pointerInput,
            context.TextInput,
            context.Cursor,
            uiTheme);

        context.Resized += ui.Resize;

        DebugConsoleOverlay? console = null;

        if (runtime.Console is not null)
        {
            console = new DebugConsoleOverlay(
                runtime.Console,
                context.TextInput,
                context.Input,
                sandboxActions.ConsoleScroll,
                context.Graphics,
                1280,
                720);

            context.Resized += console.Resize;
        }

        // Register and attach the Sandbox-only script probe.
        SandboxScriptProbe? scriptProbe = null;

        runtime.ScriptRegistry.Register(
            scriptDefinition.ScriptId,
            () =>
            {
                var instance = new SandboxScriptProbe();
                scriptProbe = instance;
                return instance;
            });

        var scriptProbeEntity =
            runtime.EcsWorld.CreateEntity();

        runtime.Scripts.Attach(
            scriptProbeEntity,
            scriptDefinition);

        Console.WriteLine(
            $"Script attached: {scriptDefinition.ScriptId}");

        // Sandbox-specific game persistence.
        var ecsSerializers =
            new EcsComponentSerializerRegistry();

        ecsSerializers.RegisterEngineComponents();

        var persistentStates =
            new PersistentStateRegistry();

        var saveLoad =
            new GameSaveLoadService(
                ecsSerializers,
                persistentStates);

        var sandbox = new SandboxState(
            runtime,
            ui,
            tileAtlas,
            sandboxActions,
            saveLoad,
            console,
            () => scriptProbe);

        context.Resized += sandbox.Resize;

        return new SandboxApplication(
            sandbox,
            resources);
    }
    catch
    {
        resources.Dispose();
        throw;
    }
}