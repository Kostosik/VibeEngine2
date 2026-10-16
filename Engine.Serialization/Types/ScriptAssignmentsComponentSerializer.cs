using Engine.Scripting;
using Engine.Serialization.Binary;
using System.Text.Json;

namespace Engine.Serialization.Types;

public sealed class ScriptAssignmentsComponentSerializer :
    IBinarySerializer<ScriptAssignmentsComponent>
{
    public void Serialize(
        ref SerializationWriter writer,
        ScriptAssignmentsComponent value)
    {
        if (value.Count >
            writer.Context.MaxCollectionLength)
        {
            throw new InvalidDataException(
                "Script assignment count exceeds the maximum allowed length.");
        }

        writer.WriteInt32(value.Count);

        foreach (var assignment in value.Assignments)
        {
            writer.WriteString(
                assignment.ScriptId);

            writer.WriteString(
                assignment.ConfigurationJson);
        }
    }

    public ScriptAssignmentsComponent Deserialize(
        ref SerializationReader reader)
    {
        var count =
            reader.ReadInt32();

        if (count < 0 ||
            count > reader.Context.MaxCollectionLength)
        {
            throw new InvalidDataException(
                $"Invalid script assignment count '{count}'.");
        }

        var assignments =
            new ScriptAssignment[count];

        for (var i = 0; i < count; i++)
        {
            var scriptId =
                reader.ReadString();

            var configurationJson =
                reader.ReadString();

            if (scriptId is null ||
                configurationJson is null)
            {
                throw new InvalidDataException(
                    "Script ID and configuration cannot be null.");
            }

            try
            {
                assignments[i] =
                    new ScriptAssignment(
                        scriptId,
                        configurationJson);
            }
            catch (Exception exception)
                when (exception is ArgumentException or JsonException)
            {
                throw new InvalidDataException(
                    "Serialized script assignment is invalid.",
                    exception);
            }
        }

        try
        {
            return new ScriptAssignmentsComponent(
                assignments);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException(
                "Serialized script assignments are invalid.",
                exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidDataException(
                "Serialized script assignments contain duplicates.",
                exception);
        }
    }
}