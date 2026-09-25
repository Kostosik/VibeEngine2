namespace Engine.Networking.Topology;

public readonly record struct NetworkTopologyEdge
{
    public NetworkTopologyEdge(
        NetworkNodeId first,
        NetworkNodeId second,
        NetworkNodeId initiator)
    {
        if (!first.IsValid)
        {
            throw new ArgumentException(
                "First node ID must be valid.",
                nameof(first));
        }

        if (!second.IsValid)
        {
            throw new ArgumentException(
                "Second node ID must be valid.",
                nameof(second));
        }

        if (first == second)
        {
            throw new ArgumentException(
                "Topology edge cannot connect a node to itself.");
        }

        if (initiator != first &&
            initiator != second)
        {
            throw new ArgumentException(
                "Initiator must be one of the edge nodes.",
                nameof(initiator));
        }

        First = first;
        Second = second;
        Initiator = initiator;
    }

    public NetworkNodeId First { get; }

    public NetworkNodeId Second { get; }

    public NetworkNodeId Initiator { get; }
}