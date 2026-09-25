using Engine.ECS.Entities;
using Engine.Editor.Commands;
using Engine.Editor.Inspection;
using Engine.Editor.Selection;
using Engine.Editor.Viewport;
using Engine.Tooling.Validation;
using Engine.Worlds;

namespace Engine.Editor.Documents;

public sealed class EditorDocument
{
    public EditorViewport Viewport { get; }
    public EditorInspector Inspector { get; }
    public EditorPropertyProviderRegistry PropertyProviders { get; }
    public EditorDocument(
        World world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        World = world;

        EntitySelection =
            new SelectionSet<EntityId>();

        CommandHistory =
            new EditorCommandHistory();

        Validation =
            new ValidationService();

        PropertyProviders =
            new EditorPropertyProviderRegistry();

        PropertyProviders.Register(
            new ReflectionPropertyProvider());

        Viewport =
    new EditorViewport();

        PropertyProviders.Register(
            new EcsComponentPropertyProvider());

        Inspector =
            new EditorInspector(
                world,
                PropertyProviders);
    }

    public World World { get; }

    public SelectionSet<EntityId> EntitySelection { get; }

    public EditorCommandHistory CommandHistory { get; }

    public ValidationService Validation { get; }

    public bool IsDirty =>
        CommandHistory.IsDirty;

    public void Execute(
        IEditorCommand command)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        CommandHistory.Execute(
            command);
    }

    public void Undo()
    {
        CommandHistory.Undo();
    }

    public void Redo()
    {
        CommandHistory.Redo();
    }

    public void MarkSaved()
    {
        CommandHistory.MarkSaved();
    }
}