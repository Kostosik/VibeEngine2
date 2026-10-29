using System.Text;
using Engine.Core.Assets;

namespace Engine.Serialization.Content;

internal static class StrictUtf8Text
{
    private static readonly UTF8Encoding Utf8 =
        new(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

    public static string Decode(
        AssetPath path,
        string resourceType,
        ReadOnlySpan<byte> data)
    {
        try
        {
            return Utf8.GetString(data);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException(
                $"{resourceType} '{path}' contains invalid UTF-8 data.",
                exception);
        }
    }
}