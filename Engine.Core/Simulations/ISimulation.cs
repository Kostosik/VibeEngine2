using Engine.Core.Commands;
using Engine.Core.Systems;

namespace Engine.Core.Simulations;

public interface ISimulation
{
    void Initialize();

    void Update(
        SystemContext context);

    void FixedUpdate(
        FixedSystemContext context);

    void Submit(
        ICommand command);

    void Shutdown();
}