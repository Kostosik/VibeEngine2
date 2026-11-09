namespace Engine.AI.Memory;

public static class AIMemoryKeys
{
    public static readonly AIMemoryKey<AILastKnownTargetMemory>
        LastKnownTargets = new("LastKnownTargets");
}