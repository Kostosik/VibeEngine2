using System.Text.Json;

namespace Engine.Scripting;

public readonly record struct ScriptAssignment
{
    public ScriptAssignment(
        string scriptId,
        string configurationJson = "{}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            scriptId);

        ArgumentNullException.ThrowIfNull(
            configurationJson);

        using var document =
            JsonDocument.Parse(configurationJson);

        if (document.RootElement.ValueKind !=
            JsonValueKind.Object)
        {
            throw new ArgumentException(
                "Script configuration must be a JSON object.",
                nameof(configurationJson));
        }

        ScriptId = scriptId;
        ConfigurationJson = configurationJson;
    }

    public string ScriptId { get; }

    public string ConfigurationJson { get; }
}