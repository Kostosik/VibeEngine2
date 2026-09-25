namespace Engine.Core.Replays;

public sealed class ReplayCommandData
{
    public ReplayCommandData(
        string type,
        string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(payload);

        Type = type;
        Payload = payload;
    }

    public string Type { get; }

    public string Payload { get; }
}