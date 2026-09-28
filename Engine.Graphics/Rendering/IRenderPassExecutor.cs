using Engine.Graphics.Commands;

namespace Engine.Graphics.Rendering;

public interface IRenderPassExecutor
{
    void Begin(
        RenderPassContext context);

    void Execute(
        IRenderCommand command);

    void End();
}