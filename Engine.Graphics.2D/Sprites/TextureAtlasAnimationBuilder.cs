using Engine.Core.Math;
using Engine.Graphics.Resources;

namespace Engine.Graphics2D.Sprites;

public sealed class TextureAtlasAnimationBuilder
{
    public SpriteAnimation Build(
        TextureAtlasBuildResult atlas,
        TextureHandle textureHandle,
        IReadOnlyList<string> frameNames,
        Vector2 size,
        float frameDurationSeconds,
        bool loop = true,
        int layer = 0)
    {
        ArgumentNullException.ThrowIfNull(atlas);
        ArgumentNullException.ThrowIfNull(frameNames);

        if (frameNames.Count == 0)
        {
            throw new ArgumentException(
                "Animation must contain at least one frame.",
                nameof(frameNames));
        }

        if (!float.IsFinite(size.X) ||
            !float.IsFinite(size.Y) ||
            size.X <= 0.0f ||
            size.Y <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                "Sprite dimensions must be finite and positive.");
        }

        if (!float.IsFinite(frameDurationSeconds) ||
            frameDurationSeconds <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(frameDurationSeconds));
        }

        var frames = new Sprite[frameNames.Count];

        for (var i = 0; i < frameNames.Count; i++)
        {
            var frameName = frameNames[i];

            ArgumentException.ThrowIfNullOrWhiteSpace(frameName);

            var region = atlas.GetRegion(frameName);

            frames[i] = new Sprite(
                textureHandle,
                region.UV,
                size,
                layer);
        }

        return new SpriteAnimation(
            frames,
            frameDurationSeconds,
            loop);
    }
}