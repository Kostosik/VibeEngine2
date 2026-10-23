
using System.Text;

namespace Engine.Editor.Assets;

public sealed class EditorAssetCreationService
{
    private readonly EditorAssetBrowser _browser;

    public EditorAssetCreationService(
        EditorAssetBrowser browser)
    {
        ArgumentNullException.ThrowIfNull(browser);
        _browser = browser;
    }


    public string CreateTextFile(
        string name,
        string extension,
        string contents,
        string? targetDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(contents);

        ValidateName(name, nameof(name));
        ValidateExtension(extension);

        var path = ResolveTargetPath(
            name + extension,
            targetDirectory);

        EnsureDoesNotExist(path);

        var created = false;

        try
        {
            using var stream = new FileStream(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);

            created = true;

            using var writer = new StreamWriter(
                stream,
                new UTF8Encoding(false));

            writer.Write(contents);
        }
        catch
        {
            // Не удаляем файл, если CreateNew не смог его создать.
            if (created)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch
                {
                    // Сохраняем исходную ошибку записи.
                }
            }

            throw;
        }

        RefreshAndSelect(path);

        return path;
    }

    public string CreateDirectory(
        string name,
        string? targetDirectory = null)
    {
        ValidateName(name, nameof(name));

        var path = ResolveTargetPath(name, targetDirectory);

        EnsureDoesNotExist(path);

        Directory.CreateDirectory(path);

        RefreshAndSelect(path);

        return path;
    }

    public string Create(
        EditorAssetCreationDefinition definition,
        string name,
        string? targetDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(definition);

        return definition.Kind switch
        {
            EditorAssetCreationKind.Directory =>
                CreateDirectory(name, targetDirectory),

            EditorAssetCreationKind.TextFile =>
                CreateTextFile(
                    name,
                    definition.Extension,
                    definition.CreateInitialContents(name),
                    targetDirectory),

            _ => throw new ArgumentOutOfRangeException(
                nameof(definition))
        };
    }

    private string ResolveTargetPath(
        string name,
        string? targetDirectory = null)
    {
        var rootPath = Path.GetFullPath(_browser.RootPath);

        var destinationPath = Path.GetFullPath(
            targetDirectory ?? _browser.CurrentPath);

        if (!Directory.Exists(destinationPath))
        {
            throw new DirectoryNotFoundException(
                $"Asset directory '{destinationPath}' was not found.");
        }

        if (!IsWithinRoot(rootPath, destinationPath))
        {
            throw new UnauthorizedAccessException(
                "The target directory is outside the asset root.");
        }

        var path = Path.GetFullPath(
            Path.Combine(destinationPath, name));

        if (!IsWithinRoot(rootPath, path))
        {
            throw new UnauthorizedAccessException(
                "The new asset must remain inside the asset root.");
        }

        return path;
    }

    private void RefreshAndSelect(string path)
    {
        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException(
                $"Could not determine the parent directory of '{path}'.");

        var pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!string.Equals(
                Path.GetFullPath(_browser.CurrentPath),
                Path.GetFullPath(directory),
                pathComparison) &&
            !_browser.NavigateTo(directory))
        {
            throw new InvalidOperationException(
                $"Could not navigate to the created asset directory '{directory}'.");
        }

        _browser.Refresh();

        if (!_browser.Select(path))
        {
            throw new InvalidOperationException(
                $"Created asset '{path}', but it could not be selected " +
                "in the Asset Browser.");
        }
    }

    private static bool IsWithinRoot(
        string rootPath,
        string path)
    {
        var relativePath = Path.GetRelativePath(
            rootPath,
            path);

        return relativePath == "." ||
            (relativePath != ".." &&
             !relativePath.StartsWith(
                 ".." + Path.DirectorySeparatorChar,
                 StringComparison.Ordinal) &&
             !relativePath.StartsWith(
                 ".." + Path.AltDirectorySeparatorChar,
                 StringComparison.Ordinal) &&
             !Path.IsPathRooted(relativePath));
    }

    private static void ValidateName(
        string name,
        string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            name,
            parameterName);

        if (name is "." or ".." ||
            name.Contains('/') ||
            name.Contains('\\') ||
            name.IndexOfAny(
                Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException(
                "The name must be a valid single file or directory name.",
                parameterName);
        }
    }

    private static void ValidateExtension(string extension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            extension);

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

    private static void EnsureDoesNotExist(string path)
    {
        if (File.Exists(path) || Directory.Exists(path))
        {
            throw new IOException(
                $"An asset already exists at '{path}'.");
        }
    }
}
