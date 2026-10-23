namespace Engine.Editor.Assets;

public enum EditorAssetCreationKind
{
    Directory,
    TextFile
}

public sealed class EditorAssetCreationDefinition
{
    private readonly Func<string, string>? _contentFactory;

    public EditorAssetCreationDefinition(
        string id,
        string displayName,
        string defaultName,
        EditorAssetCreationKind kind,
        string extension,
        Func<string, string>? contentFactory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultName);
        ArgumentNullException.ThrowIfNull(extension);

        if (kind is not EditorAssetCreationKind.Directory
            and not EditorAssetCreationKind.TextFile)
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (kind == EditorAssetCreationKind.Directory)
        {
            if (extension.Length != 0 || contentFactory is not null)
            {
                throw new ArgumentException(
                    "Directory definitions cannot have an extension or file template.");
            }
        }
        else
        {
            ValidateExtension(extension);

            ArgumentNullException.ThrowIfNull(contentFactory);
        }

        Id = id;
        DisplayName = displayName;
        DefaultName = defaultName;
        Kind = kind;
        Extension = extension;
        _contentFactory = contentFactory;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string DefaultName { get; }

    public EditorAssetCreationKind Kind { get; }

    public string Extension { get; }

    public string CreateInitialContents(string name)
    {
        if (Kind != EditorAssetCreationKind.TextFile)
        {
            throw new InvalidOperationException(
                "Directory definitions do not have file contents.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return _contentFactory!(name)
            ?? throw new InvalidOperationException(
                $"The content factory for '{Id}' returned null.");
    }

    private static void ValidateExtension(string extension)
    {
        if (extension.Length < 2 ||
            extension[0] != '.' ||
            extension.Skip(1).Any(
                character =>
                    !char.IsAsciiLetterOrDigit(character) &&
                    character != '.' &&
                    character != '_' &&
                    character != '-'))
        {
            throw new ArgumentException(
                "The extension must start with a dot and contain only " +
                "letters, digits, dots, underscores or hyphens.",
                nameof(extension));
        }
    }
}