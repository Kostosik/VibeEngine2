namespace Engine.Platform;

public interface IPlatformGraphicsProcedureAddressProvider
    : IPlatformGraphicsContext
{
    nint GetProcAddress(string name);
}