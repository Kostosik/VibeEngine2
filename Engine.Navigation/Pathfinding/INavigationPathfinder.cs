using Engine.Navigation.Maps;

namespace Engine.Navigation.Pathfinding;

public interface INavigationPathfinder
{
    bool TryFindPath(
        INavigationMap map,
        NavigationCoordinate start,
        NavigationCoordinate goal,
        out NavigationPath? path);
}