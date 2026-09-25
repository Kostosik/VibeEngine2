namespace Engine.Tooling.Profiling;

public interface IProfiler
{
    IDisposable BeginScope(
        string name);
}