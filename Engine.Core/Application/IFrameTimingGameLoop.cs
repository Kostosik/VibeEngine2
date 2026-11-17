
namespace Engine.Core.Application;

public interface IFrameTimingGameLoop
{
    IFrameTimingRecorder? FrameTimingRecorder { get; set; }
}
