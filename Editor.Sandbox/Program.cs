using Engine.Core.Application;
using Engine.Core.Assets;
using Engine.Editor;
using Engine.Editor.UI;
using Engine.Graphics.Fonts;
using Engine.Graphics.OpenGL;
using Engine.UI.Core;
using Engine.UI.Styling;
using Engine.Worlds;
using Engine.Worlds.Spatial;

using var window =
    new OpenGLWindow(
        1280,
        720,
        "VibeEngine Editor");

window.Initialize();

var pointerInput =
    window.InputBackend as Engine.Input.IPointerInput
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

window.Resized +=
    ui.Resize;

using var ecsWorld =
    new Engine.ECS.World();

var world =
    new World(
        new ChunkSize(
            32,
            32),
        ecsWorld);

var entity =
    ecsWorld.CreateEntity();

ecsWorld.Add(
    entity,
    new EditorTestComponent(
        100,
        42.5f));
world.SpatialEntities.CreateEntity(
    new WorldPosition(0, 0));

var editor =
    new EditorContext();

var application =
    new EditorApplication(
        editor,
        ui);

application.OpenDocument(
    world);

var gameLoop =
    new GameLoop(
        application,
        new Engine.Core.Time.SimulationRate(
            60));

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

public struct EditorTestComponent
{
    public EditorTestComponent(
        int value,
        float speed)
    {
        Value = value;
        Speed = speed;
    }

    public int Value { get; set; }

    public float Speed { get; set; }
}