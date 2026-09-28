using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Assets;

namespace Engine.Graphics.Resources;

public sealed class ImageTextureContentLoader :
    IContentLoader<TextureData>
{
    private readonly ImageTextureLoader _loader;

    public ImageTextureContentLoader()
    {
        _loader =
            new ImageTextureLoader();
    }

    public TextureData Load(
        AssetPath path,
        IContentLoadContext context)
    {
        ArgumentNullException.ThrowIfNull(
            context);

        return _loader.Load(
            context.ReadBytes(path));
    }

    public bool CanLoad(
    ContentAsset asset)
    {
        return asset.Extension.Equals(
                   ".png",
                   StringComparison.OrdinalIgnoreCase) ||
               asset.Extension.Equals(
                   ".jpg",
                   StringComparison.OrdinalIgnoreCase) ||
               asset.Extension.Equals(
                   ".jpeg",
                   StringComparison.OrdinalIgnoreCase) ||
               asset.Extension.Equals(
                   ".bmp",
                   StringComparison.OrdinalIgnoreCase) ||
               asset.Extension.Equals(
                   ".tga",
                   StringComparison.OrdinalIgnoreCase);
    }
}