using System.Text.Json;
using Engine.Editor.Documents;

namespace Engine.Editor.Persistence;

public sealed class JsonEditorSessionPersistence :
    IEditorSessionPersistence
{
    private static readonly JsonSerializerOptions Options =
        new()
        {
            WriteIndented = true
        };

    public void Save(
        string path,
        EditorSessionState state)
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

    public EditorSessionState Load(
        string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            path);

        var json =
            File.ReadAllText(
                path);

        var state =
            JsonSerializer.Deserialize<EditorSessionState>(
                json,
                Options);

        return state
            ?? throw new InvalidDataException(
                "Editor session state is empty.");
    }
}