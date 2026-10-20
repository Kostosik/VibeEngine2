using System.Text.Json;

namespace Engine.Editor.Projects;

internal sealed class VibeProjectManifest
{
    public const int CurrentFormatVersion = 1;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public int FormatVersion { get; init; }

    public string Name { get; init; } = string.Empty;

    public string StartupProject { get; init; } = string.Empty;

    public static VibeProjectManifest Load(string path)
    {
        VibeProjectManifest? manifest;

        try
        {
            manifest = JsonSerializer.Deserialize<VibeProjectManifest>(
                File.ReadAllText(path),
                SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"Project manifest '{path}' contains invalid JSON.",
                exception);
        }

        if (manifest is null)
        {
            throw new InvalidDataException(
                $"Project manifest '{path}' is empty.");
        }

        if (manifest.FormatVersion != CurrentFormatVersion)
        {
            throw new NotSupportedException(
                $"Project format version {manifest.FormatVersion} " +
                $"is not supported. Supported version: {CurrentFormatVersion}.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            throw new InvalidDataException(
                "Project manifest field 'name' is required.");
        }

        if (string.IsNullOrWhiteSpace(manifest.StartupProject))
        {
            throw new InvalidDataException(
                "Project manifest field 'startupProject' is required.");
        }

        return manifest;
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, SerializerOptions)
            + Environment.NewLine;
    }
}