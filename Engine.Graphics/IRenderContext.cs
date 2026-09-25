using Engine.Graphics.Commands;

namespace Engine.Graphics;

public interface IRenderContext
{
    void Submit(IRenderCommand command);

    void Clear();
}