using System.Runtime.InteropServices;

namespace Engine.Worlds.Tiles;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct Tile(uint Value)
{
    public static Tile Empty =>
        default;
}