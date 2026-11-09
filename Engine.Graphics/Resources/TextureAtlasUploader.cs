namespace Engine.Graphics.Resources;

public sealed class TextureAtlasUploader
{
    public TextureAtlasResource Upload(
        TextureAtlasBuildResult buildResult,
        ITextureManager textureManager)
    {
        ArgumentNullException.ThrowIfNull(buildResult);
        ArgumentNullException.ThrowIfNull(textureManager);

        var texture = textureManager.Create(
            buildResult.Texture);

        if (!texture.IsValid)
        {
            throw new InvalidOperationException(
                "Texture manager returned an invalid texture handle.");
        }

        try
        {
            var atlas = buildResult.CreateAtlas(texture);

            return new TextureAtlasResource(
                textureManager,
                texture,
                atlas);
        }
        catch
        {
            textureManager.Destroy(texture);
            throw;
        }
    }
}