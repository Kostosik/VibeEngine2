using Engine.Graphics.Commands;

namespace Engine.Graphics.Rendering;

public interface IRenderPassExecutor
{
    void Begin(
        RenderPassContext context);

    void Execute(
        IRenderCommand command);
    void Execute(DrawWorldTextureData command)
    {
        throw new NotSupportedException(
            "This render pass executor does not support " +
            "world texture data.");
    }
    void End();
}