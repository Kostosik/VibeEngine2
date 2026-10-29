using Engine.Graphics.Commands;

namespace Engine.Graphics;

public interface IWorldTextureDataSubmitter
{
    void Submit(DrawWorldTextureData command);
}