using Engine.Core.Determinism;
namespace Engine.Tests.Core.Determinism; 
public sealed class DeterministicRandomTests 
{ [Fact] 
    public void SameSeed_ProducesSameSequence() 
    { 
        var first = new DeterministicRandom(12345);
        var second = new DeterministicRandom(12345);
        for (var i = 0; i < 100; i++) 
        {
            Assert.Equal(first.NextUInt(), second.NextUInt()); 
        } 
    } 
    [Fact] 
    public void DifferentSeeds_ProduceDifferentSequence() 
    { 
        var first = new DeterministicRandom(1);
        var second = new DeterministicRandom(2);
        Assert.NotEqual(first.NextUInt(), second.NextUInt());
    } 
    [Fact] 
    public void NextInt_ReturnsValueInsideRange()
    { var random = new DeterministicRandom(42); 
        for (var i = 0; i < 1000; i++) 
        { var value = random.NextInt(10, 20); 
            Assert.InRange(value, 10, 19); 
        } 
    } 
    [Fact] 
    public void NextBool_ReturnsBothValues()
    { 
        var random = new DeterministicRandom(42); 
        var hasTrue = false; var hasFalse = false;
        for (var i = 0; i < 100; i++) 
        { 
            if (random.NextBool()) hasTrue = true; 
            else hasFalse = true; 
        } 
        Assert.True(hasTrue); 
        Assert.True(hasFalse); 
    }
    [Fact] 
    public void NextFloat_ReturnsValueInsideRange() 
    {
        var random = new DeterministicRandom(42);
        for (var i = 0; i < 1000; i++) 
        {
            var value = random.NextFloat(10f, 20f);
            Assert.InRange(value, 10f, 20f); 
        } 
    } 
}