using Engine.Core.Math;
using Engine.Editor.Commands;
using Engine.Editor.Documents;
using Engine.Editor.Inspection;
using Engine.Physics.Components;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class EcsComponentPropertyProviderTests
{

    [Fact]
    public void InspectorPropertyEdit_TracksDirtyStateAndUndoRedo()
    {
        using var ecsWorld = new Engine.ECS.World();

        var world = new World(
            new ChunkSize(16, 16),
            ecsWorld);

        var entity = ecsWorld.CreateEntity();

        ecsWorld.Add(
            entity,
            PhysicsBody2D.Dynamic(Fixed32.One));

        using var document = new EditorDocument(world);

        var mass = Assert.Single(
            document.Inspector.GetProperties(
                entity,
                typeof(PhysicsBody2D)),
            property => property.Name == nameof(PhysicsBody2D.Mass));

        var newMass = Fixed32.FromInt(3);

        // Изменение через историю команд документа.
        document.Execute(
            new SetEditorPropertyCommand(mass, newMass));

        Assert.Equal(
            newMass,
            ecsWorld.Get<PhysicsBody2D>(entity).Mass);

        Assert.True(document.IsDirty);
        Assert.True(document.CommandHistory.CanUndo);

        // Undo возвращает исходное значение.
        document.Undo();

        Assert.Equal(
            Fixed32.One,
            ecsWorld.Get<PhysicsBody2D>(entity).Mass);

        Assert.False(document.IsDirty);
        Assert.True(document.CommandHistory.CanRedo);

        // Redo повторяет изменение.
        document.Redo();

        Assert.Equal(
            newMass,
            ecsWorld.Get<PhysicsBody2D>(entity).Mass);

        Assert.True(document.IsDirty);

        // Сохранённое состояние становится новой точкой отсчёта.
        document.MarkSaved();

        Assert.False(document.IsDirty);

        // Отмена после сохранения снова помечает документ изменённым.
        document.Undo();

        Assert.Equal(
            Fixed32.One,
            ecsWorld.Get<PhysicsBody2D>(entity).Mass);

        Assert.True(document.IsDirty);
    }

    [Fact]
    public void PhysicsBodyMassAndInertia_AreEditableAndUndoable()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var entity =
            ecsWorld.CreateEntity();

        ecsWorld.Add(
            entity,
            PhysicsBody2D.Dynamic(
                Fixed32.One));

        using var document =
            new EditorDocument(world);

        var properties =
            document.Inspector.GetProperties(
                entity,
                typeof(PhysicsBody2D));

        var mass =
            Assert.Single(
                properties.Where(
                    property =>
                        property.Name ==
                        nameof(PhysicsBody2D.Mass)));

        var inertia =
            Assert.Single(
                properties.Where(
                    property =>
                        property.Name ==
                        nameof(PhysicsBody2D.Inertia)));

        Assert.False(mass.IsReadOnly);
        Assert.False(inertia.IsReadOnly);

        var newMass =
            Fixed32.FromInt(3);

        var setMass =
            new SetEditorPropertyCommand(
                mass,
                newMass);

        setMass.Execute();

        Assert.Equal(
            newMass,
            ecsWorld.Get<PhysicsBody2D>(
                entity).Mass);

        setMass.Undo();

        Assert.Equal(
            Fixed32.One,
            ecsWorld.Get<PhysicsBody2D>(
                entity).Mass);

        var newInertia =
            Fixed32.FromInt(2);

        var setInertia =
            new SetEditorPropertyCommand(
                inertia,
                newInertia);

        setInertia.Execute();

        Assert.Equal(
            newInertia,
            ecsWorld.Get<PhysicsBody2D>(
                entity).Inertia);

        setInertia.Undo();

        Assert.Equal(
            Fixed32.One,
            ecsWorld.Get<PhysicsBody2D>(
                entity).Inertia);
    }

    [Fact]
    public void PhysicsBodyMassAndInertia_RejectNonPositiveValues()
    {
        var body =
            PhysicsBody2D.Dynamic(
                Fixed32.One);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => body.Mass = Fixed32.Zero);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => body.Mass = Fixed32.FromInt(-1));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => body.Inertia = Fixed32.Zero);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => body.Inertia = Fixed32.FromInt(-1));
    }

    [Fact]
    public void GetProperties_PrivateSetterIsReadOnly()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var entity =
            ecsWorld.CreateEntity();

        ecsWorld.Add(
            entity,
            new TestComponent());

        var target =
            new EcsComponentEditorTarget(
                world,
                entity,
                typeof(TestComponent));

        var provider =
            new EcsComponentPropertyProvider();

        var properties =
            provider.GetProperties(target);

        var editableProperty =
            Assert.Single(
                properties.Where(
                    property =>
                        property.Name ==
                        nameof(TestComponent.EditableValue)));

        var privateSetterProperty =
            Assert.Single(
                properties.Where(
                    property =>
                        property.Name ==
                        nameof(TestComponent.ReadOnlyValue)));

        Assert.False(editableProperty.IsReadOnly);
        Assert.True(privateSetterProperty.IsReadOnly);
    }

    private struct TestComponent
    {
        public int EditableValue { get; set; }

        public int ReadOnlyValue { get; private set; }
    }
}