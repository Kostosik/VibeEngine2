using Engine.UI.Core;
using Engine.UI.Input;

namespace Engine.UI.Screens;

public abstract class UiScreen
{
    protected UiScreen(
        UiWidget root)
    {
        ArgumentNullException.ThrowIfNull(root);

        Root = root;
    }

    public UiWidget Root { get; }

    public virtual void OnEnter(
        UiFocusManager focus)
    {
    }

    public virtual void OnExit()
    {
    }

    public virtual void Update(
        double deltaSeconds)
    {
    }
}