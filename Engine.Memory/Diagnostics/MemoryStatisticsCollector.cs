namespace Engine.Memory.Diagnostics;

public sealed class MemoryStatisticsCollector
{
    private long _totalRentedBytes;
    private long _totalReturnedBytes;
    private long _activeRentedBytes;
    private long _rentCount;
    private long _returnCount;

    public MemoryStatistics Statistics =>
        new(
            _totalRentedBytes,
            _totalReturnedBytes,
            _activeRentedBytes,
            _rentCount,
            _returnCount);

    public void RecordRent(
        int bytes)
    {
        if (bytes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bytes));
        }

        Interlocked.Add(
            ref _totalRentedBytes,
            bytes);

        Interlocked.Add(
            ref _activeRentedBytes,
            bytes);

        Interlocked.Increment(
            ref _rentCount);
    }

    public void RecordReturn(
        int bytes)
    {
        if (bytes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bytes));
        }

        Interlocked.Add(
            ref _totalReturnedBytes,
            bytes);

        Interlocked.Add(
            ref _activeRentedBytes,
            -bytes);

        Interlocked.Increment(
            ref _returnCount);
    }

    public void Reset()
    {
        Interlocked.Exchange(
            ref _totalRentedBytes,
            0);

        Interlocked.Exchange(
            ref _totalReturnedBytes,
            0);

        Interlocked.Exchange(
            ref _activeRentedBytes,
            0);

        Interlocked.Exchange(
            ref _rentCount,
            0);

        Interlocked.Exchange(
            ref _returnCount,
            0);
    }
}