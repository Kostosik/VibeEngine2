namespace Engine.Core.Assets;

public interface IAssetSource
{
    ReadOnlyMemory<byte> Load(
        AssetPath path);
}