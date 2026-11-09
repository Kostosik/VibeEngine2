
using Engine.AI.Memory;
using Engine.Core.Time;

namespace Engine.Tests.AI;

public sealed class AIMemoryTests
{
    private static readonly AIMemoryKey<string> TargetKey =
        new("Target");

    private static readonly AIMemoryKey<int> ThreatKey =
        new("Threat");

    [Fact]
    public void SetAndGet_UsesTypedKey()
    {
        var memory = new AIMemory();

        memory.Set(
            TargetKey,
            "Enemy-01",
            TimeSpan.Zero);

        memory.Set(
            ThreatKey,
            75,
            TimeSpan.Zero);

        Assert.True(
            memory.TryGet(
                TargetKey,
                TimeSpan.Zero,
                out var target));

        Assert.Equal("Enemy-01", target);

        Assert.True(
            memory.TryGet(
                ThreatKey,
                TimeSpan.Zero,
                out var threat));

        Assert.Equal(75, threat);
        Assert.Equal(2, memory.Count);
    }

    [Fact]
    public void TryGet_ReturnsFalseAfterExpiration()
    {
        var memory = new AIMemory();

        memory.Set(
            TargetKey,
            "Enemy-01",
            TimeSpan.Zero,
            Duration.FromSeconds(5));

        Assert.True(
            memory.TryGet(
                TargetKey,
                TimeSpan.FromSeconds(4),
                out _));

        Assert.False(
            memory.TryGet(
                TargetKey,
                TimeSpan.FromSeconds(5),
                out _));

        Assert.Equal(0, memory.Count);
    }

    [Fact]
    public void PruneExpired_RemovesOnlyExpiredEntries()
    {
        var memory = new AIMemory();

        memory.Set(
            TargetKey,
            "Enemy-01",
            TimeSpan.Zero,
            Duration.FromSeconds(5));

        memory.Set(
            ThreatKey,
            75,
            TimeSpan.Zero);

        int removed = memory.PruneExpired(
            TimeSpan.FromSeconds(6));

        Assert.Equal(1, removed);
        Assert.Equal(1, memory.Count);

        Assert.False(
            memory.TryGet(
                TargetKey,
                TimeSpan.FromSeconds(6),
                out _));

        Assert.True(
            memory.TryGet(
                ThreatKey,
                TimeSpan.FromSeconds(6),
                out var threat));

        Assert.Equal(75, threat);
    }
}
