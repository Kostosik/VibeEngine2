using Engine.ECS;
using Engine.Networking.Authority;
using Engine.Networking.Replication;
using Engine.Networking.Topology;

namespace Engine.Tests.Networking.Replication;

public sealed class NetworkIdentityMapSynchronizerTests
{
    [Fact]
    public void Reconcile_RegistersExistingIdentity_AndIsIdempotent()
    {
        using var world = new World();

        var entity = world.CreateEntity();
        var identity = NetworkIdentity.Create();

        world.Add(entity, identity);

        var map = new NetworkEntityMap(world);
        using var synchronizer = new NetworkIdentityMapSynchronizer(
            world,
            map,
            CreateAuthority(isAuthority: true));

        Assert.Equal(1, synchronizer.Reconcile());
        Assert.Equal(0, synchronizer.Reconcile());

        Assert.True(map.TryGetNetworkId(entity, out var networkId));
        Assert.Equal(identity.Id, networkId);

        Assert.True(map.TryGetEntity(networkId, out var mappedEntity));
        Assert.Equal(entity, mappedEntity);
    }

    [Fact]
    public void Reconcile_AuthorityAssignsIdToPendingIdentity()
    {
        using var world = new World();

        var entity = world.CreateEntity();
        world.Add(entity, NetworkIdentity.Pending);

        var map = new NetworkEntityMap(world);
        using var synchronizer = new NetworkIdentityMapSynchronizer(
            world,
            map,
            CreateAuthority(isAuthority: true));

        synchronizer.Reconcile();

        var identity = world.Get<NetworkIdentity>(entity);

        Assert.True(identity.HasAssignedId);
        Assert.True(map.TryGetNetworkId(entity, out var networkId));
        Assert.Equal(identity.Id, networkId);
    }

    [Fact]
    public void Reconcile_NonAuthorityDoesNotAssignPendingId()
    {
        using var world = new World();

        var entity = world.CreateEntity();
        world.Add(entity, NetworkIdentity.Pending);

        var map = new NetworkEntityMap(world);
        using var synchronizer = new NetworkIdentityMapSynchronizer(
            world,
            map,
            CreateAuthority(isAuthority: false));

        synchronizer.Reconcile();

        Assert.False(
            world.Get<NetworkIdentity>(entity).HasAssignedId);

        Assert.False(map.TryGetNetworkId(entity, out _));
        Assert.Equal(0, map.Count);
    }

    [Fact]
    public void Reconcile_RemovingIdentityRemovesMapping()
    {
        using var world = new World();

        var entity = world.CreateEntity();
        world.Add(entity, NetworkIdentity.Create());

        var map = new NetworkEntityMap(world);
        using var synchronizer = new NetworkIdentityMapSynchronizer(
            world,
            map,
            CreateAuthority(isAuthority: true));

        synchronizer.Reconcile();
        Assert.Equal(1, map.Count);

        world.Remove<NetworkIdentity>(entity);

        synchronizer.Reconcile();

        Assert.Equal(0, map.Count);
        Assert.False(map.TryGetNetworkId(entity, out _));
    }

    [Fact]
    public void Reconcile_DestroyingEntityImmediatelyRemovesMapping()
    {
        using var world = new World();

        var entity = world.CreateEntity();
        world.Add(entity, NetworkIdentity.Create());

        var map = new NetworkEntityMap(world);
        using var synchronizer = new NetworkIdentityMapSynchronizer(
            world,
            map,
            CreateAuthority(isAuthority: true));

        synchronizer.Reconcile();

        Assert.Equal(1, map.Count);

        world.DestroyEntity(entity);

        Assert.Equal(0, map.Count);
    }

    [Fact]
    public void Reconcile_RejectsDuplicateIdentityIds()
    {
        using var world = new World();

        var first = world.CreateEntity();
        var second = world.CreateEntity();

        var identity = NetworkIdentity.Create();

        world.Add(first, identity);
        world.Add(second, identity);

        var map = new NetworkEntityMap(world);
        using var synchronizer = new NetworkIdentityMapSynchronizer(
            world,
            map,
            CreateAuthority(isAuthority: true));

        Assert.Throws<InvalidOperationException>(
            () => synchronizer.Reconcile());

        Assert.Equal(0, map.Count);
    }

    private static FixedNetworkAuthority CreateAuthority(
        bool isAuthority)
    {
        var authorityNode = new NetworkNodeId(1);
        var localNode = isAuthority
            ? authorityNode
            : new NetworkNodeId(2);

        return new FixedNetworkAuthority(
            localNode,
            authorityNode);
    }
}