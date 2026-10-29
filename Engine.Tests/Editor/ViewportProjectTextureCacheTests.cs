using System.Buffers.Binary;
using System.Reflection;
using Engine.Editor.UI.Workspace;
using Engine.Graphics.Resources;

namespace Engine.Tests.Editor;

public sealed class ViewportProjectTextureCacheTests
{
    [Fact]
    public void ProjectSwitchAndReleaseResources_DestroyCachedTextures()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"VibeEngineTextureCache-{Guid.NewGuid():N}");

        var firstAssets = Path.Combine(root, "ProjectA", "Assets");
        var secondAssets = Path.Combine(root, "ProjectB", "Assets");

        Directory.CreateDirectory(firstAssets);
        Directory.CreateDirectory(secondAssets);

        try
        {
            var image = CreateOnePixelBmp();

            File.WriteAllBytes(
                Path.Combine(firstAssets, "pixel.bmp"),
                image);

            File.WriteAllBytes(
                Path.Combine(secondAssets, "pixel.bmp"),
                image);

            var textureManager = new TestTextureManager();

            var cacheType = typeof(ViewportPanelView).Assembly.GetType(
                "Engine.Editor.UI.Workspace.ViewportProjectTextureCache");

            Assert.NotNull(cacheType);

            var constructor = cacheType!.GetConstructor(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(ITextureManager) },
                modifiers: null);

            Assert.NotNull(constructor);

            var cache = constructor!.Invoke(
                new object[] { textureManager });

            var setAssetsRoot = cacheType.GetMethod("SetAssetsRoot")!;
            var releaseResources = cacheType.GetMethod("ReleaseResources")!;

            // Загружаем текстуру первого проекта.
            setAssetsRoot.Invoke(cache, new object[] { firstAssets });

            var firstTexture = LoadTexture(cacheType, cache);

            Assert.True(textureManager.Exists(firstTexture));
            Assert.Equal(1, textureManager.CreateCount);

            // Смена проекта должна освободить текстуры старого проекта.
            setAssetsRoot.Invoke(cache, new object[] { secondAssets });

            Assert.False(textureManager.Exists(firstTexture));
            Assert.Equal(1, textureManager.DestroyCount);

            // Новый проект получает собственный ресурс.
            var secondTexture = LoadTexture(cacheType, cache);

            Assert.NotEqual(firstTexture, secondTexture);
            Assert.True(textureManager.Exists(secondTexture));

            // Закрытие панели освобождает и ресурсы нового проекта.
            releaseResources.Invoke(cache, null);

            Assert.False(textureManager.Exists(secondTexture));
            Assert.Equal(2, textureManager.DestroyCount);

            // Повторное освобождение не должно уничтожать ресурсы повторно.
            releaseResources.Invoke(cache, null);

            Assert.Equal(2, textureManager.DestroyCount);
            Assert.False(
                (bool)cacheType.GetProperty("HasAssetsRoot")!
                    .GetValue(cache)!);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static TextureHandle LoadTexture(
        Type cacheType,
        object cache)
    {
        var arguments = new object?[]
        {
            "pixel.bmp",
            TextureHandle.Invalid
        };

        var loaded = (bool)cacheType
            .GetMethod("TryLoad")!
            .Invoke(cache, arguments)!;

        Assert.True(loaded);

        return Assert.IsType<TextureHandle>(arguments[1]);
    }

    private static byte[] CreateOnePixelBmp()
    {
        var data = new byte[58];

        data[0] = (byte)'B';
        data[1] = (byte)'M';

        BinaryPrimitives.WriteInt32LittleEndian(
            data.AsSpan(2), data.Length);

        BinaryPrimitives.WriteInt32LittleEndian(
            data.AsSpan(10), 54);

        BinaryPrimitives.WriteInt32LittleEndian(
            data.AsSpan(14), 40);

        BinaryPrimitives.WriteInt32LittleEndian(
            data.AsSpan(18), 1);

        BinaryPrimitives.WriteInt32LittleEndian(
            data.AsSpan(22), 1);

        BinaryPrimitives.WriteInt16LittleEndian(
            data.AsSpan(26), 1);

        BinaryPrimitives.WriteInt16LittleEndian(
            data.AsSpan(28), 24);

        BinaryPrimitives.WriteInt32LittleEndian(
            data.AsSpan(34), 4);

        // Один пиксель BGR и байт выравнивания строки.
        data[54] = 30;
        data[55] = 60;
        data[56] = 90;

        return data;
    }

    private sealed class TestTextureManager : ITextureManager
    {
        private uint _nextHandle;

        private readonly Dictionary<TextureHandle, TextureDescription>
            _textures = new();

        public int CreateCount { get; private set; }
        public int DestroyCount { get; private set; }

        public TextureHandle Create(TextureDescription description)
        {
            var handle = new TextureHandle(++_nextHandle);

            _textures.Add(handle, description);
            CreateCount++;

            return handle;
        }

        public TextureHandle Create(TextureData data)
        {
            return Create(data.Description);
        }

        public bool Exists(TextureHandle texture)
        {
            return _textures.ContainsKey(texture);
        }

        public TextureDescription GetDescription(TextureHandle texture)
        {
            return _textures[texture];
        }

        public void Destroy(TextureHandle texture)
        {
            if (_textures.Remove(texture))
            {
                DestroyCount++;
            }
        }
    }
}