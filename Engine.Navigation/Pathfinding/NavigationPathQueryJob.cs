using Engine.Jobs.Jobs;

namespace Engine.Navigation.Pathfinding;

public sealed class NavigationPathQueryJob :
    IJob
{
    private readonly NavigationQueryService _service;
    private readonly NavigationPathQuery _query;

    public NavigationPathQueryJob(
        NavigationQueryService service,
        NavigationPathQuery query)
    {
        ArgumentNullException.ThrowIfNull(
            service);

        _service =
            service;

        _query =
            query;
    }

    public NavigationQueryResult Result { get; private set; } =
    NavigationQueryResult.Failure(
        NavigationQueryStatus.NotExecuted);

    public void Execute()
    {
        Result =
            _service.FindPath(
                _query);
    }
}