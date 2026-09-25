namespace Engine.Core.Determinism;
public struct DeterministicRandom
{
    private ulong _state;
    public DeterministicRandom(ulong seed) 
    { 
        _state = seed == 0 ? 0x9E3779B97F4A7C15UL : seed; 
    }
    public uint NextUInt() 
    { 
        var state = _state; 
        state ^= state >> 12; 
        state ^= state << 25; 
        state ^= state >> 27; 
        _state = state; 
        return (uint)((state * 0x2545F4914F6CDD1DUL) >> 32); 
    }
    public int NextInt(int minInclusive, int maxExclusive) 
    { 
        if (minInclusive >= maxExclusive) 
        { 
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Maximum value must be greater than minimum value."); 
        } 
        var range = (uint)(maxExclusive - minInclusive); 

        return minInclusive + (int)(NextUInt() % range); 
    }
    public bool NextBool() 
    { 
        return (NextUInt() & 1) != 0; 
    }
    public float NextFloat() 
    { 
        return NextUInt() / (float)uint.MaxValue; 
    }
    public float NextFloat(float minInclusive, float maxInclusive) 
    {
        if (minInclusive > maxInclusive) 
        { 
            throw new ArgumentOutOfRangeException(nameof(maxInclusive), "Maximum value must be greater than or equal to minimum value."); 
        } 
        return minInclusive + (maxInclusive - minInclusive) * NextFloat(); 
    }
}