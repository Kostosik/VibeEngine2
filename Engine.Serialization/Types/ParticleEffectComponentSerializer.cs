using Engine.Particles;
using Engine.Serialization.Binary;

namespace Engine.Serialization.Types;

public sealed class ParticleEffectComponentSerializer
    : IBinarySerializer<ParticleEffectComponent>
{
    public void Serialize(
        ref SerializationWriter writer,
        ParticleEffectComponent value)
    {
        if (value.InitialBurstCount < 0)
        {
            throw new InvalidDataException(
                "Initial particle burst count cannot be negative.");
        }

        writer.WriteString(
            value.EffectPath ?? string.Empty);

        writer.WriteInt32(
            value.InitialBurstCount);
    }

    public ParticleEffectComponent Deserialize(
        ref SerializationReader reader)
    {
        var effectPath =
            reader.ReadString() ?? string.Empty;

        var initialBurstCount =
            reader.ReadInt32();

        if (initialBurstCount < 0)
        {
            throw new InvalidDataException(
                "Serialized initial particle burst count cannot be negative.");
        }

        return new ParticleEffectComponent
        {
            EffectPath = effectPath,
            InitialBurstCount = initialBurstCount
        };
    }
}