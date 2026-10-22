namespace Engine.Editor.Projects;

/// <summary>
/// Describes a game project loaded from a .vibeproject manifest.
/// Independent from the editor's open documents.
/// </summary>
public sealed class GameProject
{
    private GameProject(
        string name,
        string rootDirectory,
        string manifestFilePath,
        string projectFilePath,
        string? solutionFilePath,
        string? startupSceneRelativePath)
    {
        Name = name;
        RootDirectory = rootDirectory;
        ManifestFilePath = manifestFilePath;
        ProjectFilePath = projectFilePath;
        SolutionFilePath = solutionFilePath;
        StartupSceneRelativePath =
    startupSceneRelativePath;
    }

    public string Name { get; }

    public string RootDirectory { get; }

    public string AssetsDirectoryPath =>
        Path.Combine(RootDirectory, "Assets");

    public string ManifestFilePath { get; }

    public string ProjectFilePath { get; }

    /// <summary>
    /// Relative path to the startup scene, if configured.
    /// </summary>
    public string? StartupSceneRelativePath { get; private set; }
    public string? SolutionFilePath { get; }

    public static GameProject Open(string manifestFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestFilePath);

        var fullManifestPath = Path.GetFullPath(manifestFilePath);

        if (!string.Equals(
                Path.GetExtension(fullManifestPath),
                ".vibeproject",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Select a .vibeproject file.",
                nameof(manifestFilePath));
        }

        if (!File.Exists(fullManifestPath))
        {
            throw new FileNotFoundException(
                "VibeEngine project manifest was not found.",
                fullManifestPath);
        }

        var rootDirectory =
            Path.GetDirectoryName(fullManifestPath)
            ?? throw new InvalidOperationException(
                "Cannot determine the project directory.");

        var manifest =
            VibeProjectManifest.Load(fullManifestPath);

        var startupProjectPath = manifest.StartupProject;

        if (Path.IsPathRooted(startupProjectPath))
        {
            throw new InvalidDataException(
                "The 'startupProject' path must be relative to the project directory.");
        }

        var projectFilePath = Path.GetFullPath(
            Path.Combine(rootDirectory, startupProjectPath));

        var relativeProjectPath = Path.GetRelativePath(
            rootDirectory,
            projectFilePath);

        if (IsOutsideProjectRoot(relativeProjectPath))
        {
            throw new InvalidDataException(
                "The 'startupProject' path cannot point outside the project directory.");
        }

        if (!string.Equals(
                Path.GetExtension(projectFilePath),
                ".csproj",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "The 'startupProject' path must point to a .csproj file.");
        }

        if (!File.Exists(projectFilePath))
        {
            throw new FileNotFoundException(
                $"Startup project '{startupProjectPath}' was not found.",
                projectFilePath);
        }

        var solutionFilePath =
            FindSolutionFilePath(
                rootDirectory,
                projectFilePath);

        return new GameProject(
            manifest.Name.Trim(),
            Path.GetFullPath(rootDirectory),
            fullManifestPath,
            projectFilePath,
            solutionFilePath,
            manifest.StartupScene);
    }

    public void SetStartupScene(string sceneFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneFilePath);

        var fullScenePath = Path.GetFullPath(sceneFilePath);

        if (!ContainsPath(fullScenePath))
        {
            throw new ArgumentException(
                "The startup scene must be inside the game project directory.",
                nameof(sceneFilePath));
        }

        if (!string.Equals(
                Path.GetExtension(fullScenePath),
                ".world",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The startup scene must be a .world file.",
                nameof(sceneFilePath));
        }

        if (!File.Exists(fullScenePath))
        {
            throw new FileNotFoundException(
                "The startup scene file was not found.",
                fullScenePath);
        }

        var relativePath = Path
            .GetRelativePath(RootDirectory, fullScenePath)
            .Replace('\\', '/');

        var currentManifest =
            VibeProjectManifest.Load(ManifestFilePath);

        var updatedManifest = new VibeProjectManifest
        {
            FormatVersion = currentManifest.FormatVersion,
            Name = currentManifest.Name,
            StartupProject = currentManifest.StartupProject,
            StartupScene = relativePath
        };

        var manifestDirectory =
            Path.GetDirectoryName(ManifestFilePath)
            ?? throw new InvalidOperationException(
                "Cannot determine the project manifest directory.");

        var temporaryPath = Path.Combine(
            manifestDirectory,
            $".{Path.GetFileName(ManifestFilePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(
                temporaryPath,
                updatedManifest.ToJson());

            // Заменяем манифест только после успешной записи нового файла.
            File.Move(
                temporaryPath,
                ManifestFilePath,
                overwrite: true);

            StartupSceneRelativePath = relativePath;
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static bool IsOutsideProjectRoot(string relativePath)
    {
        return Path.IsPathRooted(relativePath)
            || relativePath.Equals(
                "..",
                StringComparison.Ordinal)
            || relativePath.StartsWith(
                $"..{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal)
            || relativePath.StartsWith(
                $"..{Path.AltDirectorySeparatorChar}",
                StringComparison.Ordinal);
    }

    internal bool ContainsPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        var relativePath = Path.GetRelativePath(
            RootDirectory,
            fullPath);

        return !IsOutsideProjectRoot(relativePath);
    }

    private static string? FindSolutionFilePath(
        string rootDirectory,
        string projectFilePath)
    {
        var projectName =
            Path.GetFileNameWithoutExtension(projectFilePath);

        var expectedSolutionPath =
            Path.Combine(rootDirectory, $"{projectName}.sln");

        if (File.Exists(expectedSolutionPath))
        {
            return Path.GetFullPath(expectedSolutionPath);
        }

        var solutionFiles = Directory
            .EnumerateFiles(
                rootDirectory,
                "*",
                SearchOption.TopDirectoryOnly)
            .Where(path =>
                string.Equals(
                    Path.GetExtension(path),
                    ".sln",
                    StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return solutionFiles.Length == 1
            ? Path.GetFullPath(solutionFiles[0])
            : null;
    }
}