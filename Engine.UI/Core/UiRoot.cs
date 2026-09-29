using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.UI.Core;

public sealed class UiRoot : UiCanvas
{
    internal UiFocusManager? FocusManager { get; set; }
}