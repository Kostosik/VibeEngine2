using System.Text.Json;

namespace Engine.Editor.Persistence;

public sealed class EditorProjectStateStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _filePath;

    public EditorProjectStateStore()
        : this(GetDefaultFilePath())
    {
    }

    public EditorProjectStateStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        _filePath = Path.GetFullPath(filePath);
    }

    private static string GetDefaultFilePath()
    {
        var localAppData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);

        if (string.IsNullOrWhiteSpace(localAppData))
        {
            throw new InvalidOperationException(
                "Cannot determine the local application data directory.");
        }

        return Path.Combine(
            localAppData,
            "VibeEngine2",
            "Editor",
            "editor-state.json");
    }

    public string? LoadLastProjectManifestPath()
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        var json = File.ReadAllText(_filePath);

        var state = JsonSerializer.Deserialize<EditorProjectState>(
            json,
            Options)
            ?? throw new InvalidDataException(
                "Editor state file is empty.");

        return string.IsNullOrWhiteSpace(
            state.LastProjectManifestPath)
            ? null
            : Path.GetFullPath(state.LastProjectManifestPath);
    }

    public void SaveLastProjectManifestPath(string? manifestPath)
    {
        var state = new EditorProjectState
        {
            LastProjectManifestPath =
                string.IsNullOrWhiteSpace(manifestPath)
                    ? null
                    : Path.GetFullPath(manifestPath)
        };

        var directory = Path.GetDirectoryName(_filePath)
            ?? throw new InvalidOperationException(
                "Cannot determine the editor state directory.");

        Directory.CreateDirectory(directory);

        AtomicTextFile.WriteAllText(
    _filePath,
    JsonSerializer.Serialize(state, Options)
        + Environment.NewLine);
    }

    private sealed class EditorProjectState
    {
        public string? LastProjectManifestPath { get; init; }
    }
}