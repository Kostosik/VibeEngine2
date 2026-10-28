using System.Text.Json;

namespace Engine.Scripting;

public sealed class ScriptDefinition
{
    public const int CurrentFormatVersion = 1;

    public ScriptDefinition(
        int formatVersion,
        string scriptId,
        string defaultConfigurationJson = "{}",
        string? implementationAssemblyName = null,
        string? implementationTypeName = null)
    {
        if (formatVersion != CurrentFormatVersion)
        {
            throw new ArgumentOutOfRangeException(
                nameof(formatVersion),
                $"Unsupported script definition format version '{formatVersion}'.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(scriptId);
        ArgumentNullException.ThrowIfNull(defaultConfigurationJson);

        using var document =
            JsonDocument.Parse(defaultConfigurationJson);

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException(
                "Default script configuration must be a JSON object.",
                nameof(defaultConfigurationJson));
        }

        var hasAssembly =
            !string.IsNullOrWhiteSpace(implementationAssemblyName);

        var hasType =
            !string.IsNullOrWhiteSpace(implementationTypeName);

        if (hasAssembly != hasType)
        {
            throw new ArgumentException(
                "Implementation assembly and type must be specified together.");
        }

        FormatVersion = formatVersion;
        ScriptId = scriptId;

        DefaultConfigurationJson =
            JsonSerializer.Serialize(document.RootElement);

        ImplementationAssemblyName =
            hasAssembly ? implementationAssemblyName!.Trim() : null;

        ImplementationTypeName =
            hasType ? implementationTypeName!.Trim() : null;
    }

    public int FormatVersion { get; }

    public string ScriptId { get; }

    public string DefaultConfigurationJson { get; }

    public string? ImplementationAssemblyName { get; }

    public string? ImplementationTypeName { get; }

    public bool HasImplementation =>
        ImplementationAssemblyName is not null &&
        ImplementationTypeName is not null;
}