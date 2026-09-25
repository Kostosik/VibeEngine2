namespace Engine.Core.Application;

public class StatefulApplication : Application
{
    private readonly ApplicationStateMachine _states;

    public StatefulApplication(
        IApplicationState initialState)
    {
        ArgumentNullException.ThrowIfNull(
            initialState);

        _states =
            new ApplicationStateMachine();

        InitialState =
            initialState;
    }

    protected IApplicationState InitialState { get; }

    protected ApplicationStateMachine States =>
        _states;

    protected IApplicationState? CurrentState =>
        _states.CurrentState;

    protected void ChangeState(
        IApplicationState state)
    {
        _states.ChangeState(state);
    }

    public override void Initialize()
    {
        _states.Initialize(
            InitialState);
    }

    public override void Update(
        Engine.Core.Time.TimeSnapshot time)
    {
        _states.Update(
            time);
    }

    public override void FixedUpdate(
        Engine.Core.Time.SimulationTime time)
    {
        _states.FixedUpdate(
            time);
    }

    public override void Render(
        double interpolationAlpha)
    {
        _states.Render(
            interpolationAlpha);
    }

    public override void Shutdown()
    {
        _states.Shutdown();
    }
}