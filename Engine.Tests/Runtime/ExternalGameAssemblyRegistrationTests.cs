
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
        // Используем собранные библиотеки движка из каталога тестов.
        // Пользовательская игра будет отдельной сборкой.
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

        var references = string.Join(
            Environment.NewLine,
            assemblies.Select(path =>
                $$"""
                  <Reference Include="{{SecurityElement.Escape(Path.GetFileNameWithoutExtension(path))}}">
                    <HintPath>{{SecurityElement.Escape(path)}}</HintPath>
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
        var startInfo = new ProcessStartInfo
        {
            FileName =
                Environment.GetEnvironmentVariable("DOTNET_HOST_PATH")
                ?? "dotnet",
            WorkingDirectory = directory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(
            Path.Combine(directory, "ExternalGame.csproj"));
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add("Release");

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
                "Failed to start the external game project.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit(120_000))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();

            throw new TimeoutException(
                "Building or running the external game timed out."
                + Environment.NewLine
                + ReadOutput());
        }

        // Дожидаемся завершения обработки асинхронного вывода.
        process.WaitForExit();

        return (process.ExitCode, ReadOutput());
    }

    private const string GameSource = """
        using Engine.Core.Math;
        using Engine.Core.Systems;
        using Engine.Core.Time;
        using Engine.ECS.Entities;
        using Engine.Runtime;

        namespace ExternalGame;

        internal static class Program
        {
            private static int Main()
            {
                using var runtime = new EngineRuntime(
                    new EngineRuntimeOptions
                    {
                        ToolingEnabled = false
                    });

                var game = new ProbeGame(runtime);

                game.Initialize();

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
