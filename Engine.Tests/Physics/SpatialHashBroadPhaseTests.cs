using Engine.Core.Math;
using Engine.ECS.Entities;
using Engine.Physics.BroadPhase;
using Engine.Physics.Components;
using Engine.Physics.Collision;
using Engine.Physics.Shapes;

namespace Engine.Tests.Physics;

public sealed class SpatialHashBroadPhaseTests
{
    [Fact]
    public void FindPairs_FindsOverlappingCollidersInSameCell()
    {
        var first =
            CreateProxy(
                new EntityId(1, 1),
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.Zero));

        var second =
            CreateProxy(
                new EntityId(2, 1),
                new FixedVector2(
                    Fixed32.FromFloat(0.5f),
                    Fixed32.Zero));

        var broadPhase =
            new SpatialHashBroadPhase(
                Fixed32.One);

        var pairs =
            new List<CollisionPair>();

        broadPhase.FindPairs(
            new[]
            {
                first,
                second
            },
            pairs);

        var pair =
            Assert.Single(
                pairs);

        Assert.Equal(
            first.Entity,
            pair.First);

        Assert.Equal(
            second.Entity,
            pair.Second);
    }

    [Fact]
    public void FindPairs_DoesNotReturnSeparatedColliders()
    {
        var first =
            CreateProxy(
                new EntityId(1, 1),
                FixedVector2.Zero);

        var second =
            CreateProxy(
                new EntityId(2, 1),
                new FixedVector2(
                    Fixed32.FromInt(5),
                    Fixed32.Zero));

        var broadPhase =
            new SpatialHashBroadPhase(
                Fixed32.One);

        var pairs =
            new List<CollisionPair>();

        broadPhase.FindPairs(
            new[]
            {
                first,
                second
            },
            pairs);

        Assert.Empty(
            pairs);
    }

    [Fact]
    public void FindPairs_RemovesDuplicatePairsAcrossCells()
    {
        var first =
            CreateProxy(
                new EntityId(1, 1),
                FixedVector2.Zero,
                new FixedVector2(
                    Fixed32.FromInt(4),
                    Fixed32.FromInt(4)));

        var second =
            CreateProxy(
                new EntityId(2, 1),
                new FixedVector2(
                    Fixed32.One,
                    Fixed32.One),
                new FixedVector2(
                    Fixed32.FromInt(4),
                    Fixed32.FromInt(4)));

        var broadPhase =
            new SpatialHashBroadPhase(
                Fixed32.One);

        var pairs =
            new List<CollisionPair>();

        broadPhase.FindPairs(
            new[]
            {
                first,
                second
            },
            pairs);

        Assert.Single(
            pairs);
    }

    [Fact]
    public void FindPairs_RespectsCollisionMask()
    {
        var first =
            CreateProxy(
                new EntityId(1, 1),
                FixedVector2.Zero);

        var secondCollider =
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.One,
                        Fixed32.One)));

        secondCollider.CollisionLayer =
            2u;

        var second =
            new PhysicsColliderProxy(
                new EntityId(2, 1),
                secondCollider.GetWorldBounds(
                    new FixedVector2(
                        Fixed32.FromFloat(0.5f),
                        Fixed32.Zero)),
                secondCollider);

        var firstCollider =
            first.Collider;

        firstCollider.CollisionMask =
            1u;

        first =
            new PhysicsColliderProxy(
                first.Entity,
                first.Bounds,
                firstCollider);

        var broadPhase =
            new SpatialHashBroadPhase(
                Fixed32.One);

        var pairs =
            new List<CollisionPair>();

        broadPhase.FindPairs(
            new[]
            {
                first,
                second
            },
            pairs);

        Assert.Empty(
            pairs);
    }

    [Fact]
    public void FindPairs_ReturnsDeterministicOrder()
    {
        var first =
            CreateProxy(
                new EntityId(3, 1),
                FixedVector2.Zero);

        var second =
            CreateProxy(
                new EntityId(1, 1),
                new FixedVector2(
                    Fixed32.FromFloat(0.5f),
                    Fixed32.Zero));

        var third =
            CreateProxy(
                new EntityId(2, 1),
                new FixedVector2(
                    Fixed32.FromFloat(0.25f),
                    Fixed32.Zero));

        var broadPhase =
            new SpatialHashBroadPhase(
                Fixed32.One);

        var pairs =
            new List<CollisionPair>();

        broadPhase.FindPairs(
            new[]
            {
                first,
                second,
                third
            },
            pairs);

        Assert.Equal(
            3,
            pairs.Count);

        Assert.Equal(
            1u,
            pairs[0].First.Index);

        Assert.Equal(
            2u,
            pairs[0].Second.Index);

        Assert.Equal(
            1u,
            pairs[1].First.Index);

        Assert.Equal(
            3u,
            pairs[1].Second.Index);

        Assert.Equal(
            2u,
            pairs[2].First.Index);

        Assert.Equal(
            3u,
            pairs[2].Second.Index);
    }

    [Fact]
    public void FindPairs_MatchesBruteForceBroadPhase()
    {
        var colliders =
            new[]
            {
            CreateProxy(
                new EntityId(1, 1),
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.Zero)),

            CreateProxy(
                new EntityId(2, 1),
                new FixedVector2(
                    Fixed32.FromFloat(0.5f),
                    Fixed32.Zero)),

            CreateProxy(
                new EntityId(3, 1),
                new FixedVector2(
                    Fixed32.FromInt(3),
                    Fixed32.Zero)),

            CreateProxy(
                new EntityId(4, 1),
                new FixedVector2(
                    Fixed32.FromFloat(3.5f),
                    Fixed32.Zero)),

            CreateProxy(
                new EntityId(5, 1),
                new FixedVector2(
                    Fixed32.FromInt(10),
                    Fixed32.FromInt(10)),
                new FixedVector2(
                    Fixed32.FromInt(4),
                    Fixed32.FromInt(4))),

            CreateProxy(
                new EntityId(6, 1),
                new FixedVector2(
                    Fixed32.FromInt(11),
                    Fixed32.FromInt(11)),
                new FixedVector2(
                    Fixed32.FromInt(2),
                    Fixed32.FromInt(2)))
            };

        var bruteForce =
            new BruteForceBroadPhase();

        var spatialHash =
            new SpatialHashBroadPhase(
                Fixed32.One);

        var brutePairs =
            new List<CollisionPair>();

        var spatialPairs =
            new List<CollisionPair>();

        bruteForce.FindPairs(
            colliders,
            brutePairs);

        spatialHash.FindPairs(
            colliders,
            spatialPairs);

        var expected =
            brutePairs
                .ToHashSet();

        var actual =
            spatialPairs
                .ToHashSet();

        Assert.Equal(
            expected,
            actual);
    }

    [Fact]
    public void FindPairs_LargeColliderMatchesBruteForce()
    {
        var colliders =
            new[]
            {
            CreateProxy(
                new EntityId(1, 1),
                FixedVector2.Zero,
                new FixedVector2(
                    Fixed32.FromInt(8),
                    Fixed32.FromInt(8))),

            CreateProxy(
                new EntityId(2, 1),
                new FixedVector2(
                    Fixed32.FromInt(7),
                    Fixed32.FromInt(7))),

            CreateProxy(
                new EntityId(3, 1),
                new FixedVector2(
                    Fixed32.FromInt(20),
                    Fixed32.FromInt(20))),

            CreateProxy(
                new EntityId(4, 1),
                new FixedVector2(
                    Fixed32.FromInt(-3),
                    Fixed32.FromInt(-3)))
            };

        var bruteForce =
            new BruteForceBroadPhase();

        var spatialHash =
            new SpatialHashBroadPhase(
                Fixed32.One);

        var brutePairs =
            new List<CollisionPair>();

        var spatialPairs =
            new List<CollisionPair>();

        bruteForce.FindPairs(
            colliders,
            brutePairs);

        spatialHash.FindPairs(
            colliders,
            spatialPairs);

        Assert.Equal(
            brutePairs.ToHashSet(),
            spatialPairs.ToHashSet());
    }

    private static PhysicsColliderProxy CreateProxy(
        EntityId entity,
        FixedVector2 position,
        FixedVector2? size = null)
    {
        var collider =
            new Collider2D(
                new AabbShape2D(
                    size ??
                    new FixedVector2(
                        Fixed32.One,
                        Fixed32.One)));

        return new PhysicsColliderProxy(
            entity,
            collider.GetWorldBounds(
                position),
            collider);
    }
}