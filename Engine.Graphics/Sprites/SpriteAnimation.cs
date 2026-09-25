namespace Engine.Graphics.Sprites;

public sealed class SpriteAnimation
{
    private readonly Sprite[] _frames;
    private readonly float _frameDuration;
    private readonly bool _loop;

    private int _currentFrame;
    private float _elapsed;

    public SpriteAnimation(
        IReadOnlyList<Sprite> frames,
        float frameDuration,
        bool loop = true)
    {
        ArgumentNullException.ThrowIfNull(
            frames);

        if (frames.Count == 0)
        {
            throw new ArgumentException(
                "Animation must contain at least one frame.",
                nameof(frames));
        }

        if (frameDuration <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(frameDuration));
        }

        _frames =
            frames.ToArray();

        _frameDuration =
            frameDuration;

        _loop =
            loop;
    }

    public Sprite Current =>
        _frames[_currentFrame];

    public int CurrentFrame =>
        _currentFrame;

    public int FrameCount =>
        _frames.Length;

    public bool IsFinished { get; private set; }

    public void Update(
        float deltaSeconds)
    {
        if (deltaSeconds < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(deltaSeconds));
        }

        if (IsFinished)
        {
            return;
        }

        _elapsed += deltaSeconds;

        while (_elapsed >= _frameDuration)
        {
            _elapsed -= _frameDuration;

            if (_currentFrame >=
                _frames.Length - 1)
            {
                if (_loop)
                {
                    _currentFrame = 0;

                    continue;
                }

                _currentFrame =
                    _frames.Length - 1;

                IsFinished = true;

                break;
            }

            _currentFrame++;
        }
    }

    public void Reset()
    {
        _currentFrame = 0;
        _elapsed = 0.0f;
        IsFinished = false;
    }
}