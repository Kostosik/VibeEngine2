using Engine.Editor.Projects;

namespace Engine.Tests.Editor;

public sealed class EngineRuntimeBuilderTests
{
    [Fact]
    public async Task BuildAsync_WhenCancelledDuringPublish_PreservesExistingRuntime()
    {
        using var root = new TemporaryDirectory();

        var sourceRoot = FindSourceRoot();
        var outputDirectory = Path.Combine(root.Path, "Runtime");

        Directory.CreateDirectory(outputDirectory);

        var markerPath = Path.Combine(
            outputDirectory,
            "runtime.marker");

        File.WriteAllText(markerPath, "previous-runtime");

        using var cancellation = new CancellationTokenSource();

        var cancellationRequested = 0;

        var exception = await Record.ExceptionAsync(async () =>
            await EngineRuntimeBuilder.BuildAsync(
                sourceRoot,
                outputDirectory,
                    onOutput: (message, _) =>
                    {
                        // Сообщение Publishing ... выдаётся до запуска dotnet.
                        // Отменяем только после появления реального вывода публикации.
                        if (!message.StartsWith(
                                "Publishing ",
                                StringComparison.OrdinalIgnoreCase)
                            && Interlocked.Exchange(
                                ref cancellationRequested, 1) == 0)
                        {
                            cancellation.Cancel();
                        }
                    },
                cancellationToken: cancellation.Token)
            .WaitAsync(TimeSpan.FromSeconds(30)));

        Assert.IsAssignableFrom<OperationCanceledException>(exception);

        Assert.Equal(1, Volatile.Read(ref cancellationRequested));

        Assert.Equal(
            "previous-runtime",
            File.ReadAllText(markerPath));
    }

    private static string FindSourceRoot()
    {
        foreach (var startPath in new[]
                 {
                 AppContext.BaseDirectory,
                 Environment.CurrentDirectory
             })
        {
            for (
                var current = new DirectoryInfo(
                    Path.GetFullPath(startPath));
                current is not null;
                current = current.Parent)
            {
                if (EngineRuntimeBuilder.IsSourceRoot(current.FullName))
                {
                    return current.FullName;
                }
            }
        }

        throw new DirectoryNotFoundException(
            "Could not locate the VibeEngine2 source repository.");
    }

    [Fact]
    public async Task BuildAsync_WhenPublishFails_PreservesExistingRuntime()
    {
        var sourceRoot = CreateTemporaryDirectory();
        var outputRoot = CreateTemporaryDirectory();

        var outputDirectory = Path.Combine(
            outputRoot,
            "EngineRuntime",
            "win-x64");

        Directory.CreateDirectory(outputDirectory);

        var markerPath = Path.Combine(
            outputDirectory,
            "runtime-marker.txt");

        File.WriteAllText(markerPath, "existing runtime");

        try
        {
            File.WriteAllText(
                Path.Combine(sourceRoot, "VibeEngine2.sln"),
                string.Empty);

            foreach (var relativePath in
                     EngineRuntimeBuilder.SourceProjectPaths)
            {
                var projectPath = Path.Combine(
                    sourceRoot,
                    relativePath);

                Directory.CreateDirectory(
                    Path.GetDirectoryName(projectPath)!);

                File.WriteAllText(projectPath, "<Project />");
            }

            // Намеренно повреждаем первый проект,
            // чтобы dotnet publish завершился ошибкой.
            var brokenProjectPath = Path.Combine(
                sourceRoot,
                EngineRuntimeBuilder.SourceProjectPaths[0]);

            File.WriteAllText(brokenProjectPath, "<Project");

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => EngineRuntimeBuilder.BuildAsync(
                    sourceRoot,
                    outputDirectory));

            Assert.Contains(
                "Engine.Hosting.Desktop.csproj",
                exception.Message);

            // Предыдущий runtime должен сохраниться.
            Assert.True(Directory.Exists(outputDirectory));

            Assert.Equal(
                "existing runtime",
                File.ReadAllText(markerPath));

            Assert.Single(
                Directory.EnumerateFiles(outputDirectory));
        }
        finally
        {
            Directory.Delete(sourceRoot, recursive: true);
            Directory.Delete(outputRoot, recursive: true);
        }
    }

    [Fact]
    public void IsSourceRoot_RequiresSolutionAndAllBuildProjects()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            File.WriteAllText(
                Path.Combine(root, "VibeEngine2.sln"),
                string.Empty);

            foreach (var relativePath in
                     EngineRuntimeBuilder.SourceProjectPaths)
            {
                var path = Path.Combine(root, relativePath);

                Directory.CreateDirectory(
                    Path.GetDirectoryName(path)!);

                File.WriteAllText(
                    path,
                    "<Project />");
            }

            Assert.True(
                EngineRuntimeBuilder.IsSourceRoot(root));

            File.Delete(
                Path.Combine(
                    root,
                    EngineRuntimeBuilder.SourceProjectPaths[0]));

            Assert.False(
                EngineRuntimeBuilder.IsSourceRoot(root));
        }
        finally
        {
            Directory.Delete(
                root,
                recursive: true);
        }
    }

    [Fact]
    public void ContainsRequiredAssemblies_RequiresCompleteRuntime()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            foreach (var assembly in
                     EngineRuntimeBuilder.RequiredAssemblies)
            {
                File.WriteAllBytes(
                    Path.Combine(root, assembly),
                    Array.Empty<byte>());
            }

            Assert.True(
                EngineRuntimeBuilder.ContainsRequiredAssemblies(root));

            File.Delete(
                Path.Combine(
                    root,
                    EngineRuntimeBuilder.RequiredAssemblies[0]));

            Assert.False(
                EngineRuntimeBuilder.ContainsRequiredAssemblies(root));

            Assert.NotEmpty(
                EngineRuntimeBuilder.GetMissingAssemblies(root));
        }
        finally
        {
            Directory.Delete(
                root,
                recursive: true);
        }
    }
    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory =
            Directory.CreateTempSubdirectory();

        public string Path => _directory.FullName;

        public void Dispose()
        {
            if (_directory.Exists)
            {
                _directory.Delete(recursive: true);
            }
        }
    }
    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineRuntimeBuilder-{Guid.NewGuid():N}");

        Directory.CreateDirectory(path);

        return path;
    }
}