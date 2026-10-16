using Engine.Core.Assets;
using System.Text;

namespace Engine.Core.Resources;

public sealed class TextResourceLoader
    : IResourceLoader<string>
{
    public string Load(
        AssetPath path,
        ReadOnlyMemory<byte> data)
    {
        return Encoding.UTF8.GetString(
            data.Span);
    }
}