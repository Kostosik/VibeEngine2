using Engine.Graphics.Materials;
using Engine.Graphics2D.Sprites;
using Engine.Serialization.Binary;

namespace Engine.Serialization.Types;

public sealed class SpriteMaterialComponentSerializer
    : IBinarySerializer<SpriteMaterialComponent>
{
    public void Serialize(
        ref SerializationWriter writer,
        SpriteMaterialComponent value)
    {
        writer.WriteString(value.NormalTexturePath ?? string.Empty);
        writer.WriteString(value.EmissionTexturePath ?? string.Empty);

        writer.WriteSingle(value.Parameters.NormalStrength);
        writer.WriteSingle(value.Parameters.EmissionStrength);
    }

    public SpriteMaterialComponent Deserialize(
        ref SerializationReader reader)
    {
        var normalPath =
            reader.ReadString() ?? string.Empty;

        var emissionPath =
            reader.ReadString() ?? string.Empty;

        var normalStrength =
            reader.ReadSingle();

        var emissionStrength =
            reader.ReadSingle();

        return new SpriteMaterialComponent(
            normalPath,
            emissionPath,
            new SpriteMaterialParameters(
                normalStrength,
                emissionStrength));
    }
}