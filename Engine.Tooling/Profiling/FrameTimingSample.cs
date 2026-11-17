
namespace Engine.Tooling.Profiling;

public readonly record struct FrameTimingSample(
    TimeSpan FrameInterval,
    TimeSpan Update,
    TimeSpan FixedUpdate,
    TimeSpan Render,
    TimeSpan EndFrame)
{
    public void Validate()
    {
        if (FrameInterval < TimeSpan.Zero ||
            Update < TimeSpan.Zero ||
            FixedUpdate < TimeSpan.Zero ||
            Render < TimeSpan.Zero ||
            EndFrame < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(FrameTimingSample),
                "Frame timings cannot be negative.");
        }
    }
}
