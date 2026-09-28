using Engine.Core.Assets;

namespace Engine.Content.Loading;

public interface IContentLoadContext
{
    ReadOnlyMemory<byte> ReadBytes(
        AssetPath path);
}