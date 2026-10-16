using Engine.Audio;
using Engine.Audio.OpenAL;
using Engine.Content;
using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Application;
using Engine.Core.Assets;
using Engine.Graphics.Fonts;
using Engine.Graphics.OpenGL;
using Engine.Graphics.Resources;
using Engine.Input;
using Engine.Runtime;
using Engine.Scripting;
using Engine.Serialization.Content;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Game;
using Engine.Serialization.Types;
using Engine.Serialization.UI;
using Engine.Tooling.Debugging;
using Engine.UI.Actions;
using Engine.UI.Assets;
using Engine.UI.Core;
using Engine.UI.Styling;
using Game.Sandbox;

var sandboxActions =
    new SandboxInputActions();

var actionMap =
    sandboxActions.ActionMap;

using var window =
    new OpenGLWindow(
        1280,
        720,
        "VibeEngine2 Sandbox");

window.Initialize();

var input =
    new ActionInput(
        window.InputBackend,
        actionMap);

var pointerInput =
    window.InputBackend as IPointerInput
    ?? throw new InvalidOperationException(
        "Input backend does not support pointer input.");

var assetSource =
    new FileAssetSource(
        Path.Combine(
            AppContext.BaseDirectory,
            "Assets"));

var fontData =
    assetSource.Load(
        new AssetPath(
            "Fonts/ARIAL.ttf"));

var defaultFont =
    window.GraphicsDevice.Fonts.Create(
        fontData,
        new FontDescription(
            24));

var contentCatalog =
    new FileContentCatalog(
        Path.Combine(
            AppContext.BaseDirectory,
            "Assets"));

var contentLoaders =
    new ContentLoaderRegistry();

contentLoaders.Register(
    new ImageTextureContentLoader());

contentLoaders.Register(
    new BinaryContentLoader<UiAsset>(
        new UiAssetSerializer(),
        asset =>
            asset.Extension.Equals(
                ".ui",
                StringComparison.OrdinalIgnoreCase)));

contentLoaders.Register(
    new JsonScriptDefinitionContentLoader());

using var content =
    new ContentManager(
        assetSource,
        contentCatalog,
        contentLoaders);

var mainMenuAsset =
    content.Load<UiAsset>(
        new AssetPath(
            "UI/MainMenu.ui"));

var scriptDefinition =
    content.Load<ScriptDefinition>(
        new AssetPath(
            "Scripts/ScriptProbe.script.json"));

Console.WriteLine(
    $"Content loaded: {mainMenuAsset.Name} " +
    $"({mainMenuAsset.Elements.Count} elements)");

using var resources =
    new TextureResourceManager(
        content,
        window.GraphicsDevice.Textures);

var tileAtlas =
    resources.LoadAtlas(
        new AssetPath(
            "Textures/test.png"),
        32,
        32);

var uiTheme =
    new UiTheme
    {
        DefaultFont =
            defaultFont
    };

var ui =
    new UiSystem(
        window.GraphicsDevice,
        1280,
        720,
        pointerInput,
        window.TextInput,
        window.Cursor,
        uiTheme);

window.Resized +=
    ui.Resize;

using var audioDevice =
    new OpenALAudioDevice();

using var audio =
    new AudioManager(
        assetSource,
        audioDevice);

var runtimeServices =
    new EngineRuntimeServices(
        window.GraphicsDevice,
        input,
        window.Camera,
        audio,
        content);

using var runtime =
    new EngineRuntime(
        new EngineRuntimeOptions(),
        runtimeServices);

var scriptProbe =
    new SandboxScriptProbe();

runtime.ScriptRegistry.Register(
    scriptDefinition.ScriptId,
    () => scriptProbe);

var scriptProbeEntity =
    runtime.EcsWorld.CreateEntity();

runtime.Scripts.Attach(
    scriptProbeEntity,
    scriptDefinition);

Console.WriteLine(
    $"Script attached: {scriptDefinition.ScriptId}");

DebugConsoleOverlay? console =
    null;

if (runtime.Console is not null)
{
    console =
        new DebugConsoleOverlay(
            runtime.Console,
            window.TextInput,
            input,
            sandboxActions.ConsoleScroll,
            runtime.Services.Graphics,
            1280,
            720);

    window.Resized +=
        console.Resize;
}

var ecsSerializers =
    new EcsComponentSerializerRegistry();

ecsSerializers.RegisterEngineComponents();

var persistentStates =
    new PersistentStateRegistry();

var saveLoad =
    new GameSaveLoadService(
        ecsSerializers,
        persistentStates);

var sandbox =
    new SandboxState(
        runtime,
        ui,
        tileAtlas,
        sandboxActions,
        saveLoad,
        console,scriptProbe);

var application =
    new StatefulApplication(
        sandbox);

var gameLoop =
    new GameLoop(
        application,
        runtime.SimulationRate);

runtime.AttachGameLoopController(
    gameLoop);

gameLoop.Initialize();

window.Resized +=
    sandbox.Resize;

try
{
    window.Run(
        gameLoop);
}
finally
{
    if (!gameLoop.IsShutdown)
    {
        gameLoop.Shutdown();
    }
}