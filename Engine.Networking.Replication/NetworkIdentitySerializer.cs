using Engine.Serialization.Binary;
using Engine.Serialization.SaveLoad.Ecs;

namespace Engine.Networking.Replication;

public sealed class NetworkIdentitySerializer
    : IBinarySerializer<NetworkIdentity>
{
    public void Serialize(
        ref SerializationWriter writer,
        NetworkIdentity value)
    {
        if (!value.Id.IsValid)
        {
            throw new InvalidDataException(
                "Cannot serialize an invalid network identity.");
        }

        writer.WriteUInt64(value.Id.Value);
    }

    public NetworkIdentity Deserialize(
        ref SerializationReader reader)
    {
        var value = reader.ReadUInt64();

        if (value == 0)
        {
            throw new InvalidDataException(
                "Serialized network identity ID must be valid.");
        }

        return new NetworkIdentity(
            new NetworkEntityId(value));
    }
}

public static class NetworkComponentSerializerRegistryExtensions
{
    public static void RegisterNetworkComponents(
        this EcsComponentSerializerRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register(
            "engine.network.identity",
            new NetworkIdentitySerializer());
    }
}