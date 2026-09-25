namespace Engine.Core.Diagnostics;

public sealed class FatalEngineException :
    Exception
{
    public FatalEngineException(
        string message)
        : base(message)
    {
    }

    public FatalEngineException(
        string message,
        Exception innerException)
        : base(
            message,
            innerException)
    {
    }
}