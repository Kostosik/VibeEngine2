using Engine.Serialization.Binary;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Worlds.Chunks;
using Engine.Worlds.Persistence;
using Engine.Worlds.Spatial;
using Engine.Worlds.Tiles;

namespace Engine.Serialization.SaveLoad.Worlds;

public sealed class WorldSaveStateSerializer :
    IBinarySerializer<WorldSaveState>
{
    private readonly EcsWorldStateSerializer _ecsSerializer;

    public WorldSaveStateSerializer(
        EcsWorldStateSerializer ecsSerializer)
    {
        ArgumentNullException.ThrowIfNull(
            ecsSerializer);

        _ecsSerializer =
            ecsSerializer;
    }

    public void Serialize(
        ref SerializationWriter writer,
        WorldSaveState value)
    {
        ArgumentNullException.ThrowIfNull(
            value);

        writer.WriteInt32(
            value.ChunkSize.Width);

        writer.WriteInt32(
            value.ChunkSize.Height);

        _ecsSerializer.Serialize(
            ref writer,
            value.Ecs);

        if (value.Chunks.Count >
            writer.Context.MaxCollectionLength)
        {
            throw new InvalidDataException(
                "World chunk count exceeds the maximum allowed collection length.");
        }

        writer.WriteInt32(
            value.Chunks.Count);

        foreach (var chunk in value.Chunks)
        {
            writer.WriteInt32(
                chunk.Position.X);

            writer.WriteInt32(
                chunk.Position.Y);

            writer.WriteInt32(
                (int)chunk.Residency);

            writer.WriteInt32(
                (int)chunk.Simulation);

            writer.WriteInt32(
                (int)chunk.Presentation);

            if (chunk.Tiles.Length >
                writer.Context.MaxCollectionLength)
            {
                throw new InvalidDataException(
                    $"Chunk '{chunk.Position}' contains too many tiles.");
            }

            writer.WriteInt32(
                chunk.Tiles.Length);

            foreach (var tile in chunk.Tiles)
            {
                writer.WriteUInt32(
                    tile.Value);
            }
        }
    }

    public WorldSaveState Deserialize(
        ref SerializationReader reader)
    {
        var width =
            reader.ReadInt32();

        var height =
            reader.ReadInt32();

        var chunkSize =
            new ChunkSize(
                width,
                height);

        var ecs =
            _ecsSerializer.Deserialize(
                ref reader);

        var chunkCount =
            ReadCount(
                ref reader);

        var chunks =
            new List<ChunkSaveState>(
                chunkCount);

        for (var i = 0;
             i < chunkCount;
             i++)
        {
            var position =
                new ChunkPosition(
                    reader.ReadInt32(),
                    reader.ReadInt32());

            var residency =
                ReadResidencyState(
                    ref reader);

            var simulation =
                ReadSimulationState(
                    ref reader);

            var presentation =
                ReadPresentationState(
                    ref reader);

            var tileCount =
                ReadCount(
                    ref reader);

            var tiles =
                new Tile[tileCount];

            for (var tileIndex = 0;
                 tileIndex < tileCount;
                 tileIndex++)
            {
                tiles[tileIndex] =
                    new Tile(
                        reader.ReadUInt32());
            }

            chunks.Add(
                new ChunkSaveState(
                    position,
                    residency,
                    simulation,
                    presentation,
                    tiles));
        }

        return new WorldSaveState(
            chunkSize,
            ecs,
            chunks);
    }

    private static int ReadCount(
        ref SerializationReader reader)
    {
        var count =
            reader.ReadInt32();

        if (count < 0)
        {
            throw new InvalidDataException(
                $"Serialized collection length '{count}' is invalid.");
        }

        if (count >
            reader.Context.MaxCollectionLength)
        {
            throw new InvalidDataException(
                $"Serialized collection length '{count}' exceeds " +
                $"the maximum allowed length '{reader.Context.MaxCollectionLength}'.");
        }

        return count;
    }

    private static ChunkResidencyState ReadResidencyState(
        ref SerializationReader reader)
    {
        var value =
            reader.ReadInt32();

        return value switch
        {
            (int)ChunkResidencyState.Loaded =>
                ChunkResidencyState.Loaded,

            (int)ChunkResidencyState.Unloaded =>
                ChunkResidencyState.Unloaded,

            _ => throw new InvalidDataException(
                $"Invalid chunk residency state '{value}'.")
        };
    }

    private static ChunkSimulationState ReadSimulationState(
        ref SerializationReader reader)
    {
        var value =
            reader.ReadInt32();

        return value switch
        {
            (int)ChunkSimulationState.Simulating =>
                ChunkSimulationState.Simulating,

            (int)ChunkSimulationState.Suspended =>
                ChunkSimulationState.Suspended,

            _ => throw new InvalidDataException(
                $"Invalid chunk simulation state '{value}'.")
        };
    }

    private static ChunkPresentationState ReadPresentationState(
        ref SerializationReader reader)
    {
        var value =
            reader.ReadInt32();

        return value switch
        {
            (int)ChunkPresentationState.Relevant =>
                ChunkPresentationState.Relevant,

            (int)ChunkPresentationState.Irrelevant =>
                ChunkPresentationState.Irrelevant,

            _ => throw new InvalidDataException(
                $"Invalid chunk presentation state '{value}'.")
        };
    }
}