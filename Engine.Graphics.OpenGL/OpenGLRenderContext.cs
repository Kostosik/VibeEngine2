using Engine.Graphics;
using Engine.Graphics.Commands;

namespace Engine.Graphics.OpenGL;

public sealed class OpenGLRenderContext : IRenderContext
{
    public void Submit(IRenderCommand command)
    {
        switch (command)
        {
            case DrawTextureCommand drawTexture:
                DrawTexture(drawTexture);
                break;

            default:
                throw new NotSupportedException(
                    $"Render command {command.GetType().Name} is not supported.");
        }
    }

    public void Clear()
    {
    }

    private void DrawTexture(DrawTextureCommand command)
    {
        // Реальный OpenGL rendering добавим следующим шагом.
    }
}