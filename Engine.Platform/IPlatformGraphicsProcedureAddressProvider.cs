namespace Engine.Platform;

public interface IPlatformGraphicsProcedureAddressProvider
{
    nint GetProcAddress(string name);
}