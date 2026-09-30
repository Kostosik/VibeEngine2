using Engine.Content;
using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Application;
using Engine.Core.Assets;
using Engine.Editor;
using Engine.Editor.Documents.Persistence;
using Engine.Editor.UI;
using Engine.Editor.UI.Authoring;
using Engine.Graphics.Fonts;
using Engine.Graphics.OpenGL;
using Engine.Serialization.Binary;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.Types;
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
    world.SpatialEntities.CreateEntity(
        new WorldPosition(
            0,
            0));

ecsWorld.Add(
    entity,
    new EditorTestComponent(
        100,
        42.5f));

var editor =
    new EditorContext();

var editorAssetRoot =
    Path.Combine(
        AppContext.BaseDirectory,
        "Assets");

editor.InitializeAssetBrowser(
    new Engine.Editor.Assets.FileSystemAssetSource(),
    editorAssetRoot);

editor.ComponentTypes.Register<EditorTestComponent>();

var contentCatalog =
    new FileContentCatalog(
        Path.Combine(
            AppContext.BaseDirectory,
            "Assets"));

var contentLoaders =
    new ContentLoaderRegistry();

contentLoaders.Register(
    new Engine.Graphics.Resources.ImageTextureContentLoader());

using var content =
    new ContentManager(
        assetSource,
        contentCatalog,
        contentLoaders);

using var assetPreviewTextures =
    new Engine.Graphics.Resources.TextureResourceManager(
        content,
        window.GraphicsDevice.Textures);

var uiAssetPath =
    Path.Combine(
        AppContext.BaseDirectory,
        "Assets",
        "UI",
        "MainMenu.ui");

var uiDocument =
    File.Exists(uiAssetPath)
        ? EditorUiAssetFile.Load(
            uiAssetPath)
        : new EditorUiDocument(
            "Main UI");

var serializers =
    new EcsComponentSerializerRegistry();

serializers.Register(
    "engine.world.position",
    new WorldPositionComponentSerializer());

serializers.Register(
    "sandbox.editor_test_component",
    new EditorTestComponentSerializer());

var documentPersistence =
    new BinaryEditorDocumentPersistence(
        serializers);

var worldPath =
    Path.Combine(
        AppContext.BaseDirectory,
        "Worlds",
        "Sandbox.world");

Directory.CreateDirectory(
    Path.GetDirectoryName(
        worldPath)!);

var application =
    new EditorApplication(
        editor,
        ui,
        window.InputBackend,
        assetPreviewTextures,
        uiDocument,
        uiAssetPath,
        documentPersistence);

application.NewDocumentRequested +=
    () =>
    {
        var newEcsWorld =
            new Engine.ECS.World();

        var newWorld =
            new World(
                new ChunkSize(
                    32,
                    32),
                newEcsWorld);

        application.OpenDocument(
            newWorld);
    };

var document =
    application.OpenDocument(
        world,
        worldPath);

document.EntitySelection.Changed +=
    () =>
    {
        Console.WriteLine(
            "[SELECTION] Changed");

        Console.WriteLine(
            $"[SELECTION] Count=" +
            $"{document.EntitySelection.Count}");

        foreach (var selected in
                 document.EntitySelection.Items)
        {
            Console.WriteLine(
                $"[SELECTION] Entity={selected}");
        }
    };

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
    if (!gameLoop.IsShutdown)
    {
        gameLoop.Shutdown();
    }
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

public sealed class EditorTestComponentSerializer :
    IBinarySerializer<EditorTestComponent>
{
    public void Serialize(
        ref SerializationWriter writer,
        EditorTestComponent value)
    {
        writer.WriteInt32(
            value.Value);

        writer.WriteSingle(
            value.Speed);
    }

    public EditorTestComponent Deserialize(
        ref SerializationReader reader)
    {
        return new EditorTestComponent(
            reader.ReadInt32(),
            reader.ReadSingle());
    }
}