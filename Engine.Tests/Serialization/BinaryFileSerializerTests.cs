using Engine.Core.Time;
using Engine.Serialization.Binary;
using Engine.Serialization.Types;

namespace Engine.Tests.Serialization;

public sealed class BinaryFileSerializerTests
{
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