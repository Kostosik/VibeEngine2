using Engine.Editor.Projects;

namespace Engine.Tests.Editor;

public sealed class EngineRuntimeBuilderTests
{
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

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineRuntimeBuilder-{Guid.NewGuid():N}");

        Directory.CreateDirectory(path);

        return path;
    }
}