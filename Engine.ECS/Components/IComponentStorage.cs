using Engine.Core.Determinism;
using Engine.ECS.Entities;
using Engine.ECS.Persistence;

namespace Engine.ECS.Components;

internal interface IComponentStorage :
    IDisposable
{
    Type ComponentType { get; }

    bool IsDeterministic { get; }

    ulong ChangeVersion { get; }

    object GetBoxed(
        EntityId entity);

    int Count { get; }

    bool Has(
        EntityId entity);

    bool Remove(
        EntityId entity);

    void RemoveEntity(
        EntityId entity);

    EntityId GetEntity(
        int index);

    WorldComponentState CaptureState();

    void AddToHash(
        ref DeterministicStateHasher hasher);

    IComponentStorageSnapshot CreateSnapshot();

    void RestoreSnapshot(
        IComponentStorageSnapshot snapshot);
}