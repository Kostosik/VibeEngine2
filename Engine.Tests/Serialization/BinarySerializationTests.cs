using Engine.Serialization.Binary;

namespace Engine.Tests.Serialization;

public sealed class BinarySerializationTests
{
    [Fact]
    public void Deserialize_WithInvalidUtf8String_ThrowsInvalidDataException()
    {
        var data = new byte[]
        {
        1, 0, 0, 0, // Длина строки: 1 байт
        0xFF        // Некорректный UTF-8
        };

        var exception = Assert.Throws<InvalidDataException>(
            () => BinarySerializer.Deserialize(
                data,
                new StringSerializer()));

        Assert.Contains("UTF-8", exception.Message);
    }

    private sealed class StringSerializer : IBinarySerializer<string?>
    {
        public void Serialize(
            ref SerializationWriter writer,
            string? value)
        {
            writer.WriteString(value);
        }

        public string? Deserialize(
            ref SerializationReader reader)
        {
            return reader.ReadString();
        }
    }

    [Fact]
    public void Unpack_WithOverflowingDeclaredPayloadLength_ThrowsInvalidDataException()
    {
        var data = new byte[]
        {
        0x56, 0x42, 0x53, 0x31, // VBS1
        0x01, 0x00, 0x00, 0x00, // Version 1
        0xFF, 0xFF, 0xFF, 0x7F  // int.MaxValue
        };

        var context = new SerializationContext(
            maxPayloadBytes: int.MaxValue);

        Assert.Throws<InvalidDataException>(
            () => BinaryContainer.Unpack(data, context).Length);
    }

    [Fact]
    public void SerializationContext_EnforcesPayloadAndValueLimits()
    {
        var context =
            new SerializationContext(
                formatVersion: 7,
                maxStringBytes: 4,
                maxCollectionLength: 2,
                maxPayloadBytes: 16);

        Assert.Equal(
            7,
            context.FormatVersion);

        var writer =
            new SerializationWriter(
                context);

        Assert.Throws<InvalidDataException>(
            () =>
                writer.WriteString(
                    "VibeEngine"));

        Assert.Throws<InvalidDataException>(
            () =>
                writer.WriteBytes(
                    new byte[]
                    {
                    1,
                    2,
                    3
                    }));

        Assert.Throws<InvalidDataException>(
            () =>
                BinarySerializer.Deserialize(
                    new byte[17],
                    new TestDataSerializer(),
                    context));
    }

    [Fact]
    public void SerializeDeserialize_IsDeterministicAndLossless()
    {
        var serializer =
            new TestDataSerializer();

        var value =
            new TestData(
                42,
                123456789UL,
                true,
                12.5f,
                "VibeEngine",
                new byte[]
                {
                    1,
                    2,
                    3,
                    4
                });

        var first =
            BinarySerializer.Serialize(
                value,
                serializer);

        var second =
            BinarySerializer.Serialize(
                value,
                serializer);

        Assert.Equal(
            first,
            second);

        var restored =
            BinarySerializer.Deserialize(
                first,
                serializer);

        Assert.Equal(
            value.IntValue,
            restored.IntValue);

        Assert.Equal(
            value.UInt64Value,
            restored.UInt64Value);

        Assert.Equal(
            value.BooleanValue,
            restored.BooleanValue);

        Assert.Equal(
            value.FloatValue,
            restored.FloatValue);

        Assert.Equal(
            value.StringValue,
            restored.StringValue);

        Assert.Equal(
            value.Bytes,
            restored.Bytes);
    }

    [Fact]
    public void Deserialize_WithTrailingData_Throws()
    {
        var serializer =
            new TestDataSerializer();

        var value =
            new TestData(
                1,
                2,
                false,
                3.5f,
                "test",
                new byte[]
                {
                    10
                });

        var data =
            BinarySerializer.Serialize(
                value,
                serializer);

        var invalidData =
            new byte[data.Length + 1];

        data.CopyTo(
            invalidData,
            0);

        invalidData[^1] =
            255;

        Assert.Throws<InvalidDataException>(
            () =>
                BinarySerializer.Deserialize(
                    invalidData,
                    serializer));
    }

    private readonly record struct TestData(
        int IntValue,
        ulong UInt64Value,
        bool BooleanValue,
        float FloatValue,
        string? StringValue,
        byte[] Bytes);

    private sealed class TestDataSerializer :
        IBinarySerializer<TestData>
    {
        public void Serialize(
            ref SerializationWriter writer,
            TestData value)
        {
            writer.WriteInt32(
                value.IntValue);

            writer.WriteUInt64(
                value.UInt64Value);

            writer.WriteBoolean(
                value.BooleanValue);

            writer.WriteSingle(
                value.FloatValue);

            writer.WriteString(
                value.StringValue);

            writer.WriteBytes(
                value.Bytes);
        }

        public TestData Deserialize(
            ref SerializationReader reader)
        {
            return new TestData(
                reader.ReadInt32(),
                reader.ReadUInt64(),
                reader.ReadBoolean(),
                reader.ReadSingle(),
                reader.ReadString(),
                reader.ReadBytes());
        }
    }
}