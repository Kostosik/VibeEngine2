
using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Worlds;
using Engine.Worlds.Spatial;
using System.Diagnostics;
using System.Security;
using System.Text;

namespace Engine.Tests.Runtime;

public sealed class ExternalGameAssemblyRegistrationTests
{
    [Fact]
    public void SeparateGameAssembly_CanRegisterAndExecuteCustomSystem()
    {
        var projectDirectory = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineRegistrationProbe-{Guid.NewGuid():N}");

        Directory.CreateDirectory(projectDirectory);

        try
        {
            CreateGameProject(projectDirectory);

            var result = BuildAndRunGame(projectDirectory);

            Assert.True(
                result.ExitCode == 0,
                $"External game failed with exit code {result.ExitCode}."
                + Environment.NewLine
                + result.Output);

            Assert.Contains(
                "PASS: external game component and system executed.",
                result.Output);
        }
        finally
        {
            try
            {
                if (Directory.Exists(projectDirectory))
                {
                    Directory.Delete(projectDirectory, recursive: true);
                }
            }
            catch
            {
                // Не скрывать исходную ошибку теста из-за очистки.
            }
        }
    }

    private static void CreateGameProject(string directory)
    {
        var assemblies = Directory
            .EnumerateFiles(
                AppContext.BaseDirectory,
                "Engine.*.dll",
                SearchOption.TopDirectoryOnly)
            .Where(path =>
            {
                var name = Path.GetFileName(path);

                return !string.Equals(
                           name,
                           "Engine.Tests.dll",
                           StringComparison.OrdinalIgnoreCase)
                       && !name.StartsWith(
                           "Engine.Editor",
                           StringComparison.OrdinalIgnoreCase)
                       && !string.Equals(
                           name,
                           "Engine.Audio.OpenAL.dll",
                           StringComparison.OrdinalIgnoreCase)
                       && !string.Equals(
                           name,
                           "Engine.Graphics.OpenGL.dll",
                           StringComparison.OrdinalIgnoreCase)
                       && !string.Equals(
                           name,
                           "Engine.Input.SilkNet.dll",
                           StringComparison.OrdinalIgnoreCase)
                       && !string.Equals(
                           name,
                           "Engine.Hosting.Desktop.dll",
                           StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(
                Path.GetFileName,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (assemblies.Length == 0)
        {
            throw new InvalidOperationException(
                "Engine assemblies were not found in the test output directory.");
        }

        var engineDirectory = Path.Combine(
            directory,
            "Engine");

        Directory.CreateDirectory(engineDirectory);

        foreach (var assembly in assemblies)
        {
            File.Copy(
                assembly,
                Path.Combine(
                    engineDirectory,
                    Path.GetFileName(assembly)));
        }

        var assetsDirectory = Path.Combine(
    directory,
    "Assets",
    "Probe");

        Directory.CreateDirectory(assetsDirectory);

        File.WriteAllText(
            Path.Combine(assetsDirectory, "marker.txt"),
            "outside-repository-asset",
            new UTF8Encoding(false));

        var scenesDirectory = Path.Combine(
            directory,
            "Scenes");

        Directory.CreateDirectory(scenesDirectory);

        CreateStartupScene(
            Path.Combine(scenesDirectory, "Main.world"));

        var references = string.Join(
            Environment.NewLine,
            assemblies.Select(path =>
                $$"""
              <Reference Include="{{SecurityElement.Escape(Path.GetFileNameWithoutExtension(path))}}">
                <HintPath>Engine/{{SecurityElement.Escape(Path.GetFileName(path))}}</HintPath>
                <Private>true</Private>
              </Reference>
              """));

        var projectContents = $$"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <OutputType>Exe</OutputType>
            <TargetFramework>net8.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
          </PropertyGroup>
          <ItemGroup>
        {{references}}
          </ItemGroup>

                  <ItemGroup>
          <None Update="Assets/**/*">
            <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
            <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
          </None>
          <None Update="Scenes/**/*">
            <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
            <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
          </None>
        </ItemGroup>

        </Project>
        """;

        File.WriteAllText(
            Path.Combine(directory, "ExternalGame.csproj"),
            projectContents,
            new UTF8Encoding(false));

        File.WriteAllText(
            Path.Combine(directory, "Program.cs"),
            GameSource,
            new UTF8Encoding(false));
    }

    private static (int ExitCode, string Output) BuildAndRunGame(
    string directory)
    {
        var dotnet =
            Environment.GetEnvironmentVariable("DOTNET_HOST_PATH")
            ?? "dotnet";

        var publishDirectory =
            Path.Combine(directory, "publish");

        // Сначала публикуем внешний игровой проект.
        var publishResult = RunProcess(
            directory,
            dotnet,
            "publish",
            Path.Combine(directory, "ExternalGame.csproj"),
            "--configuration",
            "Release",
            "--output",
            publishDirectory);

        if (publishResult.ExitCode != 0)
        {
            return publishResult;
        }

        // Затем запускаем опубликованную игру,
        // а не исходный проект.
        var runResult = RunProcess(
            publishDirectory,
            dotnet,
            Path.Combine(publishDirectory, "ExternalGame.dll"));

        return (
            runResult.ExitCode,
            publishResult.Output
            + Environment.NewLine
            + runResult.Output);
    }

    private static (int ExitCode, string Output) RunProcess(
    string workingDirectory,
    string fileName,
    params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process
        {
            StartInfo = startInfo
        };

        var output = new StringBuilder();
        var outputLock = new object();

        void AppendOutput(string? line)
        {
            if (line is null)
            {
                return;
            }

            lock (outputLock)
            {
                output.AppendLine(line);
            }
        }

        string ReadOutput()
        {
            lock (outputLock)
            {
                return output.ToString();
            }
        }

        process.OutputDataReceived +=
            (_, args) => AppendOutput(args.Data);

        process.ErrorDataReceived +=
            (_, args) => AppendOutput(args.Data);

        if (!process.Start())
        {
            throw new InvalidOperationException(
                $"Failed to start process: {fileName}");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit(120_000))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();

            throw new TimeoutException(
                "External game process timed out."
                + Environment.NewLine
                + ReadOutput());
        }

        // Дожидаемся обработки асинхронного вывода.
        process.WaitForExit();

        return (process.ExitCode, ReadOutput());
    }

    private static void CreateStartupScene(string path)
    {
        var serializers =
            new EcsComponentSerializerRegistry();

        serializers.RegisterEngineComponents();

        var saveLoad =
            new WorldSaveLoadService(serializers);

        using var ecsWorld =
            new Engine.ECS.World();

        using var world =
            new Engine.Worlds.World(
                new ChunkSize(4, 4),
                ecsWorld);

        var entity = ecsWorld.CreateEntity();

        ecsWorld.Add(
            entity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromInt(12),
                    Fixed32.FromInt(34))));

        saveLoad.Save(path, world);
    }

    private const string GameSource = """
        using Engine.Core.Math;
        using Engine.Core.Systems;
        using Engine.Core.Time;
        using Engine.ECS.Entities;
        using Engine.Runtime;
        using Engine.ECS.Components;
        using Engine.Serialization.SaveLoad.Ecs;
        using Engine.Serialization.SaveLoad.Worlds;
        using Engine.Worlds.Spatial;


        namespace ExternalGame;

        internal static class Program
        {
            private static int Main()
            {
        using var runtime = new EngineRuntime(
            new EngineRuntimeOptions
            {
                ToolingEnabled = false,
                ChunkSize = new ChunkSize(4, 4)
            });

        var game = new ProbeGame(runtime);

                game.Initialize();

                        var sceneEntity = new EntityId(1, 1);

        if (!runtime.EcsWorld.Exists(sceneEntity))
        {
            throw new InvalidOperationException(
                "Published startup scene entity was not loaded.");
        }

        var position =
            runtime.EcsWorld
                .Get<WorldTransform2D>(sceneEntity)
                .Position;

        if (position.X != Fixed32.FromInt(12) ||
            position.Y != Fixed32.FromInt(34))
        {
            throw new InvalidOperationException(
                "Published startup scene position is incorrect.");
        }

        var assetPath = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "Probe",
            "marker.txt");

        if (!File.Exists(assetPath) ||
            File.ReadAllText(assetPath) !=
                "outside-repository-asset")
        {
            throw new InvalidOperationException(
                "Published game asset was not loaded.");
        }

        Console.WriteLine(
            "PASS: published project asset and startup scene loaded.");

        game.FixedUpdate(
        new SimulationTime(
            Fixed32.FromRatio(1, 20),
            new Tick(1)));

                var component =
                    runtime.EcsWorld.Get<GameComponent>(game.Entity);

                if (component.ExecutionCount != 1)
                {
                    throw new InvalidOperationException(
                        $"Expected one system execution, but got "
                        + $"{component.ExecutionCount}.");
                }

                game.Shutdown();

                Console.WriteLine(
                    "PASS: external game component and system executed.");

                return 0;
            }
        }

        internal struct GameComponent
        {
            public int ExecutionCount;
        }

        internal sealed class GameFixedSystem : IFixedUpdateSystem
        {
            private readonly Engine.ECS.World _world;
            private readonly EntityId _entity;

            public GameFixedSystem(
                Engine.ECS.World world,
                EntityId entity)
            {
                _world = world;
                _entity = entity;
            }

            public void FixedUpdate(FixedSystemContext context)
            {
                ref var component =
                    ref _world.Get<GameComponent>(_entity);

                component.ExecutionCount++;
            }
        }

        internal sealed class ProbeGame : EngineGame
        {
            public EntityId Entity { get; private set; }

            public ProbeGame(EngineRuntime runtime)
                : base(runtime)
            {
            }

            protected override void ConfigureRuntime()
            {
                    var serializers =
            new EcsComponentSerializerRegistry();

        serializers.RegisterEngineComponents();

        var saveLoad =
            new WorldSaveLoadService(serializers);

        saveLoad.Load(
            Path.Combine(
                AppContext.BaseDirectory,
                "Scenes",
                "Main.world"),
            Runtime.World);

                Entity = Runtime.EcsWorld.CreateEntity();

                Runtime.EcsWorld.Add(
                    Entity,
                    new GameComponent());

                Runtime.Scheduler.Add(
                    new GameFixedSystem(
                        Runtime.EcsWorld,
                        Entity),
                    SystemPhase.FixedUpdate);
            }
        }
        """;
}
