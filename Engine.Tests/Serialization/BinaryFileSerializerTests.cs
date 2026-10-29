using Engine.Core.Time;
using Engine.Serialization.Binary;
using Engine.Serialization.Types;

namespace Engine.Tests.Serialization;

public sealed class BinaryFileSerializerTests
{
    [Fact]
    public void Save_WhenReplacingLockedFileFails_PreservesOriginalAndRemovesTemporaryFile()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"VibeSerializer-{Guid.NewGuid():N}");

        Directory.CreateDirectory(root);

        var path = Path.Combine(root, "probe.vbe");

        byte[] originalContents =
        [
            0x56, 0x49, 0x42, 0x45, 0x01, 0x02, 0x03
        ];

        try
        {
            File.WriteAllBytes(path, originalContents);

            var serializer = new TickSerializer();

            // Запрещаем замену существующего файла на Windows.
            using (var lockedFile = new FileStream(
                       path,
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.None))
            {
                var exception = Record.Exception(
                    () => BinaryFileSerializer.Save(
                        path,
                        new Tick(200),
                        serializer));

                Assert.NotNull(exception);

                Assert.True(
                    exception is IOException or UnauthorizedAccessException,
                    $"Unexpected exception type: {exception.GetType().FullName}");
            }

            // Исходный файл не должен измениться.
            Assert.Equal(
                originalContents,
                File.ReadAllBytes(path));

            // Временный файл должен быть удалён после ошибки.
            Assert.Empty(
                Directory.EnumerateFiles(
                    root,
                    "probe.vbe.*.tmp"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Load_RejectsFileExceedingPayloadLimitBeforeDeserialization()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.vbe");

        try
        {
            // Максимум 4 байта payload + заголовок контейнера.
            File.WriteAllBytes(path, new byte[17]);

            var context = new SerializationContext(
                maxPayloadBytes: 4);

            var exception = Assert.Throws<InvalidDataException>(
                () => BinaryFileSerializer.Load(
                    path,
                    new TickSerializer(),
                    context));

            Assert.Contains(
                "maximum allowed file size",
                exception.Message);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Save_OverwritesExistingFile()
    {
        var path =
            Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid():N}.vbe");

        try
        {
            var serializer =
                new TickSerializer();

            BinaryFileSerializer.Save(
                path,
                new Tick(100),
                serializer);

            BinaryFileSerializer.Save(
                path,
                new Tick(200),
                serializer);

            var restored =
                BinaryFileSerializer.Load(
                    path,
                    serializer);

            Assert.Equal(
                new Tick(200),
                restored);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void SaveAndLoad_RoundTripsThroughRealFile()
    {
        var path =
            Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid():N}.vbe");

        try
        {
            var serializer =
                new TickSerializer();

            var value =
                new Tick(
                    123456789UL);

            BinaryFileSerializer.Save(
                path,
                value,
                serializer);

            Assert.True(
                File.Exists(path));

            var restored =
                BinaryFileSerializer.Load(
                    path,
                    serializer);

            Assert.Equal(
                value,
                restored);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}