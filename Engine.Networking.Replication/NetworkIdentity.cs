using System.Security.Cryptography;

namespace Engine.Networking.Replication;

public struct NetworkIdentity
{
    public NetworkIdentity(NetworkEntityId id)
    {
        if (!id.IsValid)
        {
            throw new ArgumentException(
                "Network entity ID must be valid.",
                nameof(id));
        }

        Id = id;
    }

    public NetworkEntityId Id { get; }

    /// <summary>
    /// Creates a network identity with a new persistent ID.
    /// </summary>
    public static NetworkIdentity Create()
    {
        Span<byte> bytes = stackalloc byte[sizeof(ulong)];

        ulong value;

        do
        {
            RandomNumberGenerator.Fill(bytes);
            value = BitConverter.ToUInt64(bytes);
        }
        while (value == 0);

        return new NetworkIdentity(
            new NetworkEntityId(value));
    }
}