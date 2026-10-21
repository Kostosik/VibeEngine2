using Engine.Core.Math;
using Engine.Editor.Commands;
using Engine.Editor.Documents;
using Engine.Editor.Inspection;
using Engine.Graphics.Commands;
using Engine.Physics.Materials;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;
using System.Globalization;

namespace Engine.Editor.UI.Inspection;

public sealed class PhysicsMaterial2DInspectorPropertyEditor
    : IInspectorPropertyEditor
{
    public int Priority => 300;

    public bool CanEdit(Type propertyType) =>
        propertyType == typeof(PhysicsMaterial2D);

    public UiWidget Create(
        EditorDocument document,
        EditorProperty property)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(property);

        var material =
            property.GetValue() is PhysicsMaterial2D value
                ? value
                : PhysicsMaterial2D.Default;

        var frictionBox =
            CreateValueBox(material.Friction);

        var restitutionBox =
            CreateValueBox(material.Restitution);

        var errorLabel =
            new UiLabel
            {
                FontSize = 11.0f,
                Color = new UiColor(230, 130, 130, 255)
            };

        var applyButton =
            new UiButton("Apply")
            {
                Width = 100.0f,
                Height = 30.0f,
                Enabled = !property.IsReadOnly
            };

        var editor =
            new UiStackPanel
            {
                Orientation = UiOrientation.Vertical,
                Spacing = 4.0f,
                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,
                VerticalAlignment = UiVerticalAlignment.Top
            };

        editor.AddChild(
            new UiLabel(property.DisplayName)
            {
                FontSize = 12.0f
            });

        editor.AddChild(
            CreateRow("Friction", frictionBox));

        editor.AddChild(
            CreateRow("Restitution", restitutionBox));

        editor.AddChild(applyButton);
        editor.AddChild(errorLabel);

        if (!property.IsReadOnly)
        {
            void Apply()
            {
                if (!Fixed32InspectorPropertyEditor.TryParseFixed32(
                        frictionBox.Text,
                        out var friction) ||
                    !Fixed32InspectorPropertyEditor.TryParseFixed32(
                        restitutionBox.Text,
                        out var restitution))
                {
                    errorLabel.Text =
                        "Enter valid finite numbers.";

                    return;
                }

                PhysicsMaterial2D newMaterial;

                try
                {
                    newMaterial =
                        new PhysicsMaterial2D(
                            friction,
                            restitution);
                }
                catch (ArgumentOutOfRangeException)
                {
                    errorLabel.Text =
                        "Friction must be >= 0; restitution must be 0..1.";

                    return;
                }

                document.Execute(
                    new SetEditorPropertyCommand(
                        property,
                        newMaterial));

                errorLabel.Text = string.Empty;
            }

            applyButton.Clicked += Apply;
            frictionBox.Submitted += _ => Apply();
            restitutionBox.Submitted += _ => Apply();
        }
        else
        {
            frictionBox.Enabled = false;
            restitutionBox.Enabled = false;
        }

        return editor;
    }

    private static UiTextBox CreateValueBox(
        Fixed32 value)
    {
        return new UiTextBox(
            value.ToFloat().ToString(
                "0.#####",
                CultureInfo.InvariantCulture))
        {
            Width = 110.0f,
            Height = 30.0f,
            FontSize = 12.0f
        };
    }

    private static UiStackPanel CreateRow(
        string label,
        UiTextBox textBox)
    {
        var row =
            new UiStackPanel
            {
                Orientation = UiOrientation.Horizontal,
                Spacing = 6.0f,
                VerticalAlignment = UiVerticalAlignment.Top
            };

        row.AddChild(
            new UiLabel(label)
            {
                Width = 90.0f,
                FontSize = 12.0f
            });

        row.AddChild(textBox);

        return row;
    }
}