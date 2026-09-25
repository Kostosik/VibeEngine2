namespace Engine.Memory.Diagnostics;

public readonly record struct MemoryStatistics(
    long TotalRentedBytes,
    long TotalReturnedBytes,
    long ActiveRentedBytes,
    long RentCount,
    long ReturnCount)
{
    public static MemoryStatistics Empty =>
        default;
}