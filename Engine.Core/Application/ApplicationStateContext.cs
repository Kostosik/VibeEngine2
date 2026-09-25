namespace Engine.Core.Application;

public sealed class ApplicationStateContext
{
    private readonly Action<IApplicationState> _requestTransition;

    internal ApplicationStateContext(
        Action<IApplicationState> requestTransition)
    {
        ArgumentNullException.ThrowIfNull(requestTransition);

        _requestTransition = requestTransition;
    }

    public void ChangeState(
        IApplicationState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        _requestTransition(state);
    }
}