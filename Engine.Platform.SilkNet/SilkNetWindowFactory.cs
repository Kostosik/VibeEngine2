using Engine.Platform;

namespace Engine.Platform.SilkNet;

public sealed class SilkNetWindowFactory
    : IPlatformWindowFactory<ISilkNetWindow>
{
    public ISilkNetWindow Create(
        PlatformWindowOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new SilkNetWindow(options);
    }
}