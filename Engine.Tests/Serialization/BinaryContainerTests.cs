using Engine.Serialization.Binary;
using System.Buffers.Binary;

namespace Engine.Tests.Serialization;

public sealed class BinaryContainerTests
{
    [Fact]
    public void PackAndUnpack_PreservesPayloadAndVersion()
    {
        var context =
            new SerializationContext(
                formatVersion: 3,
                maxPayloadBytes: 64);

        var payload =
            new byte[]
            {
                10,
                20,
                30,
                40
            };

        var container =
            BinaryContainer.Pack(
                payload,
                context);

        var restored =
            BinaryContainer.Unpack(
                container,
                context);

        Assert.Equal(
            payload,
            restored.ToArray());
    }

    [Fact]
    public void Unpack_WithWrongVersion_Throws()
    {
        var writeContext =
            new SerializationContext(
                formatVersion: 1);

        var readContext =
            new SerializationContext(
                formatVersion: 2);

        var container =
            BinaryContainer.Pack(
                new byte[]
                {
                    1,
                    2,
                    3
                },
                writeContext);

        Assert.Throws<InvalidDataException>(
            () =>
                BinaryContainer.Unpack(
                    container,
                    readContext));
    }

    [Fact]
    public void Unpack_WithInvalidPayloadLength_Throws()
    {
        var context =
            SerializationContext.Default;

        var container =
            BinaryContainer.Pack(
                new byte[]
                {
                    1,
                    2,
                    3
                },
                context);

        BinaryPrimitives.WriteInt32LittleEndian(
            container.AsSpan(8),
            100);

        Assert.Throws<InvalidDataException>(
            () =>
                BinaryContainer.Unpack(
                    container,
                    context));
    }
}