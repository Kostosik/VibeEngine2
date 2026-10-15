using Engine.Core.Time;

namespace Engine.Scripting;

public interface IEntityScript
{
    void OnAttach(ScriptContext context)
    {
    }

    void Update(
        ScriptContext context,
        TimeSnapshot time)
    {
    }

    void FixedUpdate(
        ScriptContext context,
        SimulationTime time)
    {
    }

    void OnDetach(ScriptContext context)
    {
    }
}