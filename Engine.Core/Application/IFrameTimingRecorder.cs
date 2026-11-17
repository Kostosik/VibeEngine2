
namespace Engine.Core.Application;

public interface IFrameTimingRecorder
{
    bool IsCapturing { get; }

    void BeginUpdate();

    void RecordUpdate(TimeSpan elapsed);

    void RecordFixedUpdate(TimeSpan elapsed);

    void BeginFrame();

    void RecordRender(TimeSpan elapsed);

    void RecordEndFrame(TimeSpan elapsed);

    void EndFrame();
}
