using Engine.Core.Assets;

namespace Engine.Core.Resources;

public interface IResourceLoader<TResource>
{
    TResource Load(
        AssetPath path,
        ReadOnlyMemory<byte> data);
}