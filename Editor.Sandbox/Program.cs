using Editor.Sandbox;
using Engine.Content;
using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Application;
using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.Editor;
using Engine.Editor.Documents.Persistence;
using Engine.Editor.UI;
using Engine.Editor.UI.Authoring;
using Engine.Graphics.Fonts;
using Engine.Graphics.OpenGL;
using Engine.Input;
using Engine.Serialization.Binary;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.Types;
using Engine.Tooling.Debugging;
using Engine.Tooling.Inspection;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;
using Engine.UI.Styling;
using Engine.Worlds;
using Engine.Worlds.Spatial;

using var window =
    new OpenGLWindow(
        1280,
        720,
        "VibeEngine Editor");

window.Initialize();

var actionMap =
    new InputActionMap();

var consoleScroll =
    new InputAction(
        "ConsoleScroll");

actionMap.Bind(
    consoleScroll,
    new InputBinding(
        "Mouse.Scroll"));

var input =
    new ActionInput(
        window.InputBackend,
        actionMap);

var debugRegistry =
    new DebugCommandRegistry();

var debugConsole =
    new DebugConsole(
        debugRegistry);



debugRegistry.Register(
    new HelpDebugCommand(
        debugRegistry));



var consoleOverlay =
    new DebugConsoleOverlay(
        debugConsole,
        window.TextInput,
        input,
        consoleScroll,
        window.GraphicsDevice,
        1280,
        720);





window.Resized +=
    consoleOverlay.Resize;

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

var toolsMenu =
    new UiContextMenu(
        ui.Overlays);

toolsMenu.AddItem(
    "Console",
    consoleOverlay.Toggle);

consoleOverlay.Opened +=
    ui.Focus.ClearFocus;

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

var inspection =
    new WorldInspectionService(
        ecsWorld.Inspector);

debugRegistry.Register(
    new EntitiesDebugCommand(
        inspection));

debugRegistry.Register(
    new InspectDebugCommand(
        inspection));

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

var documentFiles =
    editor.CreateDocumentFileService(
        documentPersistence);

var worldPath =
    Path.Combine(
        AppContext.BaseDirectory,
        "Worlds",
        "Sandbox.world");

Directory.CreateDirectory(
    Path.GetDirectoryName(
        worldPath)!);

var fileDialogs =
    new WindowsFileDialogService();

var application =
    new EditorApplication(
        editor,
        ui,
        window.InputBackend,
        assetPreviewTextures,
        uiDocument,
        uiAssetPath, fileDialogs,
        documentPersistence,
        documentFiles);

var toolingApplication =
    new EditorToolingApplication(
        application,
        consoleOverlay);

window.CloseRequested +=
    application.RequestApplicationClose;

application.ApplicationCloseRequested +=
    window.Close;

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

Action<UiRect> toolsRequested =
    bounds =>
    {
        toolsMenu.Open(
            new Vector2(
                bounds.X,
                bounds.Bottom));
    };

var viewMenu =
    new UiContextMenu(
        ui.Overlays);

viewMenu.AddItem(
    "Hierarchy",
    () =>
    {
        editor.Workspace.SetPanelOpen(
            "Hierarchy",
            !editor.Workspace.FindPanel(
                "Hierarchy")!.IsOpen);

        application.MainShell.Refresh();
    });

viewMenu.AddItem(
    "Viewport",
    () =>
    {
        editor.Workspace.SetPanelOpen(
            "Viewport",
            !editor.Workspace.FindPanel(
                "Viewport")!.IsOpen);

        application.MainShell.Refresh();
    });

viewMenu.AddItem(
    "Inspector",
    () =>
    {
        editor.Workspace.SetPanelOpen(
            "Inspector",
            !editor.Workspace.FindPanel(
                "Inspector")!.IsOpen);

        application.MainShell.Refresh();
    });

viewMenu.AddItem(
    "Asset Browser",
    () =>
    {
        editor.Workspace.SetPanelOpen(
            "AssetBrowser",
            !editor.Workspace.FindPanel(
                "AssetBrowser")!.IsOpen);

        application.MainShell.Refresh();
    });

viewMenu.AddItem(
    "Asset Preview",
    () =>
    {
        editor.Workspace.SetPanelOpen(
            "AssetPreview",
            !editor.Workspace.FindPanel(
                "AssetPreview")!.IsOpen);

        application.MainShell.Refresh();
    });

Action<UiRect> viewRequested =
    bounds =>
    {
        viewMenu.Open(
            new Vector2(
                bounds.X,
                bounds.Bottom));
    };

application.MainShell.ViewRequested +=
    viewRequested;

application.MainShell.ToolsRequested +=
    toolsRequested;

var gameLoop =
    new GameLoop(
        toolingApplication,
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
    application.MainShell.ToolsRequested -=
        toolsRequested;

    consoleOverlay.Opened -=
        ui.Focus.ClearFocus;
    application.MainShell.ViewRequested -=
    viewRequested;
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