namespace Engine.Editor.Projects;

/// <summary>
/// Describes a game project stored on disk.
/// This is independent from the editor's open documents.
/// </summary>
public sealed class GameProject
{
    private GameProject(
        string name,
        string rootDirectory,
        string projectFilePath,
        string? solutionFilePath)
    {
        Name = name;
        RootDirectory = rootDirectory;
        ProjectFilePath = projectFilePath;
        SolutionFilePath = solutionFilePath;
    }

    public string Name { get; }

    public string RootDirectory { get; }

    public string ProjectFilePath { get; }

    public string? SolutionFilePath { get; }

    /// <summary>
    /// Creates a project descriptor from an existing project directory.
    /// The directory must contain exactly one top-level .csproj file.
    /// </summary>
    public static GameProject FromDirectory(
        string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        var rootDirectory = Path.GetFullPath(directory);

        if (!Directory.Exists(rootDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Project directory does not exist: {rootDirectory}");
        }

        var projectFiles = Directory
            .EnumerateFiles(
                rootDirectory,
                "*",
                SearchOption.TopDirectoryOnly)
            .Where(path =>
                string.Equals(
                    Path.GetExtension(path),
                    ".csproj",
                    StringComparison.OrdinalIgnoreCase))
            .OrderBy(
                path => Path.GetFileName(path),
                StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (projectFiles.Length == 0)
        {
            throw new FileNotFoundException(
                $"No project file (.csproj) was found in '{rootDirectory}'.");
        }

        if (projectFiles.Length > 1)
        {
            throw new InvalidOperationException(
                $"Multiple project files were found in '{rootDirectory}'.");
        }

        var projectFilePath = Path.GetFullPath(projectFiles[0]);

        var name = Path.GetFileNameWithoutExtension(projectFilePath);

        var expectedSolutionPath = Path.Combine(
            rootDirectory,
            $"{name}.sln");

        string? solutionFilePath = null;

        if (File.Exists(expectedSolutionPath))
        {
            solutionFilePath = Path.GetFullPath(expectedSolutionPath);
        }
        else
        {
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

            if (solutionFiles.Length == 1)
            {
                solutionFilePath = Path.GetFullPath(solutionFiles[0]);
            }
        }

        return new GameProject(
            name,
            rootDirectory,
            projectFilePath,
            solutionFilePath);
    }
}