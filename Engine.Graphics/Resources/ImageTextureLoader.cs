using Engine.Core.Assets;
using StbiSharp;

namespace Engine.Graphics.Resources;

public sealed class ImageTextureLoader
{
    public TextureData Load(
        IAssetSource source,
        AssetPath path, TextureColorSpace colorSpace = TextureColorSpace.Srgb)
    {
        ArgumentNullException.ThrowIfNull(source);

        var encodedData =
            source.Load(path);

        if (encodedData.IsEmpty)
        {
            throw new InvalidDataException(
                $"Asset '{path}' is empty.");
        }

        using var image =
            Stbi.LoadFromMemory(
                encodedData.Span,
                4);

        var pixels =
            image.Data.ToArray();

        return new TextureData(
            new TextureDescription(
                image.Width,
                image.Height,
                TextureFormat.Rgba8)
            {
                ColorSpace = colorSpace
            },
            pixels);
    }

    public TextureData Load(
    ReadOnlyMemory<byte> encodedData, TextureColorSpace colorSpace = TextureColorSpace.Srgb)
    {
        if (encodedData.IsEmpty)
        {
            throw new InvalidDataException(
                "Texture asset is empty.");
        }

        using var image =
            Stbi.LoadFromMemory(
                encodedData.Span,
                4);

        var pixels =
            image.Data.ToArray();

        return new TextureData(
            new TextureDescription(
                image.Width,
                image.Height,
                TextureFormat.Rgba8)
            {
                ColorSpace = colorSpace
            },
            pixels);
    }
}