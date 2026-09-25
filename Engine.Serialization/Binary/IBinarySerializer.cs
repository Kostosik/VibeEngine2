namespace Engine.Serialization.Binary;

public interface IBinarySerializer<T>
{
    void Serialize(
        ref SerializationWriter writer,
        T value);

    T Deserialize(
        ref SerializationReader reader);
}