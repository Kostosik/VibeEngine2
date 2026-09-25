namespace Engine.Worlds.Spatial;

public struct WorldPositionComponent
{
    public WorldPositionComponent(
        WorldPosition position)
    {
        Position = position;
    }

    public WorldPosition Position;
}