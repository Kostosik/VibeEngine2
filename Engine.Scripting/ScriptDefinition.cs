using System.Text.Json;

namespace Engine.Scripting;

public sealed class ScriptDefinition
{
    public const int CurrentFormatVersion = 1;

    public ScriptDefinition(
        int formatVersion,
        string scriptId,
        string defaultConfigurationJson = "{}")
    {
        if (formatVersion != CurrentFormatVersion)
        {
            throw new ArgumentOutOfRangeException(
                nameof(formatVersion),
                $"Unsupported script definition format version '{formatVersion}'.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            scriptId);

        ArgumentNullException.ThrowIfNull(
            defaultConfigurationJson);

        using var document =
            JsonDocument.Parse(defaultConfigurationJson);

        if (document.RootElement.ValueKind !=
            JsonValueKind.Object)
        {
            throw new ArgumentException(
                "Default script configuration must be a JSON object.",
                nameof(defaultConfigurationJson));
        }

        FormatVersion = formatVersion;
        ScriptId = scriptId;

        DefaultConfigurationJson =
            JsonSerializer.Serialize(
                document.RootElement);
    }

    public int FormatVersion { get; }

    public string ScriptId { get; }

    public string DefaultConfigurationJson { get; }
}