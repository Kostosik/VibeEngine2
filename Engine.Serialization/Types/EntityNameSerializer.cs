using Engine.ECS.Components;
using Engine.Serialization.Binary;

namespace Engine.Serialization.Types;

public sealed class EntityNameSerializer :
    IBinarySerializer<EntityName>
{
    public void Serialize(
        ref SerializationWriter writer,
        EntityName value)
    {
        if (!value.IsValid)
        {
            throw new InvalidDataException(
                "Entity name cannot be empty.");
        }

        writer.WriteString(
            value.Value);
    }

    public EntityName Deserialize(
        ref SerializationReader reader)
    {
        var value =
            reader.ReadString();

        if (value is null)
        {
            throw new InvalidDataException(
                "Serialized entity name cannot be null.");
        }

        try
        {
            return new EntityName(value);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException(
                "Serialized entity name is invalid.",
                exception);
        }
    }
}