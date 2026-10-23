using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.Editor;
using Engine.Editor.Documents;
using Engine.Editor.UI.Workspace;
using Engine.Particles;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class ParticleEffectInspectorTests
{
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