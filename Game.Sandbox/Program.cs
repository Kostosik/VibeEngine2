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
using Engine.Serialization.Content;
using Engine.Serialization.UI;
using Engine.Tooling.Debugging;
using Engine.UI.Actions;
using Engine.UI.Assets;
using Engine.UI.Core;
using Engine.UI.Styling;
using Game.Sandbox;

var actionMap =
    new InputActionMap();

var resetAction =
    new InputAction("Reset");

var spawnAction =
    new InputAction("Spawn");

var clearAction =
    new InputAction("Clear");

var forceAction =
    new InputAction("Force");

var triggerAction =
    new InputAction("ToggleTrigger");

var rotateAction =
    new InputAction("Rotate");

var moveUp =
    new InputAction("MoveUp");

var moveDown =
    new InputAction("MoveDown");

var moveLeft =
    new InputAction("MoveLeft");

var moveRight =
    new InputAction("MoveRight");

var switchModeAction =
    new InputAction("SwitchMode");

actionMap.Bind(
    moveUp,
    new InputBinding("Keyboard.W"));

actionMap.Bind(
    moveDown,
    new InputBinding("Keyboard.S"));

actionMap.Bind(
    moveLeft,
    new InputBinding("Keyboard.A"));

actionMap.Bind(
    moveRight,
    new InputBinding("Keyboard.D"));

actionMap.Bind(
    switchModeAction,
    new InputBinding("Keyboard.F2"));

actionMap.Bind(
    resetAction,
    new InputBinding("Keyboard.F3"));

actionMap.Bind(
    spawnAction,
    new InputBinding("Keyboard.F4"));

actionMap.Bind(
    clearAction,
    new InputBinding("Keyboard.F5"));

actionMap.Bind(
    forceAction,
    new InputBinding("Keyboard.Space"));

actionMap.Bind(
    triggerAction,
    new InputBinding("Keyboard.T"));

actionMap.Bind(
    rotateAction,
    new InputBinding("Keyboard.R"));

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

using var content =
    new ContentManager(
        assetSource,
        contentCatalog,
        contentLoaders);

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

DebugConsoleOverlay? console =
    null;

if (runtime.Console is not null)
{
    var consoleScroll =
        new InputAction(
            "ConsoleScroll");

    actionMap.Bind(
        consoleScroll,
        new InputBinding(
            "Mouse.Scroll"));

    console =
        new DebugConsoleOverlay(
            runtime.Console,
            window.TextInput,
            input,
            consoleScroll,
            runtime.Services.Graphics,
            1280,
            720);

    window.Resized +=
        console.Resize;
}

var sandbox =
    new SandboxState(
        runtime,
        input,
        window.Camera,
        window.GraphicsDevice,
        ui,
        tileAtlas,
        moveUp,
        moveDown,
        moveLeft,
        moveRight,
        switchModeAction,
        resetAction,
        spawnAction,
        clearAction,
        forceAction,
        triggerAction,
        rotateAction,
        console);

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