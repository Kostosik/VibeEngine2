using System.Text;

namespace Engine.Editor.Projects;

/// <summary>
/// Creates a runnable empty game project using a prepared
/// binary distribution of VibeEngine2.
/// </summary>
public sealed class ProjectCreationService
{
    private const string EmptyTemplateId = "empty";

    private static readonly string[] RequiredRuntimeAssemblies =
    {
        "Engine.Hosting.Desktop.dll",
        "Engine.Runtime.dll",
        "Engine.Graphics.OpenGL.dll",
        "Engine.Audio.OpenAL.dll",
        "Engine.Input.SilkNet.dll"
    };

    private readonly string? _engineRuntimeDirectory;

    public ProjectCreationService(
        string? engineRuntimeDirectory = null)
    {
        _engineRuntimeDirectory =
            string.IsNullOrWhiteSpace(engineRuntimeDirectory)
                ? null
                : Path.GetFullPath(engineRuntimeDirectory);
    }

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

        // Resolve all required binaries before creating any project files.
        var runtimeDirectory =
            ResolveEngineRuntimeDirectory();

        var runtimeAssemblies =
            Directory.GetFiles(
                    runtimeDirectory,
                    "*.dll",
                    SearchOption.TopDirectoryOnly)
                .OrderBy(
                    Path.GetFileName,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        if (runtimeAssemblies.Length == 0)
        {
            throw new InvalidOperationException(
                $"No runtime assemblies were found in '{runtimeDirectory}'.");
        }

        var stagingDirectory =
            Path.Combine(
                parentDirectory,
                $".vibeengine-project-{Guid.NewGuid():N}.tmp");

        Directory.CreateDirectory(stagingDirectory);

        try
        {
            var engineDirectory =
                Path.Combine(
                    stagingDirectory,
                    "Engine");

            CopyDirectory(
                runtimeDirectory,
                engineDirectory);

            var projectFileName =
                $"{name}.csproj";

            WriteFile(
                Path.Combine(
                    stagingDirectory,
                    projectFileName),
                CreateProjectFileContents(
                    runtimeAssemblies));

            WriteFile(
                Path.Combine(
                    stagingDirectory,
                    "Program.cs"),
                CreateProgramContents(name));

            WriteFile(
                Path.Combine(
                    stagingDirectory,
                    "GameMain.cs"),
                CreateGameContents());

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

            Directory.Move(
                stagingDirectory,
                projectDirectory);

            return projectDirectory;
        }
        catch
        {
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
                // Preserve the original exception.
            }

            throw;
        }
    }

    private string ResolveEngineRuntimeDirectory()
    {
        if (_engineRuntimeDirectory is not null)
        {
            return ValidateRuntimeDirectory(
                _engineRuntimeDirectory);
        }

        var configuredDirectory =
            Environment.GetEnvironmentVariable(
                "VIBEENGINE_RUNTIME_DIR");

        if (!string.IsNullOrWhiteSpace(configuredDirectory))
        {
            return ValidateRuntimeDirectory(
                Path.GetFullPath(configuredDirectory));
        }

        // Installed editor layout:
        // Editor.Sandbox.exe
        // EngineRuntime\win-x64\*.dll
        var besideEditor =
            Path.Combine(
                AppContext.BaseDirectory,
                "EngineRuntime",
                "win-x64");

        if (ContainsRequiredAssemblies(besideEditor))
        {
            return Path.GetFullPath(besideEditor);
        }

        // Development layout:
        // repository\artifacts\engine-runtime\win-x64
        // Search ancestors so the editor can be launched from
        // Visual Studio without hardcoding a repository path.
        for (
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            current is not null;
            current = current.Parent)
        {
            var candidate =
                Path.Combine(
                    current.FullName,
                    "artifacts",
                    "engine-runtime",
                    "win-x64");

            if (ContainsRequiredAssemblies(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        throw new DirectoryNotFoundException(
            "The VibeEngine2 runtime distribution was not found. " +
            "Prepare it with tools\\BuildEngineRuntime.ps1, " +
            "or configure VIBEENGINE_RUNTIME_DIR.");
    }

    private static string ValidateRuntimeDirectory(
        string directory)
    {
        if (!ContainsRequiredAssemblies(directory))
        {
            var missing =
                RequiredRuntimeAssemblies
                    .Where(
                        file => !File.Exists(
                            Path.Combine(directory, file)))
                    .ToArray();

            throw new DirectoryNotFoundException(
                $"Engine runtime directory '{directory}' is incomplete. " +
                $"Missing: {string.Join(", ", missing)}");
        }

        return Path.GetFullPath(directory);
    }

    private static bool ContainsRequiredAssemblies(
        string directory)
    {
        return Directory.Exists(directory) &&
               RequiredRuntimeAssemblies.All(
                   file => File.Exists(
                       Path.Combine(directory, file)));
    }

    private static string CreateProjectFileContents(
        IReadOnlyList<string> runtimeAssemblies)
    {
        var lines = new List<string>
        {
            "<Project Sdk=\"Microsoft.NET.Sdk\">",
            "  <PropertyGroup>",
            "    <OutputType>Exe</OutputType>",
            "    <TargetFramework>net8.0</TargetFramework>",
            "    <ImplicitUsings>enable</ImplicitUsings>",
            "    <Nullable>enable</Nullable>",
            "  </PropertyGroup>",
            "",
            "  <ItemGroup>"
        };

        foreach (var assemblyPath in runtimeAssemblies)
        {
            var assemblyFile =
                Path.GetFileName(assemblyPath);

            var assemblyName =
                Path.GetFileNameWithoutExtension(assemblyFile);

            lines.Add(
                $"    <Reference Include=\"{XmlEscape(assemblyName)}\">");

            lines.Add(
                $"      <HintPath>Engine\\{XmlEscape(assemblyFile)}</HintPath>");

            lines.Add(
                "      <Private>true</Private>");

            lines.Add(
                "    </Reference>");
        }

        lines.AddRange(
            new[]
            {
        "  </ItemGroup>",
        "",
        "  <ItemGroup>",
        "    <None Update=\"Engine\\runtimes\\win-x64\\native\\**\\*.dll\">",
        "      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>",
        "      <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>",
        "      <TargetPath>runtimes\\win-x64\\native\\%(Filename)%(Extension)</TargetPath>",
        "    </None>",
        "  </ItemGroup>",
        "</Project>"
            });

        return string.Join(
            Environment.NewLine,
            lines) + Environment.NewLine;
    }

    private static string CreateProgramContents(
        string projectName)
    {
        var title =
            EscapeCSharpString(projectName);

        return $$"""
            using Engine.Hosting.Desktop;
            using Engine.Runtime;

            namespace GameProject;

            internal static class Program
            {
                private static void Main(string[] args)
                {
                    DesktopGameHost.Run(
                        runtime => new GameMain(runtime),
                        new DesktopGameHostOptions
                        {
                            Title = "{{title}}"
                        });
                }
            }
            """ + Environment.NewLine;
    }

    private static string CreateGameContents()
    {
        return """
            using Engine.Core.Math;
            using Engine.Graphics.Commands;
            using Engine.Graphics.DebugGraphics;
            using Engine.Runtime;

            namespace GameProject;

            internal sealed class GameMain : EngineGame
            {
                public GameMain(EngineRuntime runtime)
                    : base(runtime)
                {
                }

                protected override void OnInitialize()
                {
                    Console.WriteLine(
                        "VibeEngine2 game initialized.");
                }

                protected override void OnRender(
                    double interpolationAlpha)
                {
                    Runtime.Services.Graphics.Submit(
                        new DrawDebugTextCommand(
                            "VibeEngine2 game is running",
                            new Vector2(24.0f, 24.0f),
                            DebugColor.White,
                            2.0f,
                            DebugRenderSpace.Screen));
                }

                protected override void OnShutdown()
                {
                    Console.WriteLine(
                        "VibeEngine2 game stopped.");
                }
            }
            """ + Environment.NewLine;
    }

    private static void CopyDirectory(
        string sourceDirectory,
        string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var file in Directory.EnumerateFiles(
                     sourceDirectory,
                     "*",
                     SearchOption.AllDirectories))
        {
            var relativePath =
                Path.GetRelativePath(
                    sourceDirectory,
                    file);

            var destinationFile =
                Path.Combine(
                    destinationDirectory,
                    relativePath);

            var destinationParent =
                Path.GetDirectoryName(destinationFile);

            if (!string.IsNullOrWhiteSpace(destinationParent))
            {
                Directory.CreateDirectory(destinationParent);
            }

            File.Copy(
                file,
                destinationFile);
        }
    }

    private static string EscapeCSharpString(
        string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }

    private static string XmlEscape(
        string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&apos;", StringComparison.Ordinal);
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