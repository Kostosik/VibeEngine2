
using Engine.Core.Math;
using Engine.Editor.Commands;
using Engine.Editor.Documents;
using Engine.Editor.Inspection;
using Engine.Graphics.Commands;
using Engine.Physics.Shapes;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;
using System.Globalization;

namespace Engine.Editor.UI.Inspection;

internal sealed class PhysicsShape2DInspectorPropertyEditor
    : IInspectorPropertyEditor
{
    public int Priority => 300;

    public bool CanEdit(Type propertyType) =>
        propertyType == typeof(PhysicsShape2D);

    public UiWidget Create(
        EditorDocument document,
        EditorProperty property)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(property);

        var shape =
            property.GetValue() is PhysicsShape2D value
                ? value
                : PhysicsShape2D.FromAabb(
                    new AabbShape2D(FixedVector2.One));

        var initialSize = GetInitialSize(shape);

        var sizeXBox = CreateBox(initialSize.X);
        var sizeYBox = CreateBox(initialSize.Y);

        var radiusBox =
            CreateBox(
                shape.Type == PhysicsShapeType.Circle
                    ? shape.Circle.Radius
                    : Fixed32.One);

        var verticesBox =
            new UiTextBox(GetInitialVertices(shape))
            {
                Width = 300.0f,
                Height = 40.0f,
                FontSize = 12.0f,
                MaxLength = 2048,
                Enabled = !property.IsReadOnly
            };

        var selector =
            new UiDropdown();

        selector.AddOption("AABB");
        selector.AddOption("Circle");
        selector.AddOption("Polygon");

        selector.SetSelectedIndex((int)shape.Type);
        selector.Enabled = !property.IsReadOnly;

        var sizePanel =
            new UiStackPanel
            {
                Orientation = UiOrientation.Vertical,
                Spacing = 4.0f,
                Visible = shape.Type == PhysicsShapeType.Aabb
            };

        sizePanel.AddChild(
            CreateRow("Size X", sizeXBox));

        sizePanel.AddChild(
            CreateRow("Size Y", sizeYBox));

        var circlePanel =
            new UiStackPanel
            {
                Orientation = UiOrientation.Vertical,
                Spacing = 4.0f,
                Visible = shape.Type == PhysicsShapeType.Circle
            };

        circlePanel.AddChild(
            CreateRow("Radius", radiusBox));

        var polygonPanel =
            new UiStackPanel
            {
                Orientation = UiOrientation.Vertical,
                Spacing = 4.0f,
                Visible = shape.Type == PhysicsShapeType.Polygon
            };

        polygonPanel.AddChild(
            new UiLabel(
                "Vertices: x,y; x,y; x,y (counter-clockwise)")
            {
                FontSize = 11.0f
            });

        polygonPanel.AddChild(verticesBox);

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
                Spacing = 5.0f,
                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,
                VerticalAlignment = UiVerticalAlignment.Top
            };

        editor.AddChild(
            new UiLabel(property.DisplayName)
            {
                FontSize = 12.0f
            });

        editor.AddChild(selector);
        editor.AddChild(sizePanel);
        editor.AddChild(circlePanel);
        editor.AddChild(polygonPanel);
        editor.AddChild(applyButton);
        editor.AddChild(errorLabel);

        void UpdateShapePanels(string selectedText)
        {
            sizePanel.Visible = selectedText == "AABB";
            circlePanel.Visible = selectedText == "Circle";
            polygonPanel.Visible = selectedText == "Polygon";
        }

        selector.SelectionChanged +=
            (_, selectedText) =>
                UpdateShapePanels(selectedText);

        if (!property.IsReadOnly)
        {
            void Apply()
            {
                try
                {
                    PhysicsShape2D newShape;

                    switch (selector.SelectedText)
                    {
                        case "AABB":
                            if (!TryReadFixed32(
                                    sizeXBox.Text,
                                    out var sizeX) ||
                                !TryReadFixed32(
                                    sizeYBox.Text,
                                    out var sizeY))
                            {
                                errorLabel.Text =
                                    "Enter valid finite dimensions.";
                                return;
                            }

                            newShape =
                                PhysicsShape2D.FromAabb(
                                    new AabbShape2D(
                                        new FixedVector2(
                                            sizeX,
                                            sizeY)));
                            break;

                        case "Circle":
                            if (!TryReadFixed32(
                                    radiusBox.Text,
                                    out var radius))
                            {
                                errorLabel.Text =
                                    "Enter a valid finite radius.";
                                return;
                            }

                            newShape =
                                PhysicsShape2D.FromCircle(
                                    new CircleShape2D(radius));
                            break;

                        case "Polygon":
                            if (!TryReadVertices(
                                    verticesBox.Text,
                                    out var vertices))
                            {
                                errorLabel.Text =
                                    "Enter at least three valid vertices.";
                                return;
                            }

                            newShape =
                                PhysicsShape2D.FromPolygon(
                                    new PolygonShape2D(vertices));
                            break;

                        default:
                            errorLabel.Text =
                                "Select a supported shape.";
                            return;
                    }

                    document.Execute(
                        new SetEditorPropertyCommand(
                            property,
                            newShape));

                    errorLabel.Text = string.Empty;
                }
                catch (ArgumentException exception)
                {
                    errorLabel.Text = exception.Message;
                }
                catch (InvalidOperationException exception)
                {
                    errorLabel.Text = exception.Message;
                }
            }

            applyButton.Clicked += Apply;
            sizeXBox.Submitted += _ => Apply();
            sizeYBox.Submitted += _ => Apply();
            radiusBox.Submitted += _ => Apply();
            verticesBox.Submitted += _ => Apply();
        }

        return editor;
    }

    private static UiTextBox CreateBox(Fixed32 value)
    {
        return new UiTextBox(Format(value))
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

    private static FixedVector2 GetInitialSize(
        PhysicsShape2D shape)
    {
        var size =
            shape.Type switch
            {
                PhysicsShapeType.Aabb =>
                    shape.Aabb.Size,

                PhysicsShapeType.Circle =>
                    new FixedVector2(
                        shape.Circle.Diameter,
                        shape.Circle.Diameter),

                PhysicsShapeType.Polygon
                    when shape.Polygon.VertexCount >= 3 =>
                    shape.Polygon.GetBounds(
                        FixedVector2.Zero).Size,

                _ => FixedVector2.One
            };

        return size.X > Fixed32.Zero &&
               size.Y > Fixed32.Zero
            ? size
            : FixedVector2.One;
    }

    private static string GetInitialVertices(
        PhysicsShape2D shape)
    {
        if (shape.Type == PhysicsShapeType.Polygon &&
            shape.Polygon.VertexCount >= 3)
        {
            return string.Join(
                "; ",
                Enumerable.Range(
                    0,
                    shape.Polygon.VertexCount)
                .Select(index =>
                {
                    var vertex =
                        shape.Polygon.GetVertex(index);

                    return
                        $"{Format(vertex.X)},{Format(vertex.Y)}";
                }));
        }

        return "-0.5,-0.5; 0.5,-0.5; 0,0.5";
    }

    private static bool TryReadFixed32(
        string text,
        out Fixed32 value)
    {
        return Fixed32InspectorPropertyEditor.TryParseFixed32(
            text,
            out value);
    }

    private static bool TryReadVertices(
        string text,
        out List<FixedVector2> vertices)
    {
        vertices = new List<FixedVector2>();

        var points =
            text.Split(';', StringSplitOptions.TrimEntries);

        if (points.Length < 3)
        {
            return false;
        }

        foreach (var point in points)
        {
            var coordinates =
                point.Split(
                    ',',
                    StringSplitOptions.TrimEntries);

            if (coordinates.Length != 2 ||
                !TryReadFixed32(
                    coordinates[0],
                    out var x) ||
                !TryReadFixed32(
                    coordinates[1],
                    out var y))
            {
                vertices.Clear();
                return false;
            }

            vertices.Add(
                new FixedVector2(x, y));
        }

        return true;
    }

    private static string Format(Fixed32 value)
    {
        return value.ToFloat().ToString(
            "0.#####",
            CultureInfo.InvariantCulture);
    }
}
