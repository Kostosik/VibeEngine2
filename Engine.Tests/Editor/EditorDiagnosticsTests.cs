using Engine.Core.Diagnostics;
using Engine.Editor;
using Engine.Tooling.Debugging;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class EditorDiagnosticsTests
{
    [Fact]
    public void SetDiagnosticConsole_ReportsUnsupportedInspectorPropertiesForExistingAndNewDocuments()
    {
        var editor = new EditorContext();

        using var console = new DebugConsole(
            new DebugCommandRegistry());

        using var firstEcsWorld = new Engine.ECS.World();
        using var secondEcsWorld = new Engine.ECS.World();

        try
        {
            // Первый документ открывается до подключения консоли.
            var firstWorld = new World(
                new ChunkSize(16, 16),
                firstEcsWorld);

            var firstEntity = firstEcsWorld.CreateEntity();

            firstEcsWorld.Add(
                firstEntity,
                new UnsupportedPropertyComponent());

            var firstDocument = editor.OpenDocument(firstWorld);

            editor.SetDiagnosticConsole(console);

            var firstProperties =
                firstDocument.Inspector.GetProperties(
                    firstEntity,
                    typeof(UnsupportedPropertyComponent));

            Assert.Empty(firstProperties);

            const string diagnosticCode =
                "INSPECTOR_UNSUPPORTED_PROPERTY";

            Assert.Single(
                console.Messages.Where(
                    message => message.Code == diagnosticCode));

            // Второй документ открывается уже после подключения консоли.
            var secondWorld = new World(
                new ChunkSize(16, 16),
                secondEcsWorld);

            var secondEntity = secondEcsWorld.CreateEntity();

            secondEcsWorld.Add(
                secondEntity,
                new UnsupportedPropertyComponent());

            var secondDocument = editor.OpenDocument(secondWorld);

            secondDocument.Inspector.GetProperties(
                secondEntity,
                typeof(UnsupportedPropertyComponent));

            var diagnostics = console.Messages
                .Where(message => message.Code == diagnosticCode)
                .ToArray();

            Assert.Equal(2, diagnostics.Length);

            Assert.All(
                diagnostics,
                message => Assert.Equal(
                    DiagnosticLevel.Debug,
                    message.Level));
        }
        finally
        {
            editor.Session.DiscardAll();
        }
    }

    private struct UnsupportedPropertyComponent
    {
        public Span<int> UnsupportedSpan =>
            Span<int>.Empty;
    }
}