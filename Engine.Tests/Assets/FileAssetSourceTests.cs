using Engine.Core.Assets;

namespace Engine.Tests.Assets;

public sealed class FileAssetSourceTests
{
    [Fact]
    public async Task LoadAndLoadAsync_RejectSymbolicLinkAssetRoot()
    {
        var tempRoot = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineAssetRootSymlink-{Guid.NewGuid():N}");

        var targetRoot = Path.Combine(tempRoot, "ExternalAssets");
        var assetRoot = Path.Combine(tempRoot, "Assets");

        Directory.CreateDirectory(targetRoot);

        File.WriteAllBytes(
            Path.Combine(targetRoot, "test.bin"),
            [1, 2, 3]);

        try
        {
            try
            {
                Directory.CreateSymbolicLink(
                    assetRoot,
                    targetRoot);
            }
            catch (Exception exception) when (
                exception is UnauthorizedAccessException or
                IOException or
                PlatformNotSupportedException)
            {
                // Symbolic links may be unavailable on this machine.
                return;
            }

            var source = new FileAssetSource(assetRoot);
            var path = new AssetPath("test.bin");

            Assert.Throws<InvalidOperationException>(
                () => source.Load(path));

            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await source.LoadAsync(path));
        }
        finally
        {
            if (Directory.Exists(assetRoot))
            {
                Directory.Delete(assetRoot);
            }

            if (Directory.Exists(targetRoot))
            {
                Directory.Delete(targetRoot, recursive: true);
            }

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot);
            }
        }
    }

    [Fact]
    public async Task LoadAndLoadAsync_RejectSymbolicLinkOutsideAssetRoot()
    {
        var parent = Directory.CreateTempSubdirectory();

        var root = Path.Combine(parent.FullName, "Assets");
        var outside = Path.Combine(parent.FullName, "Outside");

        Directory.CreateDirectory(root);
        Directory.CreateDirectory(outside);

        File.WriteAllText(
            Path.Combine(outside, "secret.txt"),
            "outside");

        var link = Path.Combine(root, "Linked");

        try
        {
            Directory.CreateSymbolicLink(link, outside);
        }
        catch (Exception exception) when (
            exception is UnauthorizedAccessException
                or PlatformNotSupportedException
                or IOException)
        {
            // Система не разрешает создавать символические ссылки.
            return;
        }

        var source = new FileAssetSource(root);
        var assetPath = new AssetPath("Linked/secret.txt");

        Assert.Throws<InvalidOperationException>(
            () => source.Load(assetPath));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await source.LoadAsync(assetPath));
    }

    [Fact]
    public async Task LoadRejectsPathOnDifferentDrive()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var parent = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        var root = Path.Combine(parent, "Assets");
        Directory.CreateDirectory(root);

        try
        {
            var rootPath = Path.GetPathRoot(root)!;

            var currentDrive =
                rootPath.Length >= 2 && rootPath[1] == ':'
                    ? char.ToUpperInvariant(rootPath[0])
                    : '\0';

            var otherDrive = currentDrive == 'C' ? 'D' : 'C';

            var assetPath = new AssetPath(
                $"{otherDrive}:/outside.txt");

            var source = new FileAssetSource(root);

            Assert.Throws<InvalidOperationException>(
                () => source.Load(assetPath));

            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await source.LoadAsync(assetPath));
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    [Fact]
    public void LoadRejectsPathOutsideAssetRoot()
    {
        var parent = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        var root = Path.Combine(parent, "Assets");
        Directory.CreateDirectory(root);

        var outsideFile = Path.Combine(parent, "outside.txt");
        File.WriteAllText(outsideFile, "outside");

        try
        {
            var source = new FileAssetSource(root);

            Assert.Throws<InvalidOperationException>(
                () => source.Load(new AssetPath("../outside.txt")));
        }
        finally
        {
            Directory.Delete(parent, true);
        }
    }

    [Fact]
    public async Task LoadAsyncRejectsPathOutsideAssetRoot()
    {
        var parent = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        var root = Path.Combine(parent, "Assets");
        Directory.CreateDirectory(root);

        var outsideFile = Path.Combine(parent, "outside.txt");
        File.WriteAllText(outsideFile, "outside");

        try
        {
            var source = new FileAssetSource(root);

            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await source.LoadAsync(
                    new AssetPath("../outside.txt")));
        }
        finally
        {
            Directory.Delete(parent, true);
        }
    }

    [Fact]
    public void LoadReturnsFileContent()
    {
        var root =
            Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString());

        Directory.CreateDirectory(root);

        try
        {
            var filePath =
                Path.Combine(
                    root,
                    "test.txt");

            File.WriteAllBytes(
                filePath,
                new byte[]
                {
                    1,
                    2,
                    3
                });

            var source =
                new FileAssetSource(root);

            var data =
                source.Load(
                    new AssetPath("test.txt"));

            Assert.Equal(
                new byte[]
                {
                    1,
                    2,
                    3
                },
                data.ToArray());
        }
        finally
        {
            Directory.Delete(
                root,
                true);
        }
    }

    [Fact]
    public void LoadThrowsForMissingFile()
    {
        var root =
            Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString());

        Directory.CreateDirectory(root);

        try
        {
            var source =
                new FileAssetSource(root);

            Assert.Throws<FileNotFoundException>(
                () =>
                {
                    source.Load(
                        new AssetPath(
                            "missing.txt"));
                });
        }
        finally
        {
            Directory.Delete(
                root,
                true);
        }
    }
}