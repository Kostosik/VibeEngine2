namespace Engine.Navigation.Maps;

public interface INavigationMap
{
    bool Contains(
        NavigationCoordinate coordinate);

    bool IsTraversable(
        NavigationCoordinate coordinate);

    float GetTraversalCost(
        NavigationCoordinate coordinate);
}