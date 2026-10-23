using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.Editor;
using Engine.Editor.Documents;
using Engine.Editor.UI.Workspace;
using Engine.Input;
using Engine.Particles;
using Engine.Physics.Components;
using Engine.Physics.Shapes;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class InspectorComponentPickerTests
{
    [Fact]
    public void Inspector_AddsAndEditsParticleEffectComponent()
    {
        using var ecsWorld = new Engine.ECS.World();

        using var world = new World(
            new ChunkSize(16, 16),
            ecsWorld);

        var editor = new EditorContext();
        editor.ComponentTypes.RegisterEngineComponents();

        var entity = ecsWorld.CreateEntity();

        // Эмиттеру нужен трансформ в мировых координатах.
        ecsWorld.Add(entity, new WorldTransform2D());

        var document = editor.OpenDocument(world);
        document.EntitySelection.Set(entity);

        try
        {
            var inspector = new InspectorPanelView(editor);
            inspector.Refresh();

            // Добавляем компонент через настоящий Add Component.
            var addableTypes =
                GetAddableTypes(editor, document, entity);

            var particleIndex = Array.IndexOf(
                addableTypes,
                typeof(ParticleEffectComponent));

            Assert.True(particleIndex >= 0);

            FindComponentPicker(inspector)
                .SetSelectedIndex(particleIndex);

            Assert.True(
                ecsWorld.Has<ParticleEffectComponent>(entity));

            inspector.Refresh();

            // Настраиваем ввод через UI.
            var root = new UiRoot();
            root.AddChild(inspector);

            var focus = new UiFocusManager();

            _ = new UiInputRouter(
                root,
                new EmptyPointerInput(),
                new EmptyTextInput(),
                focus);

            var effectPathEditor =
                FindBodyTextPropertyEditor(
                    inspector,
                    nameof(ParticleEffectComponent.EffectPath));

            var effectPathBox = Assert.Single(
                Descendants(effectPathEditor)
                    .OfType<UiTextBox>());

            effectPathBox.SetText(
                "Particles/Smoke.particle.json");

            focus.SetFocus(effectPathBox);
            focus.Activate();

            Assert.Equal(
                "Particles/Smoke.particle.json",
                ecsWorld.Get<ParticleEffectComponent>(entity)
                    .EffectPath);

            var burstEditor =
                FindBodyTextPropertyEditor(
                    inspector,
                    nameof(ParticleEffectComponent.InitialBurstCount));

            var burstBox = Assert.Single(
                Descendants(burstEditor)
                    .OfType<UiTextBox>());

            burstBox.SetText("16");

            focus.SetFocus(burstBox);
            focus.Activate();

            Assert.Equal(
                16,
                ecsWorld.Get<ParticleEffectComponent>(entity)
                    .InitialBurstCount);
        }
        finally
        {
            editor.Session.Discard(document);
        }
    }

    [Fact]
    public void Inspector_UpdatesColliderShapeAndMaterialInEcs()
    {
        using var ecsWorld = new Engine.ECS.World();

        using var world = new World(
            new ChunkSize(16, 16),
            ecsWorld);

        var editor = new EditorContext();

        editor.ComponentTypes.RegisterEngineComponents();

        editor.ComponentTypes.Register<PhysicsBody2D>(
            () => PhysicsBody2D.Dynamic(Fixed32.One));

        editor.ComponentTypes.Register<Collider2D>(
            () => new Collider2D(
                new AabbShape2D(FixedVector2.One)));

        editor.ComponentDependencies.Register(
            typeof(PhysicsBody2D),
            typeof(WorldTransform2D));

        editor.ComponentDependencies.Register(
            typeof(Collider2D),
            typeof(PhysicsBody2D),
            typeof(WorldTransform2D));

        var entity = ecsWorld.CreateEntity();

        ecsWorld.Add(entity, new WorldTransform2D());
        ecsWorld.Add(entity, PhysicsBody2D.Dynamic(Fixed32.One));
        ecsWorld.Add(
            entity,
            new Collider2D(
                new AabbShape2D(FixedVector2.One)));

        var document = editor.OpenDocument(world);
        document.EntitySelection.Set(entity);

        try
        {
            var inspector = new InspectorPanelView(editor);
            inspector.Refresh();

            // Меняем форму коллайдера с AABB на Circle.
            var shapeEditor = FindPropertyEditor(
                inspector,
                "Shape");

            var shapeControls = Descendants(shapeEditor)
                .ToArray();

            var shapePicker = shapeControls
                .OfType<UiDropdown>()
                .Single();

            var shapeTextBoxes = shapeControls
                .OfType<UiTextBox>()
                .ToArray();

            Assert.Equal(4, shapeTextBoxes.Length);

            shapePicker.SetSelectedIndex(
                (int)PhysicsShapeType.Circle);

            shapeTextBoxes[2].SetText("2.5");

            shapeControls
                .OfType<UiButton>()
                .Single(button => button.Text == "Apply")
                .Click();

            var collider = ecsWorld.Get<Collider2D>(entity);

            Assert.Equal(
                PhysicsShapeType.Circle,
                collider.Shape.Type);

            Assert.Equal(
                Fixed32.FromFloat(2.5f),
                collider.Shape.Circle.Radius);

            // Меняем трение и упругость материала.
            inspector.Refresh();

            var materialEditor = FindPropertyEditor(
                inspector,
                "Material");

            var materialControls = Descendants(materialEditor)
                .ToArray();

            var materialTextBoxes = materialControls
                .OfType<UiTextBox>()
                .ToArray();

            Assert.Equal(2, materialTextBoxes.Length);

            materialTextBoxes[0].SetText("0.75");
            materialTextBoxes[1].SetText("0.25");

            materialControls
                .OfType<UiButton>()
                .Single(button => button.Text == "Apply")
                .Click();

            collider = ecsWorld.Get<Collider2D>(entity);

            Assert.Equal(
                Fixed32.FromFloat(0.75f),
                collider.Material.Friction);

            Assert.Equal(
                Fixed32.FromFloat(0.25f),
                collider.Material.Restitution);
        }
        finally
        {
            editor.Session.Discard(document);
        }
    }

    [Fact]
    public void AddComponentPicker_RequiresPhysicsBodyBeforeCollider()
    {
        using var ecsWorld = new Engine.ECS.World();

        using var world = new World(
            new ChunkSize(16, 16),
            ecsWorld);

        var editor = new EditorContext();

        editor.ComponentTypes.RegisterEngineComponents();

        editor.ComponentTypes.Register<PhysicsBody2D>(
            () => PhysicsBody2D.Dynamic(Fixed32.One));

        editor.ComponentTypes.Register<Collider2D>(
            () => new Collider2D(
                new AabbShape2D(FixedVector2.One)));

        editor.ComponentDependencies.Register(
            typeof(PhysicsBody2D),
            typeof(WorldTransform2D));

        editor.ComponentDependencies.Register(
            typeof(Collider2D),
            typeof(PhysicsBody2D),
            typeof(WorldTransform2D));

        var entity = ecsWorld.CreateEntity();

        ecsWorld.Add(
            entity,
            new WorldTransform2D());

        var document = editor.OpenDocument(world);
        document.EntitySelection.Set(entity);

        try
        {
            var inspector = new InspectorPanelView(editor);
            inspector.Refresh();

            var blockedLabel = Descendants(inspector)
                .OfType<UiLabel>()
                .Single(label =>
                    label.Text.StartsWith(
                        "Collider2D requires:",
                        StringComparison.Ordinal));

            Assert.Contains("PhysicsBody2D", blockedLabel.Text);
            Assert.DoesNotContain("WorldTransform2D", blockedLabel.Text);

            Assert.False(
                ecsWorld.Has<PhysicsBody2D>(entity));

            Assert.False(
                ecsWorld.Has<Collider2D>(entity));

            // Добавляем физическое тело через настоящий Add Component.
            var bodyIndex = Array.IndexOf(
                GetAddableTypes(editor, document, entity),
                typeof(PhysicsBody2D));

            Assert.True(bodyIndex >= 0);

            FindComponentPicker(inspector)
                .SetSelectedIndex(bodyIndex);

            Assert.True(
                ecsWorld.Has<PhysicsBody2D>(entity));

            Assert.Equal(
                PhysicsBodyType.Dynamic,
                ecsWorld.Get<PhysicsBody2D>(entity).BodyType);

            // После обновления Collider должен стать доступен.
            inspector.Refresh();

            Assert.False(
                Descendants(inspector)
                    .OfType<UiLabel>()
                    .Any(label =>
                        label.Text.StartsWith(
                            "Collider2D requires:",
                            StringComparison.Ordinal)));

            var colliderIndex = Array.IndexOf(
                GetAddableTypes(editor, document, entity),
                typeof(Collider2D));

            Assert.True(colliderIndex >= 0);

            FindComponentPicker(inspector)
                .SetSelectedIndex(colliderIndex);

            Assert.True(
                ecsWorld.Has<Collider2D>(entity));

            Assert.Equal(
                PhysicsShapeType.Aabb,
                ecsWorld.Get<Collider2D>(entity).Shape.Type);
        }
        finally
        {
            editor.Session.Discard(document);
        }
    }

    [Fact]
    public void Inspector_PreventsRemovingPhysicsBodyRequiredByCollider()
    {
        using var ecsWorld = new Engine.ECS.World();

        using var world = new World(
            new ChunkSize(16, 16),
            ecsWorld);

        var editor = new EditorContext();

        editor.ComponentTypes.RegisterEngineComponents();

        editor.ComponentTypes.Register<PhysicsBody2D>(
            () => PhysicsBody2D.Dynamic(Fixed32.One));

        editor.ComponentTypes.Register<Collider2D>(
            () => new Collider2D(
                new AabbShape2D(FixedVector2.One)));

        editor.ComponentDependencies.Register(
            typeof(PhysicsBody2D),
            typeof(WorldTransform2D));

        editor.ComponentDependencies.Register(
            typeof(Collider2D),
            typeof(PhysicsBody2D),
            typeof(WorldTransform2D));

        var entity = ecsWorld.CreateEntity();

        ecsWorld.Add(entity, new WorldTransform2D());
        ecsWorld.Add(entity, PhysicsBody2D.Dynamic(Fixed32.One));
        ecsWorld.Add(
            entity,
            new Collider2D(
                new AabbShape2D(FixedVector2.One)));

        var document = editor.OpenDocument(world);
        document.EntitySelection.Set(entity);

        try
        {
            var inspector = new InspectorPanelView(editor);
            inspector.Refresh();

            Assert.Contains(
                Descendants(inspector).OfType<UiLabel>(),
                label => label.Text == "Required by: Collider2D.");

            var bodyHeader = Descendants(inspector)
                .OfType<UiStackPanel>()
                .Single(panel =>
                    panel.Children
                        .OfType<UiLabel>()
                        .Any(label => label.Text == "PhysicsBody2D"));

            Assert.DoesNotContain(
                bodyHeader.Children.OfType<UiButton>(),
                button => button.Text == "Remove");

            var command =
                new Engine.Editor.Commands.RemoveEditorComponentCommand(
                    world,
                    document.GetEntityReference(entity),
                    typeof(PhysicsBody2D),
                    editor.ComponentDependencies);

            Assert.Throws<InvalidOperationException>(
                command.Execute);

            Assert.True(ecsWorld.Has<PhysicsBody2D>(entity));
            Assert.True(ecsWorld.Has<Collider2D>(entity));
        }
        finally
        {
            editor.Session.Discard(document);
        }
    }

    [Fact]
    public void AddComponentPicker_AddsTransformsAndPreservesWorldTransform()
    {
        using var ecsWorld = new Engine.ECS.World();

        using var world = new World(
            new ChunkSize(16, 16),
            ecsWorld);

        var editor = new EditorContext();
        editor.ComponentTypes.RegisterEngineComponents();

        var entity = ecsWorld.CreateEntity();
        var document = editor.OpenDocument(world);
        document.EntitySelection.Set(entity);

        try
        {
            var inspector = new InspectorPanelView(editor);
            inspector.Refresh();

            Assert.False(ecsWorld.Has<WorldTransform2D>(entity));
            Assert.False(ecsWorld.Has<LocalTransform2D>(entity));

            Assert.Contains(
                Descendants(inspector).OfType<UiLabel>(),
                label => label.Text.StartsWith(
                    "LocalTransform2D requires:",
                    StringComparison.Ordinal));

            // Сначала добавляем WorldTransform2D.
            var addableTypes = GetAddableTypes(editor, document, entity);
            var worldTransformIndex = Array.IndexOf(
                addableTypes,
                typeof(WorldTransform2D));

            Assert.True(worldTransformIndex >= 0);

            FindComponentPicker(inspector)
                .SetSelectedIndex(worldTransformIndex);

            Assert.True(ecsWorld.Has<WorldTransform2D>(entity));

            // Задаём нетривиальный мировой трансформ.
            var expected = new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromInt(4),
                    Fixed32.FromInt(-2)))
            {
                Rotation = Fixed32.FromFloat(0.5f),
                Scale = new FixedVector2(
                    Fixed32.FromInt(2),
                    Fixed32.FromInt(3))
            };

            ecsWorld.Get<WorldTransform2D>(entity) = expected;

            // После обновления Inspector локальный трансформ доступен.
            inspector.Refresh();

            Assert.DoesNotContain(
                Descendants(inspector).OfType<UiLabel>(),
                label => label.Text.StartsWith(
                    "LocalTransform2D requires:",
                    StringComparison.Ordinal));

            addableTypes = GetAddableTypes(editor, document, entity);
            var localTransformIndex = Array.IndexOf(
                addableTypes,
                typeof(LocalTransform2D));

            Assert.True(localTransformIndex >= 0);

            FindComponentPicker(inspector)
                .SetSelectedIndex(localTransformIndex);

            Assert.True(ecsWorld.Has<LocalTransform2D>(entity));

            var local = ecsWorld.Get<LocalTransform2D>(entity);

            Assert.Equal(expected.Position, local.Position);
            Assert.Equal(expected.Rotation, local.Rotation);
            Assert.Equal(expected.Scale, local.Scale);

            inspector.Refresh();

            Assert.Contains(
                Descendants(inspector).OfType<UiLabel>(),
                label => label.Text ==
                    "Mode: Derived from LocalTransform2D / hierarchy");
        }
        finally
        {
            editor.Session.Discard(document);
        }
    }

    [Fact]
    public void Inspector_UpdatesPhysicsBodyTypeMassAndInertia()
    {
        using var ecsWorld = new Engine.ECS.World();

        using var world = new World(
            new ChunkSize(16, 16),
            ecsWorld);

        var editor = new EditorContext();

        editor.ComponentTypes.RegisterEngineComponents();

        editor.ComponentTypes.Register<PhysicsBody2D>(
            () => PhysicsBody2D.Dynamic(Fixed32.One));

        var entity = ecsWorld.CreateEntity();

        ecsWorld.Add(entity, new WorldTransform2D());
        ecsWorld.Add(
            entity,
            PhysicsBody2D.Dynamic(Fixed32.One));

        var document = editor.OpenDocument(world);
        document.EntitySelection.Set(entity);

        try
        {
            var inspector = new InspectorPanelView(editor);
            inspector.Refresh();

            // Подключаем Inspector к UI и готовим управление фокусом.
            var root = new UiRoot();
            root.AddChild(inspector);

            var focus = new UiFocusManager();

            _ = new UiInputRouter(
                root,
                new EmptyPointerInput(),
                new EmptyTextInput(),
                focus);

            // Редактируем Mass через текстовое поле Inspector.
            var massEditor = FindBodyTextPropertyEditor(
                inspector,
                nameof(PhysicsBody2D.Mass));

            var massBox = Assert.Single(
                Descendants(massEditor).OfType<UiTextBox>());

            massBox.SetText("3");
            focus.SetFocus(massBox);
            Assert.Same(massBox, focus.FocusedWidget);
            focus.Activate();

            Assert.Equal(
                Fixed32.FromInt(3),
                ecsWorld.Get<PhysicsBody2D>(entity).Mass);

            // Редактируем Inertia тем же способом.
            var inertiaEditor = FindBodyTextPropertyEditor(
                inspector,
                nameof(PhysicsBody2D.Inertia));

            var inertiaBox = Assert.Single(
                Descendants(inertiaEditor).OfType<UiTextBox>());

            inertiaBox.SetText("2");
            focus.SetFocus(inertiaBox);
            focus.Activate();

            Assert.Equal(
                Fixed32.FromInt(2),
                ecsWorld.Get<PhysicsBody2D>(entity).Inertia);

            // Меняем BodyType через выпадающий список.
            var bodyTypeEditor = FindBodyEnumPropertyEditor(
                inspector,
                nameof(PhysicsBody2D.BodyType));

            var bodyTypePicker = Assert.Single(
                Descendants(bodyTypeEditor).OfType<UiDropdown>());

            bodyTypePicker.SetSelectedIndex(
                (int)PhysicsBodyType.Kinematic);

            Assert.Equal(
                PhysicsBodyType.Kinematic,
                ecsWorld.Get<PhysicsBody2D>(entity).BodyType);
        }
        finally
        {
            editor.Session.Discard(document);
        }
    }

    private static UiStackPanel FindBodyTextPropertyEditor(
    UiWidget root,
    string propertyName)
    {
        return Descendants(root)
            .OfType<UiStackPanel>()
            .Single(panel =>
                panel.Children.OfType<UiLabel>()
                    .Any(label => label.Text == propertyName) &&
                panel.Children.OfType<UiTextBox>().Any());
    }

    private static UiStackPanel FindBodyEnumPropertyEditor(
        UiWidget root,
        string propertyName)
    {
        return Descendants(root)
            .OfType<UiStackPanel>()
            .Single(panel =>
                panel.Children.OfType<UiLabel>()
                    .Any(label => label.Text == propertyName) &&
                panel.Children.OfType<UiDropdown>().Any());
    }

    private sealed class EmptyPointerInput : IPointerInput
    {
        public Vector2 Position => Vector2.Zero;
        public float ScrollDelta => 0f;

        public bool IsDown(InputMouseButton button) => false;
        public bool IsPressed(InputMouseButton button) => false;
        public bool IsReleased(InputMouseButton button) => false;
    }

    private sealed class EmptyTextInput : ITextInput
    {
        public IReadOnlyList<char> Characters =>
            Array.Empty<char>();

        public bool IsPressed(TextInputKey key) => false;
    }

    private static Type[] GetAddableTypes(
        EditorContext editor,
        EditorDocument document,
        Engine.ECS.Entities.EntityId entity)
    {
        var existing = document.Inspector
            .GetComponentTypes(entity)
            .ToHashSet();

        return editor.ComponentTypes.Types
            .Where(type =>
                type != typeof(WorldPositionComponent) &&
                !existing.Contains(type))
            .Where(type =>
                editor.ComponentDependencies.CanAdd(type, existing))
            .OrderBy(
                type => type.FullName ?? type.Name,
                StringComparer.Ordinal)
            .ToArray();
    }

    private static UiStackPanel FindPropertyEditor(
    UiWidget root,
    string propertyName)
    {
        return Descendants(root)
            .OfType<UiStackPanel>()
            .Single(panel =>
                panel.Children
                    .OfType<UiLabel>()
                    .Any(label => label.Text == propertyName) &&
                panel.Children
                    .OfType<UiButton>()
                    .Any(button => button.Text == "Apply"));
    }

    private static UiDropdown FindComponentPicker(
        InspectorPanelView inspector)
    {
        // Add Component расположен раньше редакторов свойств
        // уже существующих компонентов.
        return Descendants(inspector)
            .OfType<UiDropdown>()
            .First();
    }

    private static IEnumerable<UiWidget> Descendants(
        UiWidget widget)
    {
        yield return widget;

        if (widget is not UiContainer container)
        {
            yield break;
        }

        foreach (var child in container.Children)
        {
            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }
}