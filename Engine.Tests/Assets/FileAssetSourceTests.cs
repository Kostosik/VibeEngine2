using Engine.Core.Assets;

namespace Engine.Tests.Assets;

public sealed class FileAssetSourceTests
{
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