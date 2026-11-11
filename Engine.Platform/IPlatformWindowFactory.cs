namespace Engine.Platform;

public interface IPlatformWindowFactory<out TWindow>
    where TWindow : IPlatformWindow
{
    TWindow Create(PlatformWindowOptions options);
}