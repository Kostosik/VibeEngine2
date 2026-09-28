namespace Engine.Graphics.Resources;

public interface IGraphicsBufferManager
{
    GraphicsBufferHandle Create(
        GraphicsBufferDescription description);

    void Update(
        GraphicsBufferHandle buffer,
        ReadOnlySpan<byte> data,
        int offsetInBytes = 0);

    bool Exists(
        GraphicsBufferHandle buffer);

    GraphicsBufferDescription GetDescription(
        GraphicsBufferHandle buffer);

    void Destroy(
        GraphicsBufferHandle buffer);
}