using Engine.Core.Systems;

namespace Engine.Tests.Systems;

internal abstract class TestSystemBase
{
    protected readonly List<string> Execution;
    private readonly string _name;

    protected TestSystemBase(
        string name,
        List<string> execution)
    {
        _name = name;
        Execution = execution;
    }

    protected void Record()
    {
        Execution.Add(_name);
    }
}

internal sealed class BuildingSystem : TestSystemBase, IFixedUpdateSystem
{
    public BuildingSystem(
        List<string> execution)
        : base("Building", execution)
    {
    }

    public void FixedUpdate(
        FixedSystemContext context)
    {
        Record();
    }
}

internal sealed class ProductionSystem : TestSystemBase, IFixedUpdateSystem
{
    public ProductionSystem(
        List<string> execution)
        : base("Production", execution)
    {
    }

    public void FixedUpdate(
        FixedSystemContext context)
    {
        Record();
    }
}

internal sealed class LogisticsSystem : TestSystemBase, IFixedUpdateSystem
{
    public LogisticsSystem(
        List<string> execution)
        : base("Logistics", execution)
    {
    }

    public void FixedUpdate(
        FixedSystemContext context)
    {
        Record();
    }
}

internal sealed class StatisticsSystem : TestSystemBase, IFixedUpdateSystem
{
    public StatisticsSystem(
        List<string> execution)
        : base("Statistics", execution)
    {
    }

    public void FixedUpdate(
        FixedSystemContext context)
    {
        Record();
    }
}

internal sealed class InputSystem : TestSystemBase, IUpdateSystem
{
    public InputSystem(
        List<string> execution)
        : base("Input", execution)
    {
    }

    public void Update(
        SystemContext context)
    {
        Record();
    }
}