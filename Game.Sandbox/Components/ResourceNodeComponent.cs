namespace Game.Sandbox.Components;

public struct ResourceNodeComponent
{
    public ResourceNodeComponent(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Resource amount must be positive.");
        }

        Amount = amount;
    }

    public int Amount { get; }
}