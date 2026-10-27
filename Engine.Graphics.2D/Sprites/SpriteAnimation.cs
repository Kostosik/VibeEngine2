using Engine.Animation;
using Engine.Core.Assets;
using Engine.Graphics.Resources;
using Engine.Core.Math;

namespace Engine.Graphics2D.Sprites;

public sealed class SpriteAnimation
{
    private static readonly AnimationTrackId FrameTrack = new("frame");

    private readonly Sprite[] _frames;
    private readonly AnimationClip _clip;
    private readonly AnimationPlayer _player;

    private int _currentFrame;

    public SpriteAnimation(
        IReadOnlyList<Sprite> frames,
        float frameDuration,
        bool loop = true)
    {
        ArgumentNullException.ThrowIfNull(frames);

        if (frames.Count == 0)
            throw new ArgumentException(
                "Animation must contain at least one frame.",
                nameof(frames));

        if (!float.IsFinite(frameDuration) || frameDuration <= 0.0f)
            throw new ArgumentOutOfRangeException(nameof(frameDuration));

        _frames = frames.ToArray();

        _clip = new AnimationClip(
            frames.Count * (double)frameDuration);

        var keyframes = new AnimationKeyframe<int>[frames.Count];

        for (var i = 0; i < frames.Count; i++)
        {
            keyframes[i] = new AnimationKeyframe<int>(
                i * (double)frameDuration,
                i);
        }

        _clip.AddTrack(
            FrameTrack,
            new AnimationTrack<int>(
                keyframes,
                new StepAnimationInterpolator<int>()));

        _player = new AnimationPlayer(_clip)
        {
            LoopMode = loop
                ? AnimationLoopMode.Loop
                : AnimationLoopMode.Once
        };

        _currentFrame = 0;

        _player.Play();
    }

    public SpriteAnimation(
    SpriteAnimationDefinition definition,
    ITextureResourceManager resources,
    float width = 1.0f,
    float height = 1.0f,
    int layer = 0)
    : this(
        CreateSprites(
            definition,
            resources,
            width,
            height,
            layer),
        definition.FrameDurationSeconds,
        definition.Loop)
    {
    }

    private static IReadOnlyList<Sprite> CreateSprites(
        SpriteAnimationDefinition definition,
        ITextureResourceManager resources,
        float width,
        float height,
        int layer)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(resources);

        if (!float.IsFinite(width) ||
            !float.IsFinite(height) ||
            width <= 0.0f ||
            height <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                "Sprite dimensions must be finite and positive.");
        }

        if (definition.Frames.Count == 0)
        {
            throw new InvalidOperationException(
                "Sprite animation must contain at least one frame.");
        }

        var sprites = new Sprite[definition.Frames.Count];

        for (var i = 0; i < sprites.Length; i++)
        {
            var assetPath = new AssetPath(
                definition.Frames[i].TexturePath);

            var texture = resources.Load(assetPath);

            if (!resources.TryGetDescription(
                    assetPath,
                    out _))
            {
                throw new InvalidOperationException(
                    $"Could not read texture description for '{assetPath}'.");
            }

            sprites[i] = new Sprite(
                texture,
                new Rectangle(0.0f, 0.0f, 1.0f, 1.0f),
                new Vector2(width, height),
                layer);
        }

        return sprites;
    }

    public Sprite Current => _frames[_currentFrame];

    public int CurrentFrame => _currentFrame;

    public int FrameCount => _frames.Length;

    public bool IsFinished => _player.IsFinished;

    public void Update(float deltaSeconds)
    {
        if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0.0f)
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

        _player.Update(deltaSeconds);

        _currentFrame = _clip.Sample<int>(
            FrameTrack,
            _player.TimeSeconds);
    }

    public void Reset()
    {
        _player.Reset();
        _currentFrame = 0;
        _player.Play();
    }
}