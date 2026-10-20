using Editor.Sandbox;
using Engine.Content;
using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Application;
using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.Editor;
using Engine.Editor.Documents;
using Engine.Editor.Documents.Persistence;
using Engine.Editor.Persistence;
using Engine.Editor.Projects;
using Engine.Editor.UI;
using Engine.Editor.UI.Authoring;
using Engine.Graphics.Fonts;
using Engine.Graphics.OpenGL;
using Engine.Serialization.SaveLoad.Ecs;
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

var debugRegistry =
    new DebugCommandRegistry();

var debugConsole =
    new DebugConsole(
        debugRegistry);

debugRegistry.Register(
    new HelpDebugCommand(
        debugRegistry));

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

var editor =
    new EditorContext();

var editorAssetRoot =
    Path.Combine(
        AppContext.BaseDirectory,
        "Assets");

editor.InitializeAssetBrowser(
    new Engine.Editor.Assets.FileSystemAssetSource(),
    editorAssetRoot);

var projectStateStore =
    new EditorProjectStateStore();

// Сохраняем путь при создании, открытии
// или переключении активного игрового проекта.
editor.ActiveGameProjectChanged += project =>
{
    try
    {
        projectStateStore.SaveLastProjectManifestPath(
            project?.ManifestFilePath);
    }
    catch (Exception exception)
    {
        debugConsole.WriteException(
            exception,
            "Failed to save editor project state.");
    }
};

// Восстанавливаем последний активный проект.
// Ошибка восстановления не должна закрывать редактор.
try
{
    var lastProjectPath =
        projectStateStore.LoadLastProjectManifestPath();

    if (!string.IsNullOrWhiteSpace(lastProjectPath))
    {
        try
        {
            var project = GameProject.Open(lastProjectPath);

            editor.SetActiveGameProject(project);

            debugConsole.WriteLine(
                $"Restored game project: {project.Name}");
        }
        catch (Exception exception)
        {
            debugConsole.WriteException(
                exception,
                "Failed to restore the last game project.");
        }
    }
}
catch (Exception exception)
{
    debugConsole.WriteException(
        exception,
        "Failed to load editor state.");
}

editor.ComponentTypes.RegisterEngineComponents();

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

var serializers =
    new EcsComponentSerializerRegistry();

serializers.RegisterEngineComponents();

var documentPersistence =
    new BinaryEditorDocumentPersistence(
        serializers);

var documentFiles =
    editor.CreateDocumentFileService(
        documentPersistence);

var inspection =
    new WorldInspectionService(
        () =>
            (editor.Session.ActiveEditorDocument as EditorDocument)
                ?.World.EcsWorld.Inspector);

debugRegistry.Register(
    new EntitiesDebugCommand(
        inspection));

debugRegistry.Register(
    new InspectDebugCommand(
        inspection));

var fileDialogs =
    new WindowsFileDialogService();

var application =
    new EditorApplication(
        editor,
        ui,
        window.InputBackend,
        assetPreviewTextures,
        fileDialogs,
        documentPersistence,
        documentFiles,
        debugConsole: debugConsole, filePreviewTextureManager:
    window.GraphicsDevice.Textures);


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

var toolsMenu = new UiContextMenu(ui.Overlays);

toolsMenu.AddItem(
    "Console",
    application.ToggleConsole);

Action<UiRect> toolsRequested = bounds =>
{
    toolsMenu.Open(
        new Vector2(
            bounds.X,
            bounds.Bottom));
};

application.MainShell.ToolsRequested +=
    toolsRequested;

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

Action<UiRect> viewRequested =
    bounds =>
    {
        viewMenu.Open(
            new Vector2(
                bounds.X,
                bounds.Bottom));
    };

var editMenu =
    new UiContextMenu(
        ui.Overlays);

Action<UiRect> editRequested =
    bounds =>
    {
        editMenu.Open(
            new Vector2(
                bounds.X,
                bounds.Bottom));
    };

application.MainShell.EditRequested +=
    editRequested;

editMenu.AddItem(
    "Undo",
    application.MainShell.Undo,
    () => application.MainShell.CanUndo);

editMenu.AddItem(
    "Redo",
    application.MainShell.Redo,
    () => application.MainShell.CanRedo);

application.MainShell.ViewRequested +=
    viewRequested;

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
    application.MainShell.ToolsRequested -=
        toolsRequested;
    application.MainShell.ViewRequested -=
    viewRequested;

    application.MainShell.EditRequested -=
    editRequested;

    if (!gameLoop.IsShutdown)
    {
        gameLoop.Shutdown();
    }
}