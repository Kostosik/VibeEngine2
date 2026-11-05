using Engine.Core.Time;

namespace Engine.Runtime.Scenes;

/// <summary>
/// Represents a runtime scene managed by SceneManager.
/// </summary>
public interface IScene<TContext> : IDisposable
    where TContext : class
{
    /// <summary>
    /// Initializes the scene using the supplied context.
    /// </summary>
    void Initialize(
        TContext context,
        Action<IScene<TContext>> requestScene);

    /// <summary>
    /// Updates the scene.
    /// </summary>
    void Update(TimeSnapshot time);

    /// <summary>
    /// Renders the scene.
    /// </summary>
    void Render(double interpolationAlpha);
}