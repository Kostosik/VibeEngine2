namespace Engine.AI.Scheduling;

public enum AIUpdateGroup : byte
{
    EveryUpdate = 0,
    Critical = 1,
    High = 2,
    Normal = 3,
    Low = 4,
    Background = 5
}