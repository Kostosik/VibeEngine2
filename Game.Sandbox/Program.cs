using Engine.Audio;
using Engine.Audio.OpenAL;
using Engine.Core.Application;
using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Graphics.Fonts;
using Engine.Graphics.OpenGL;
using Engine.Graphics.Resources;
using Engine.Input;
using Engine.Physics.Components;
using Engine.Physics.Shapes;
using Engine.Runtime;
using Engine.Tooling.Debugging;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Styling;
using Engine.Worlds.Spatial;
using Engine.Worlds.Tiles;
using Game.Sandbox.Gameplay;
using Game.Sandbox.States;

var actionMap =
    new InputActionMap();

var moveUp =
    new InputAction("MoveUp");

var moveDown =
    new InputAction("MoveDown");

var moveLeft =
    new InputAction("MoveLeft");

var moveRight =
    new InputAction("MoveRight");

var zoomIn =
    new InputAction("ZoomIn");

var zoomOut =
    new InputAction("ZoomOut");

var interaction = new InputAction("Interact");

actionMap.Bind(interaction, new InputBinding("Keyboard.F"));

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
    zoomIn,
    new InputBinding("Keyboard.Q"));

actionMap.Bind(
    zoomOut,
    new InputBinding("Keyboard.E"));

var consoleScroll =
    new InputAction(
        "ConsoleScroll");

actionMap.Bind(
    consoleScroll,
    new InputBinding(
        "Mouse.Scroll"));

using var window =
    new OpenGLWindow(
        1280,
        720,
        "VibeEngine");

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
            32));

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
window.Resized += ui.Resize;
using var resources =
    new TextureResourceManager(
        assetSource,
        window.GraphicsDevice.Textures);

var tileAtlas =
    resources.LoadAtlas(
        new AssetPath(
            "Textures/test.png"),
        32,
        32);

using var audioDevice =
    new OpenALAudioDevice();

using var audio =
    new AudioManager(
        assetSource,
        audioDevice);

var playerMoveSound =
    audio.Load(
        new AssetPath(
            "Sounds/smoke.wav"));

var runtimeServices =
    new EngineRuntimeServices(
        window.GraphicsDevice,
        input,
        window.Camera,
        audio);

using var runtime =
    new EngineRuntime(
        new EngineRuntimeOptions(),
        runtimeServices);

var player =
    runtime.EcsWorld.CreateEntity();

var interactionTargets =
    new List<EntityId>();

var terminal =
    runtime.EcsWorld.CreateEntity();

runtime.EcsWorld.Add(
    terminal,
    new Transform2D(
        new FixedVector2(
            Fixed32.FromInt(20),
            Fixed32.FromInt(16))));

runtime.EcsWorld.Add(
    terminal,
    new InteractionTarget(
        Fixed32.FromInt(2)));

interactionTargets.Add(
    terminal);

runtime.EcsWorld.Add(
    player,
    new Transform2D(
        new FixedVector2(
            Fixed32.FromInt(16),
            Fixed32.FromInt(16))));

runtime.EcsWorld.Add(
    player,
    PhysicsBody2D.Dynamic(
        Fixed32.One));

runtime.EcsWorld.Add(
    player,
    new Collider2D(
        new AabbShape2D(
            new FixedVector2(
                Fixed32.One,
                Fixed32.One))));

using var playerMovementSubscription =
    runtime.Simulation.CommandDispatcher.Register(
        new PlayerMovementCommandHandler(
            runtime.EcsWorld,
            Fixed32.FromInt(4)));

runtime.Simulation.CommandDispatcher.Register(
    new InteractCommandHandler(
        runtime.EcsWorld));

var walls =
    new List<EntityId>();

walls.Add(
    CreateStaticWall(
        runtime.EcsWorld,
        new FixedVector2(
            Fixed32.FromFloat(-0.5f),
            Fixed32.FromInt(16)),
        new FixedVector2(
            Fixed32.One,
            Fixed32.FromInt(32))));

walls.Add(
    CreateStaticWall(
        runtime.EcsWorld,
        new FixedVector2(
            Fixed32.FromFloat(32.5f),
            Fixed32.FromInt(16)),
        new FixedVector2(
            Fixed32.One,
            Fixed32.FromInt(32))));

walls.Add(
    CreateStaticWall(
        runtime.EcsWorld,
        new FixedVector2(
            Fixed32.FromInt(16),
            Fixed32.FromFloat(-0.5f)),
        new FixedVector2(
            Fixed32.FromInt(32),
            Fixed32.One)));

walls.Add(
    CreateStaticWall(
        runtime.EcsWorld,
        new FixedVector2(
            Fixed32.FromInt(16),
            Fixed32.FromFloat(32.5f)),
        new FixedVector2(
            Fixed32.FromInt(32),
            Fixed32.One)));
DebugConsoleOverlay? consoleOverlay = null;

if (runtime.Console is not null)
{
    consoleOverlay =
        new DebugConsoleOverlay(
            runtime.Console,
            window.TextInput,
            input,
            consoleScroll,
            runtime.Services.Graphics,
            1280,
            720);

    window.Resized +=
        consoleOverlay.Resize;
}

IGameLoopController? gameLoopController = null;

var gameplayState =
    new GameplayState(
        runtime,
        runtime.Simulation,
        runtime.Services.Input,
        runtime.Services.Camera,
        runtime.Services.Graphics,
        runtime.EcsWorld,
        runtime.World,
        tileAtlas,
        moveUp,
        moveDown,
        moveLeft,
        moveRight,
        zoomIn,
        zoomOut,interaction,
        player,walls,interactionTargets,
        runtime.Services.Audio,
        playerMoveSound, ui,
window.TextInput,
() => gameLoopController?.Pause(),
() => gameLoopController?.Resume(), consoleOverlay);
var application =
    new StatefulApplication(
        gameplayState);

var gameLoop =
    new GameLoop(
        application,
        runtime.SimulationRate);

gameLoopController =
    gameLoop;

runtime.AttachGameLoopController(
    gameLoop);

var chunk =
    runtime.World.CreateChunk(
        new ChunkPosition(
            0,
            0));

chunk.Tiles.Fill(
    new Tile(2));

runtime.Services.Camera.Position =
    new Vector2(
        16,
        16);

runtime.Services.Camera.Zoom =
    32.0f;

gameLoop.Initialize();

try
{
    window.Run(
        gameLoop);
}
finally
{
    gameLoop.Shutdown();
}


static EntityId CreateStaticWall(
    Engine.ECS.World world,
    FixedVector2 position,
    FixedVector2 size)
{
    var entity =
        world.CreateEntity();

    world.Add(
        entity,
        new Transform2D(
            position));

    world.Add(
        entity,
        PhysicsBody2D.Static());

    world.Add(
        entity,
        new Collider2D(
            new AabbShape2D(
                size)));

    return entity;
}