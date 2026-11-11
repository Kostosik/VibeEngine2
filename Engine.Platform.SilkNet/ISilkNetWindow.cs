
using Engine.Platform;
using Silk.NET.Windowing;

namespace Engine.Platform.SilkNet;

public interface ISilkNetWindow : IPlatformWindow
{
    IWindow NativeWindow { get; }
}
