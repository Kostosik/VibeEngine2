
namespace Engine.Editor.Projects;

/// <summary>
/// Defines how an editor launches a game project.
/// </summary>
public enum GameProjectRunMode
{
    /// <summary>
    /// Starts the project normally using dotnet run.
    /// </summary>
    Standard,

    /// <summary>
    /// Starts the project using dotnet watch for Hot Reload.
    /// </summary>
    HotReload
}
