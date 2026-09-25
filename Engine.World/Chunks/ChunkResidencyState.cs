namespace Engine.Worlds.Chunks;

public enum ChunkResidencyState : byte
{
    Unloaded = 0,
    Loading = 1,
    Loaded = 2,
    Unloading = 3
}