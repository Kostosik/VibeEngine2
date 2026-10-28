using Engine.Core.Time;

namespace Engine.Scripting;

/// <summary>
/// Defines behavior that can be attached to an entity and executed by the script runtime.
/// Implement only the callbacks required by the script.
/// All callbacks have default no-op implementations.
/// </summary>
public interface IEntityScript
{
    /// <summary>
    /// Called when the script is attached to an entity.
    /// Use this callback to initialize the script.
    /// </summary>
    /// <param name="context">Provides access to the script's execution context.</param>
    void OnAttach(ScriptContext context)
    {
    }

    /// <summary>
    /// Called during each variable-duration update.
    /// Use this callback for per-frame script logic.
    /// </summary>
    /// <param name="context">Provides access to the script's execution context.</param>
    /// <param name="time">Contains timing information for the current frame.</param>
    void Update(
        ScriptContext context,
        TimeSnapshot time)
    {
    }

    /// <summary>
    /// Called during a fixed-duration simulation update.
    /// Use this callback for logic that requires a consistent simulation timestep.
    /// </summary>
    /// <param name="context">Provides access to the script's execution context.</param>
    /// <param name="time">Contains timing information for the current simulation step.</param>
    void FixedUpdate(
        ScriptContext context,
        SimulationTime time)
    {
    }

    /// <summary>
    /// Called when the script is detached from an entity.
    /// Use this callback to release resources or clean up script state.
    /// </summary>
    /// <param name="context">Provides access to the script's execution context.</param>
    void OnDetach(ScriptContext context)
    {
    }
}