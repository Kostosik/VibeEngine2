using System.Text;

namespace Engine.Editor.Projects;

/// <summary>
/// Creates the initial files for an empty game project.
/// Does not depend on the editor UI or on a local engine checkout.
/// </summary>
public sealed class ProjectCreationService
{
    private const string EmptyTemplateId = "empty";

    private const string ProjectFileContents = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <OutputType>Exe</OutputType>
            <TargetFramework>net8.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
          </PropertyGroup>
        </Project>
        """;

    private const string ProgramFileContents = """
        // VibeEngine2 Empty Game template.
        // Engine runtime startup will be connected in a later step.

        internal static class Program
        {
            private static void Main(string[] args)
            {
                // The initial project scaffold intentionally does not
                // initialize or simulate the engine runtime yet.
            }
        }
        """;

    public string Create(ProjectCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Project name cannot be empty.",
                nameof(request));
        }

        ValidateProjectName(name);

        if (!string.Equals(
                request.TemplateId,
                EmptyTemplateId,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                $"Project template '{request.TemplateId}' is not supported.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            request.ParentDirectory);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            request.ProjectDirectory);

        var parentDirectory =
            Path.GetFullPath(request.ParentDirectory);

        if (!Directory.Exists(parentDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Parent directory does not exist: {parentDirectory}");
        }

        var projectDirectory =
            Path.GetFullPath(
                Path.Combine(parentDirectory, name));

        var requestedProjectDirectory =
            Path.GetFullPath(request.ProjectDirectory);

        if (!PathsEqual(
                projectDirectory,
                requestedProjectDirectory))
        {
            throw new ArgumentException(
                "Project directory does not match the project name and parent directory.",
                nameof(request));
        }

        if (Directory.Exists(projectDirectory) ||
            File.Exists(projectDirectory))
        {
            throw new IOException(
                $"A file or directory already exists at '{projectDirectory}'.");
        }

        // Stage the complete project beside its destination.
        // Publishing it with Directory.Move avoids leaving a partially
        // created project at the final path if generation fails.
        var stagingDirectory =
            Path.Combine(
                parentDirectory,
                $".vibeengine-project-{Guid.NewGuid():N}.tmp");

        Directory.CreateDirectory(stagingDirectory);

        try
        {
            var projectFileName =
                $"{name}.csproj";

            WriteFile(
                Path.Combine(
                    stagingDirectory,
                    projectFileName),
                ProjectFileContents);

            WriteFile(
                Path.Combine(
                    stagingDirectory,
                    "Program.cs"),
                ProgramFileContents);

            if (request.CreateSolution)
            {
                WriteFile(
                    Path.Combine(
                        stagingDirectory,
                        $"{name}.sln"),
                    CreateSolutionContents(
                        name,
                        projectFileName));
            }

            // This fails rather than overwriting a destination that
            // appeared after the initial existence check.
            Directory.Move(
                stagingDirectory,
                projectDirectory);

            return projectDirectory;
        }
        catch
        {
            // Do not let a cleanup error hide the original exception.
            try
            {
                if (Directory.Exists(stagingDirectory))
                {
                    Directory.Delete(
                        stagingDirectory,
                        recursive: true);
                }
            }
            catch
            {
                // Preserve the original generation error.
            }

            throw;
        }
    }

    private static void ValidateProjectName(string name)
    {
        if (name is "." or ".." ||
            name.EndsWith(' ') ||
            name.EndsWith('.') ||
            name.Any(character =>
                char.IsControl(character) ||
                Path.GetInvalidFileNameChars().Contains(character)))
        {
            throw new ArgumentException(
                "Project name contains characters that are not valid in a folder name.");
        }
    }

    private static void WriteFile(
        string path,
        string contents)
    {
        File.WriteAllText(
            path,
            contents,
            new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: false));
    }

    private static string CreateSolutionContents(
        string name,
        string projectFileName)
    {
        // Use the C# project type GUID used by the existing solution.
        const string projectTypeGuid =
            "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";

        var projectGuid =
            Guid.NewGuid()
                .ToString("B")
                .ToUpperInvariant();

        var lines = new[]
        {
            "Microsoft Visual Studio Solution File, Format Version 12.00",
            "# Visual Studio Version 17",
            "VisualStudioVersion = 17.0.31903.59",
            "MinimumVisualStudioVersion = 10.0.40219.1",

            $"Project(\"{projectTypeGuid}\") = \"{name}\", \"{projectFileName}\", \"{projectGuid}\"",
            "EndProject",

            "Global",
            "\tGlobalSection(SolutionConfigurationPlatforms) = preSolution",
            "\t\tDebug|Any CPU = Debug|Any CPU",
            "\t\tRelease|Any CPU = Release|Any CPU",
            "\tEndGlobalSection",

            "\tGlobalSection(ProjectConfigurationPlatforms) = postSolution",
            $"\t\t{projectGuid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU",
            $"\t\t{projectGuid}.Debug|Any CPU.Build.0 = Debug|Any CPU",
            $"\t\t{projectGuid}.Release|Any CPU.ActiveCfg = Release|Any CPU",
            $"\t\t{projectGuid}.Release|Any CPU.Build.0 = Release|Any CPU",
            "\tEndGlobalSection",
            "EndGlobal"
        };

        return string.Join(
            Environment.NewLine,
            lines) + Environment.NewLine;
    }

    private static bool PathsEqual(
        string left,
        string right)
    {
        return string.Equals(
            left,
            right,
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal);
    }
}