using System.Text.Json;
using Engine.Editor.Workspace;

namespace Engine.Editor.Persistence;

public sealed class JsonEditorWorkspacePersistence :
    IEditorWorkspacePersistence
{
    private static readonly JsonSerializerOptions Options =
        new()
        {
            WriteIndented = true
        };

    public void Save(
        string path,
        EditorWorkspaceState state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            path);

        ArgumentNullException.ThrowIfNull(
            state);

        var json =
            JsonSerializer.Serialize(
                state,
                Options);

        File.WriteAllText(
            path,
            json);
    }

    public EditorWorkspaceState Load(
        string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            path);

        var json =
            File.ReadAllText(
                path);

        var state =
            JsonSerializer.Deserialize<EditorWorkspaceState>(
                json,
                Options);

        return state
            ?? throw new InvalidDataException(
                "Editor workspace state is empty.");
    }
}