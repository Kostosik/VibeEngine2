using Engine.Serialization.Binary;
using Game.Sandbox.Components;

namespace Game.Sandbox.Serialization;

public sealed class ResourceInventoryComponentSerializer
    : IBinarySerializer<ResourceInventoryComponent>
{
    public void Serialize(
        ref SerializationWriter writer,
        ResourceInventoryComponent value)
    {
        if (value.Amount < 0)
        {
            throw new InvalidDataException(
                "Resource inventory amount cannot be negative.");
        }

        writer.WriteInt32(value.Amount);
    }

    public ResourceInventoryComponent Deserialize(
        ref SerializationReader reader)
    {
        var amount = reader.ReadInt32();

        if (amount < 0)
        {
            throw new InvalidDataException(
                "Serialized resource inventory amount cannot be negative.");
        }

        return new ResourceInventoryComponent
        {
            Amount = amount
        };
    }
}