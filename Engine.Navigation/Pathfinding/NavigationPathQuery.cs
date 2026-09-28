namespace Engine.Navigation.Pathfinding;

public readonly record struct NavigationPathQuery(
    NavigationCoordinate Start,
    NavigationCoordinate Goal);