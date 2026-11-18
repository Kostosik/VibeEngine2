using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Worlds;
using Engine.Worlds.Spatial;
using System.Text;

namespace Engine.Editor.Projects;

/// <summary>
/// Creates a runnable empty game project using a prepared
/// binary distribution of VibeEngine2.
/// </summary>
public sealed class ProjectCreationService
{
    private const string EmptyTemplateId = "empty";

    private readonly string? _engineRuntimeDirectory;


    private readonly Func<string, bool> _rootDirectoryExists;

    public ProjectCreationService(
        string? engineRuntimeDirectory = null,
        Func<string, bool>? rootDirectoryExists = null)
    {
        _engineRuntimeDirectory =
            string.IsNullOrWhiteSpace(engineRuntimeDirectory)
                ? null
                : Path.GetFullPath(engineRuntimeDirectory);

        _rootDirectoryExists =
            rootDirectoryExists ?? Directory.Exists;
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

        var parentRoot =
            Path.GetPathRoot(parentDirectory);

        if (string.IsNullOrWhiteSpace(parentRoot) ||
            !_rootDirectoryExists(parentRoot))
        {
            throw new DirectoryNotFoundException(
                $"Drive or root directory does not exist: '{parentRoot}'.");
        }

        if (File.Exists(parentDirectory))
        {
            throw new IOException(
                $"A file exists at the parent directory path: '{parentDirectory}'.");
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

        // Create missing parent directories only after the
        // engine runtime has been validated.
        Directory.CreateDirectory(parentDirectory);

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

            Directory.CreateDirectory(
                Path.Combine(
                    stagingDirectory,
                    "Assets"));


            var projectFileName = $"{name}.csproj";

            WriteFile(
                Path.Combine(
                    stagingDirectory,
                    projectFileName),
                CreateProjectFileContents(
                    runtimeAssemblies));

            WriteFile(
                Path.Combine(
                    stagingDirectory,
                    $"{name}.vibeproject"),
                    new VibeProjectManifest
                    {
                        FormatVersion =
                            VibeProjectManifest.CurrentFormatVersion,

                        Name = name,

                        StartupProject = projectFileName,
                    }.ToJson());

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

    private static void CreateEmptyStartupScene(
    string projectDirectory)
    {
        var scenePath = Path.Combine(
            projectDirectory,
            VibeProjectManifest.DefaultStartupScenePath);

        var sceneDirectory =
            Path.GetDirectoryName(scenePath)
            ?? throw new InvalidOperationException(
                "Cannot determine the startup scene directory.");

        Directory.CreateDirectory(sceneDirectory);

        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new Engine.Worlds.World(
                new ChunkSize(32, 32),
                ecsWorld);

        var serializers =
            new EcsComponentSerializerRegistry();

        serializers.RegisterEngineComponents();

        var saveLoad =
            new WorldSaveLoadService(serializers);

        saveLoad.Save(
            scenePath,
            world);
    }

    private string ResolveEngineRuntimeDirectory()
    {
        // Явно заданный путь имеет приоритет.
        // Если он неверный, показываем ошибку вместо скрытого fallback.
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

        // Установленный редактор:
        // Editor.Sandbox.exe
        // EngineRuntime\win-x64\*.dll
        var besideEditor = Path.Combine(
            AppContext.BaseDirectory,
            "EngineRuntime",
            "win-x64");

        if (ContainsRequiredAssemblies(besideEditor))
        {
            return Path.GetFullPath(besideEditor);
        }

        // Разработка из клона репозитория:
        // artifacts\engine-runtime\win-x64
        for (
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            current is not null;
            current = current.Parent)
        {
            var candidate = Path.Combine(
                current.FullName,
                "artifacts",
                "engine-runtime",
                "win-x64");

            if (ContainsRequiredAssemblies(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        // Готовой поставки нет. Ищем исходники и собираем runtime.
        var sourceRoot = ResolveEngineSourceRoot();

        var buildOutputDirectory = Path.Combine(
            sourceRoot,
            "artifacts",
            "engine-runtime",
            "win-x64");

        // Повторная проверка нужна, если каталог находится
        // в найденном исходном репозитории, но не попал в поиск выше.
        if (!ContainsRequiredAssemblies(buildOutputDirectory))
        {
            EngineRuntimeBuilder.BuildAsync(
                    sourceRoot,
                    buildOutputDirectory,
                    "Debug")
                .GetAwaiter()
                .GetResult();
        }

        return ValidateRuntimeDirectory(
            buildOutputDirectory);
    }

    private static string ResolveEngineSourceRoot()
    {
        var configuredSource =
            Environment.GetEnvironmentVariable(
                "VIBEENGINE_SOURCE_DIR");

        if (!string.IsNullOrWhiteSpace(configuredSource))
        {
            var root = Path.GetFullPath(configuredSource);

            if (!EngineRuntimeBuilder.IsSourceRoot(root))
            {
                throw new DirectoryNotFoundException(
                    $"VIBEENGINE_SOURCE_DIR does not point to a valid " +
                    $"VibeEngine2 source repository: {root}");
            }

            return root;
        }

        // Поддерживаем запуск редактора из Visual Studio
        // и из каталога внутри репозитория.
        foreach (var startPath in new[]
                 {
                 Environment.CurrentDirectory,
                 AppContext.BaseDirectory
             })
        {
            for (
                var current = new DirectoryInfo(
                    Path.GetFullPath(startPath));
                current is not null;
                current = current.Parent)
            {
                if (EngineRuntimeBuilder.IsSourceRoot(
                        current.FullName))
                {
                    return current.FullName;
                }
            }
        }

        throw new DirectoryNotFoundException(
            "The VibeEngine2 runtime distribution was not found, " +
            "and the engine source repository could not be located. " +
            "Set VIBEENGINE_SOURCE_DIR to the VibeEngine2 source root.");
    }

    private static string ValidateRuntimeDirectory(
        string directory)
    {
        if (!ContainsRequiredAssemblies(directory))
        {
            var missing =
                EngineRuntimeBuilder.GetMissingAssemblies(
                    directory);

            throw new DirectoryNotFoundException(
                $"Engine runtime directory '{directory}' is incomplete. " +
                $"Missing: {string.Join(", ", missing)}");
        }

        return Path.GetFullPath(directory);
    }

    private static bool ContainsRequiredAssemblies(
        string directory)
    {
        return EngineRuntimeBuilder.ContainsRequiredAssemblies(
            directory);
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
        "    <PlatformTarget>x64</PlatformTarget>",
        "    <ImplicitUsings>enable</ImplicitUsings>",
        "    <Nullable>enable</Nullable>",
        "  </PropertyGroup>",
        "",
        "  <ItemGroup>"
    };

        // Managed assemblies are referenced by the game project.
        foreach (var assemblyPath in runtimeAssemblies)
        {
            if (!IsManagedAssembly(assemblyPath))
            {
                continue;
            }

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
            "  <ItemGroup>"
            });

        // Native DLLs are copied beside the generated game executable.
        foreach (var nativePath in runtimeAssemblies)
        {
            if (IsManagedAssembly(nativePath))
            {
                continue;
            }

            var nativeFile =
                Path.GetFileName(nativePath);

            lines.Add(
                $"    <None Update=\"Engine\\{XmlEscape(nativeFile)}\">");

            lines.Add(
                "      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>");

            lines.Add(
                "      <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>");

            lines.Add(
                $"      <TargetPath>{XmlEscape(nativeFile)}</TargetPath>");

            lines.Add(
                "    </None>");
        }

        // Copy project assets to the game output directory.
        lines.Add(
            "    <None Update=\"Assets/**/*\">");

        lines.Add(
            "      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>");

        lines.Add(
            "      <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>");

        lines.Add(
            "    </None>");

        // Copy the project manifest beside the game executable.
        lines.Add(
            "    <None Update=\"*.vibeproject\">");

        lines.Add(
            "      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>");

        lines.Add(
            "      <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>");

        lines.Add(
            "    </None>");

        lines.AddRange(
            new[]
            {
            "  </ItemGroup>",
            "</Project>"
            });

        return string.Join(
            Environment.NewLine,
            lines) + Environment.NewLine;
    }

    private static bool IsManagedAssembly(string path)
    {
        try
        {
            _ = System.Reflection.AssemblyName.GetAssemblyName(path);
            return true;
        }
        catch (BadImageFormatException)
        {
            return false;
        }
    }

    private static string CreateProgramContents(
    string projectName)
    {
        var title = EscapeCSharpString(projectName);

        return $$"""
    using Engine.Hosting.Desktop;
    using Engine.Hosting.Desktop.SilkNet;
    using Engine.Serialization.Content;
    using Engine.Serialization.UI;
    using Engine.UI.Assets;

    namespace GameProject;

    internal static class Program
    {
        private static void Main(string[] args)
        {
            SilkNetDesktopHost.Run(
                (DesktopGameHostContext context) =>
                    new GameMain(context.Runtime),
                new DesktopGameHostOptions
                {
                    Title = "{{title}}",

                    ConfigureContentLoaders = loaders =>
                    {
                        loaders.Register(
                            new BinaryContentLoader<UiAsset>(
                                new UiAssetSerializer(),
                                asset => asset.Extension.Equals(
                                    ".ui",
                                    StringComparison.OrdinalIgnoreCase)));
                    }
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