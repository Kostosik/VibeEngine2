namespace Engine.Networking.Replication;

public enum ReplicationOperation : byte
{
    Spawn = 1,
    Update = 2,
    Despawn = 3
}