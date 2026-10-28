using Engine.Core.Assets;
using Engine.Scripting;
using Engine.Serialization.Content;
using System.Text;

namespace Engine.Editor.Documents;

public sealed class ScriptDefinitionDocument : IEditorDocument
{
    public const string TypeId = "vibeengine.script-definition";

    private byte[] _savedSnapshot;

    private ScriptDefinitionDocument(
        string filePath,
        ScriptDefinition definition)
    {
        FilePath = filePath;
        Definition = definition;

        _savedSnapshot =
            JsonScriptDefinitionSerializer.Serialize(definition);
    }

    public string DocumentTypeId => TypeId;

    public string FilePath { get; }

    public string DisplayName =>
        Path.GetFileName(FilePath);

    public ScriptDefinition Definition { get; private set; }

    public bool IsDirty =>
        !JsonScriptDefinitionSerializer
            .Serialize(Definition)
            .AsSpan()
            .SequenceEqual(_savedSnapshot);

    public event Action? Changed;

    public void UpdateScriptId(string scriptId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptId);

        var current = Definition;

        if (string.Equals(
                current.ScriptId,
                scriptId,
                StringComparison.Ordinal))
        {
            return;
        }

        UpdateDefinition(
            new ScriptDefinition(
                current.FormatVersion,
                scriptId,
                current.DefaultConfigurationJson,
                current.ImplementationAssemblyName,
                current.ImplementationTypeName));
    }

    public static ScriptDefinitionDocument Open(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);

        if (!fullPath.EndsWith(
                ".script.json",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The file must have a .script.json extension.",
                nameof(path));
        }

        var definition =
            JsonScriptDefinitionSerializer.Deserialize(
                new AssetPath(Path.GetFileName(fullPath)),
                File.ReadAllBytes(fullPath));

        return new ScriptDefinitionDocument(
            fullPath,
            definition);
    }

    public void UpdateDefinition(
        ScriptDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var contents =
            JsonScriptDefinitionSerializer.Serialize(definition);

        // Проверяем, что полученный JSON можно загрузить обратно.
        _ = JsonScriptDefinitionSerializer.Deserialize(
            new AssetPath(Path.GetFileName(FilePath)),
            contents);

        if (contents.AsSpan().SequenceEqual(
                JsonScriptDefinitionSerializer.Serialize(Definition)))
        {
            return;
        }

        Definition = definition;
        Changed?.Invoke();
    }

    public void UpdateImplementation(
        string? assemblyName,
        string? typeName)
    {
        var current = Definition;

        UpdateDefinition(
            new ScriptDefinition(
                current.FormatVersion,
                current.ScriptId,
                current.DefaultConfigurationJson,
                assemblyName,
                typeName));
    }

    public void Save()
    {
        var contents =
            JsonScriptDefinitionSerializer.Serialize(Definition);

        _ = JsonScriptDefinitionSerializer.Deserialize(
            new AssetPath(Path.GetFileName(FilePath)),
            contents);

        var directory = Path.GetDirectoryName(FilePath)
            ?? throw new InvalidOperationException(
                "Could not determine the script definition directory.");

        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(FilePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllBytes(temporaryPath, contents);

            File.Move(
                temporaryPath,
                FilePath,
                overwrite: true);

            _savedSnapshot = contents;
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch
            {
                // Не маскируем первоначальную ошибку сохранения.
            }
        }

        Changed?.Invoke();
    }

    public void MarkSaved()
    {
        _savedSnapshot =
            JsonScriptDefinitionSerializer.Serialize(Definition);

        Changed?.Invoke();
    }
}