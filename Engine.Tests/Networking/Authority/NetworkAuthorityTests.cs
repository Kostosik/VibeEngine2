using Engine.Networking.Authority;
using Engine.Networking.Topology;

namespace Engine.Tests.Networking.Authority;

public sealed class NetworkAuthorityTests
{
    [Fact]
    public void FixedAuthority_LocalAuthority_IsAuthority()
    {
        var localNode =
            new NetworkNodeId(1);

        var authority =
            new FixedNetworkAuthority(
                localNode,
                localNode);

        Assert.Equal(
            localNode,
            authority.LocalNode);

        Assert.Equal(
            localNode,
            authority.AuthorityNode);

        Assert.True(
            authority.HasAuthority);
    }

    [Fact]
    public void FixedAuthority_RemoteAuthority_IsNotAuthority()
    {
        var localNode =
            new NetworkNodeId(1);

        var authorityNode =
            new NetworkNodeId(2);

        var authority =
            new FixedNetworkAuthority(
                localNode,
                authorityNode);

        Assert.Equal(
            authorityNode,
            authority.AuthorityNode);

        Assert.False(
            authority.HasAuthority);
    }
}