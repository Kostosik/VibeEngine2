using Engine.Serialization.Binary;
using Game.Sandbox.Components;

namespace Game.Sandbox.Serialization;

public sealed class ResourceNodeComponentSerializer
    : IBinarySerializer<ResourceNodeComponent>
{
    public void Serialize(
        ref SerializationWriter writer,
        ResourceNodeComponent value)
    {
        if (value.Amount <= 0)
        {
            throw new InvalidDataException(
                "Resource node amount must be positive.");
        }

        writer.WriteInt32(value.Amount);
    }

    public ResourceNodeComponent Deserialize(
        ref SerializationReader reader)
    {
        var amount = reader.ReadInt32();

        if (amount <= 0)
        {
            throw new InvalidDataException(
                "Serialized resource node amount must be positive.");
        }

        return new ResourceNodeComponent(amount);
    }
}