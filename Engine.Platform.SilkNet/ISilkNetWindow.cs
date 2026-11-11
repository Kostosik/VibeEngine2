
using Engine.Platform;
using Silk.NET.Core.Contexts;
using Silk.NET.Windowing;

namespace Engine.Platform.SilkNet;

public interface ISilkNetWindow : IPlatformWindow
{
    IGLContext OpenGLContext { get; }
    IWindow NativeWindow { get; }
}
