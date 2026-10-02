using Engine.Serialization.Binary;

namespace Engine.Networking.Replication;

public sealed class ReplicationMessageSerializer :
    IBinarySerializer<ReplicationMessage>
{
    public void Serialize(
        ref SerializationWriter writer,
        ReplicationMessage message)
    {
        writer.WriteByte(
            (byte)message.Operation);

        writer.WriteUInt64(
            message.State.Id.Value);

        if (message.Operation ==
            ReplicationOperation.Despawn)
        {
            writer.WriteInt32(
                0);

            return;
        }

        var components =
            message.State.Components;

        if (components.Count >
            writer.Context.MaxCollectionLength)
        {
            throw new InvalidDataException(
                $"Replication component count '{components.Count}' " +
                $"exceeds the maximum allowed length " +
                $"'{writer.Context.MaxCollectionLength}'.");
        }

        writer.WriteInt32(
            components.Count);

        foreach (var component in components)
        {
            writer.WriteString(
                component.Id);

            writer.WriteBytes(
                component.Payload.Span);
        }
    }

    public ReplicationMessage Deserialize(
        ref SerializationReader reader)
    {
        var operationValue =
            reader.ReadByte();

        if (!Enum.IsDefined(
                typeof(ReplicationOperation),
                operationValue))
        {
            throw new InvalidDataException(
                $"Unknown replication operation '{operationValue}'.");
        }

        var operation =
            (ReplicationOperation)operationValue;

        var networkId =
            new NetworkEntityId(
                reader.ReadUInt64());

        if (!networkId.IsValid)
        {
            throw new InvalidDataException(
                "Replication message contains an invalid network entity ID.");
        }

        var componentCount =
            reader.ReadInt32();

        if (componentCount < 0)
        {
            throw new InvalidDataException(
                $"Replication component count '{componentCount}' is invalid.");
        }

        if (componentCount >
            reader.Context.MaxCollectionLength)
        {
            throw new InvalidDataException(
                $"Replication component count '{componentCount}' " +
                $"exceeds the maximum allowed length " +
                $"'{reader.Context.MaxCollectionLength}'.");
        }

        if (operation ==
            ReplicationOperation.Despawn &&
            componentCount != 0)
        {
            throw new InvalidDataException(
                "Despawn replication message cannot contain components.");
        }

        var components =
            new ReplicatedComponentState[
                componentCount];

        for (var i = 0;
             i < componentCount;
             i++)
        {
            var id =
                reader.ReadString();

            if (string.IsNullOrWhiteSpace(id))
            {
                throw new InvalidDataException(
                    "Replication component ID cannot be null or empty.");
            }

            var payload =
                reader.ReadBytes();

            components[i] =
                new ReplicatedComponentState(
                    id,
                    payload);
        }

        var state =
            new ReplicatedEntityState(
                networkId,
                components);

        return operation switch
        {
            ReplicationOperation.Spawn =>
                ReplicationMessage.Spawn(state),

            ReplicationOperation.Update =>
                ReplicationMessage.Update(state),

            ReplicationOperation.Despawn =>
                ReplicationMessage.Despawn(networkId),

            _ =>
                throw new InvalidDataException(
                    $"Unknown replication operation '{operationValue}'.")
        };
    }
}