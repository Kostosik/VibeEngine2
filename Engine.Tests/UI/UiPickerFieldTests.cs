using Engine.UI.Controls;

namespace Engine.Tests.UI;

public sealed class UiPickerFieldTests
{
    [Fact]
    public void Constructor_StoresSelectedText()
    {
        var field =
            new UiPickerField("Marine_Diffuse");

        Assert.Equal(
            "Marine_Diffuse",
            field.SelectedText);

        Assert.Equal(
            "Select...",
            field.Placeholder);
    }

    [Fact]
    public void RequestSelection_RaisesEvent()
    {
        var field =
            new UiPickerField("Marine_Diffuse");

        var requestCount = 0;

        field.SelectionRequested +=
            () => requestCount++;

        field.RequestSelection();

        Assert.Equal(1, requestCount);
    }

    [Fact]
    public void RequestSelection_WhenDisabled_DoesNotRaiseEvent()
    {
        var field =
            new UiPickerField("Marine_Diffuse")
            {
                Enabled = false
            };

        var requestCount = 0;

        field.SelectionRequested +=
            () => requestCount++;

        field.RequestSelection();

        Assert.Equal(0, requestCount);
    }

    [Fact]
    public void SelectedText_CanBeUpdatedAndCleared()
    {
        var field =
            new UiPickerField("Marine_Diffuse");

        field.SelectedText = "Explosion";

        Assert.Equal(
            "Explosion",
            field.SelectedText);

        field.SelectedText = null;

        Assert.Null(field.SelectedText);
    }
}