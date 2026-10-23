using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.Editor;
using Engine.Editor.Assets;
using Engine.Editor.Documents;
using Engine.Editor.UI.Dialogs;
using Engine.Editor.UI.Workspace;
using Engine.Particles;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class ParticleEffectInspectorTests
{
    [Fact]
    public void PickerField_SelectsParticleEffectFromDialog()
    {
        var rootPath = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineAssetPicker-{Guid.NewGuid():N}");

        var particlesPath = Path.Combine(
            rootPath,
            "Particles");

        Directory.CreateDirectory(particlesPath);

        File.WriteAllText(
            Path.Combine(
                particlesPath,
                "Smoke.particle.json"),
            "{}");

        File.WriteAllText(
            Path.Combine(
                particlesPath,
                "notes.json"),
            "{}");

        using var ecsWorld = new Engine.ECS.World();

        using var world = new World(
            new ChunkSize(16, 16),
            ecsWorld);

        var entity = ecsWorld.CreateEntity();

        ecsWorld.Add(entity, new WorldTransform2D());
        ecsWorld.Add(entity, new ParticleEffectComponent());

        var editor = new EditorContext();

        editor.InitializeAssetBrowser(
            new FileSystemAssetSource(),
            rootPath);

        var document = editor.OpenDocument(world);

        var root = new UiRoot();
        var overlays = new UiOverlayLayer();
        root.AddChild(overlays);

        var dialog = new EditorAssetPickerDialog(
            editor,
            overlays,
            new UiFocusManager());

        try
        {
            document.EntitySelection.Set(entity);

            var panel = new InspectorPanelView(
                editor,
                assetPickerDialog: dialog);

            panel.Refresh();

            var picker = Descendants(panel)
                .OfType<UiPickerField>()
                .Single();

            picker.RequestSelection();

            Assert.True(dialog.IsOpen);

            FindButton(overlays, "[Folder] Particles")
                .Click();

            Assert.Contains(
                Descendants(overlays).OfType<UiButton>(),
                button => button.Text == "Smoke.particle.json");

            Assert.DoesNotContain(
                Descendants(overlays).OfType<UiButton>(),
                button => button.Text == "notes.json");

            FindButton(overlays, "Smoke.particle.json")
                .Click();

            Assert.False(dialog.IsOpen);

            Assert.Equal(
                "Particles/Smoke.particle.json",
                ecsWorld.Get<ParticleEffectComponent>(entity)
                    .EffectPath);

            document.Undo();

            Assert.Equal(
                string.Empty,
                ecsWorld.Get<ParticleEffectComponent>(entity)
                    .EffectPath);

            document.Redo();

            Assert.Equal(
                "Particles/Smoke.particle.json",
                ecsWorld.Get<ParticleEffectComponent>(entity)
                    .EffectPath);
        }
        finally
        {
            dialog.Close();
            editor.Session.Discard(document);

            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }

    [Fact]
    public void UseSelectedParticleEffect_AssignsPathAndSupportsUndoRedo()
    {
        using var ecsWorld = new Engine.ECS.World();

        using var world = new World(
            new ChunkSize(16, 16),
            ecsWorld);

        var entity = ecsWorld.CreateEntity();

        ecsWorld.Add(entity, new WorldTransform2D());
        ecsWorld.Add(entity, new ParticleEffectComponent());

        var editor = new EditorContext();
        var document = editor.OpenDocument(world);

        try
        {
            document.EntitySelection.Set(entity);

            var panel = new InspectorPanelView(
                editor,
                getSelectedParticleEffectPath:
                    () => "Particles/Smoke.particle.json");

            panel.Refresh();

            FindButton(panel, "Use selected").Click();

            Assert.Equal(
                "Particles/Smoke.particle.json",
                ecsWorld.Get<ParticleEffectComponent>(entity)
                    .EffectPath);

            document.Undo();

            Assert.Equal(
                string.Empty,
                ecsWorld.Get<ParticleEffectComponent>(entity)
                    .EffectPath);

            document.Redo();

            Assert.Equal(
                "Particles/Smoke.particle.json",
                ecsWorld.Get<ParticleEffectComponent>(entity)
                    .EffectPath);

            FindButton(panel, "Clear").Click();

            Assert.Equal(
                string.Empty,
                ecsWorld.Get<ParticleEffectComponent>(entity)
                    .EffectPath);

            document.Undo();

            Assert.Equal(
                "Particles/Smoke.particle.json",
                ecsWorld.Get<ParticleEffectComponent>(entity)
                    .EffectPath);
        }
        finally
        {
            editor.Session.Discard(document);
        }
    }

    private static UiButton FindButton(
        UiWidget root,
        string text)
    {
        return Descendants(root)
            .OfType<UiButton>()
            .Single(button => button.Text == text);
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