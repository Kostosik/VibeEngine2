
using Engine.Editor.Assets;
using Engine.Editor.Documents;
using Engine.Editor.UI.Dialogs;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.Particles;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;
using System.Globalization;

namespace Engine.Editor.UI.Workspace;

public sealed class ParticleEffectEditorPanelView : UiPanel
{
    private readonly ParticleEffectDocument _document;
    private readonly ParticleEffectPreviewView _previewView;

    private readonly UiDropdown _modifierTypeDropdown = new();

    private readonly UiStackPanel _modifierRowsPanel = new()
    {
        Orientation = UiOrientation.Vertical,
        Spacing = 4.0f,
        HorizontalAlignment = UiHorizontalAlignment.Stretch
    };

    private readonly List<ModifierEditorRow> _modifierRows = new();

    public ParticleEffectDocument Document => _document;
    private readonly Dictionary<string, UiTextBox> _fields = new();
    private readonly Dictionary<string, UiWidget> _fieldRows = new();
    private readonly UiDropdown _shapeDropdown = new();
    private readonly EditorAssetPickerDialog? _assetPickerDialog;
    private UiPickerField? _texturePathPicker;
    private readonly UiLabel _messageLabel;

    public ParticleEffectEditorPanelView(
        ParticleEffectDocument document,
        EditorAssetPickerDialog? assetPickerDialog = null)
        : this(document, null, null, assetPickerDialog)
    {
    }

    public ParticleEffectEditorPanelView(
        ParticleEffectDocument document,
        EditorContext? editor,
        ITextureManager? textureManager,
        EditorAssetPickerDialog? assetPickerDialog = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        _document = document;
        _assetPickerDialog = assetPickerDialog;

        Padding = new UiThickness(12.0f);
        Background = new UiColor(28, 28, 28, 255);

        var layout = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 8.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        layout.AddChild(new UiLabel("Particle Effect")
        {
            Height = 30.0f,
            FontSize = 20.0f
        });

        layout.AddChild(new UiLabel(document.DisplayName)
        {
            Height = 24.0f,
            FontSize = 13.0f,
            Color = new UiColor(170, 170, 170, 255)
        });

        _previewView = new ParticleEffectPreviewView(
    document,
    editor,
    textureManager);
        _previewView.Width = 400.0f;
        _previewView.Height = 250.0f;

        var scroll = new UiScrollView
        {
            Width = 820.0f,
            Height = 250.0f,
            HorizontalScrolling = false,
            VerticalScrolling = true,
            ShowVerticalScrollBar = true,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        var fieldsPanel = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 6.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch
        };

        var emitterFieldsPanel = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 6.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch
        };

        var spawnFieldsPanel = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 6.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch
        };

        var velocityFieldsPanel = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 6.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch
        };

        var modifierFieldsPanel = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 6.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch
        };

        var emitterExpander = new UiExpander("Emitter")
        {
            IsExpanded = true,
            HeaderHeight = 32.0f,
            ContentSpacing = 4.0f,
            ContentPadding = new UiThickness(8.0f)
        };
        emitterExpander.SetContent(emitterFieldsPanel);

        var spawnExpander = new UiExpander("Lifetime & Size")
        {
            IsExpanded = true,
            HeaderHeight = 32.0f,
            ContentSpacing = 4.0f,
            ContentPadding = new UiThickness(8.0f)
        };
        spawnExpander.SetContent(spawnFieldsPanel);

        var velocityExpander = new UiExpander("Initial Velocity")
        {
            IsExpanded = false,
            HeaderHeight = 32.0f,
            ContentSpacing = 4.0f,
            ContentPadding = new UiThickness(8.0f)
        };
        velocityExpander.SetContent(velocityFieldsPanel);

        var modifiersExpander = new UiExpander("Modifiers")
        {
            IsExpanded = false,
            HeaderHeight = 32.0f,
            ContentSpacing = 4.0f,
            ContentPadding = new UiThickness(8.0f)
        };
        modifiersExpander.SetContent(modifierFieldsPanel);

        fieldsPanel.AddChild(emitterExpander);
        fieldsPanel.AddChild(spawnExpander);
        fieldsPanel.AddChild(velocityExpander);
        fieldsPanel.AddChild(modifiersExpander);

        scroll.SetContent(fieldsPanel);

        var contentRow = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 12.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        contentRow.AddChild(scroll);
        contentRow.AddChild(_previewView);

        layout.AddChild(contentRow);

        _messageLabel = new UiLabel
        {
            Height = 30.0f,
            FontSize = 12.0f,
            Color = new UiColor(220, 180, 100, 255)
        };

        layout.AddChild(_messageLabel);

        var buttons = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 8.0f,
            Height = 34.0f
        };

        var applyButton = new UiButton("Apply Changes")
        {
            Width = 140.0f,
            Height = 32.0f
        };

        applyButton.Clicked += ApplyChanges;

        var resetButton = new UiButton("Reset Fields")
        {
            Width = 120.0f,
            Height = 32.0f
        };

        resetButton.Clicked += ResetFields;



        buttons.AddChild(applyButton);
        buttons.AddChild(resetButton);
        layout.AddChild(buttons);

        AddChild(layout);

        // Emitter
        AddField(
            emitterFieldsPanel,
            "texturePath",
            "Texture path");

        AddField(
            emitterFieldsPanel,
            "emissionRate",
            "Emission rate / second");

        AddField(
            emitterFieldsPanel,
            "maxParticles",
            "Maximum particles");

        AddShapeDropdown(emitterFieldsPanel);

        AddField(
            emitterFieldsPanel,
            "circleRadius",
            "Circle radius");

        AddField(
            emitterFieldsPanel,
            "rectangleWidth",
            "Rectangle width");

        AddField(
            emitterFieldsPanel,
            "rectangleHeight",
            "Rectangle height");

        AddField(
            emitterFieldsPanel,
            "layer",
            "Render layer");

        // Lifetime & Size
        AddField(
            spawnFieldsPanel,
            "lifetimeMin",
            "Lifetime minimum (seconds)");

        AddField(
            spawnFieldsPanel,
            "lifetimeMax",
            "Lifetime maximum (seconds)");

        AddField(
            spawnFieldsPanel,
            "sizeMin",
            "Size minimum");

        AddField(
            spawnFieldsPanel,
            "sizeMax",
            "Size maximum");

        // Initial Velocity
        AddField(
            velocityFieldsPanel,
            "velocityMinX",
            "Velocity minimum X");

        AddField(
            velocityFieldsPanel,
            "velocityMinY",
            "Velocity minimum Y");

        AddField(
            velocityFieldsPanel,
            "velocityMaxX",
            "Velocity maximum X");

        AddField(
            velocityFieldsPanel,
            "velocityMaxY",
            "Velocity maximum Y");

        // Modifiers
        AddModifierEditor(modifierFieldsPanel);

        _document.Changed += OnDocumentChanged;

        RefreshFields();
        _document.Changed += OnDocumentChanged;

        RefreshFields();
    }

    public void ReleaseResources()
    {
        _document.Changed -= OnDocumentChanged;
        _previewView.ReleaseResources();
    }

    private void AddField(
        UiStackPanel panel,
        string key,
        string label)
    {
        var row = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 8.0f,
            Height = 32.0f
        };

        row.AddChild(new UiLabel(label)
        {
            Width = 220.0f,
            Height = 30.0f,
            FontSize = 13.0f,
            VerticalAlignment = UiVerticalAlignment.Center
        });

        if (key == "texturePath")
        {
            var pickerField = new UiPickerField
            {
                Width = 280.0f,
                Height = 30.0f,
                FontSize = 13.0f,
                Placeholder = label
            };

            pickerField.SelectionRequested += () =>
            {
                if (_assetPickerDialog is null)
                {
                    return;
                }

                _assetPickerDialog.Show(
                    "Select texture",
                    IsTextureAsset,
                    path =>
                    {
                        pickerField.SelectedText = path;

                        _messageLabel.Text =
                            "Texture selected. Apply Changes to use it.";
                    });
            };

            _texturePathPicker = pickerField;
            row.AddChild(pickerField);
        }
        else
        {
            var textBox = new UiTextBox
            {
                Width = 280.0f,
                Height = 30.0f,
                FontSize = 13.0f,
                MaxLength = 256,
                Placeholder = label
            };

            _fields.Add(key, textBox);
            row.AddChild(textBox);
        }

        panel.AddChild(row);
        _fieldRows.Add(key, row);
    }

    private static bool IsTextureAsset(EditorAssetEntry entry)
    {
        if (entry.IsDirectory)
        {
            return false;
        }

        var extension = Path.GetExtension(entry.Name);

        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".tga", StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshFields()
    {
        var definition = _document.Definition;

        var shapeIndex = definition.Emitter.Shape switch
        {
            PointParticleEmissionShapeDefinition => 0,
            CircleParticleEmissionShapeDefinition => 1,
            RectangleParticleEmissionShapeDefinition => 2,

            var shape => throw new NotSupportedException(
                $"Unsupported particle emission shape: " +
                $"'{shape.GetType().Name}'.")
        };

        _shapeDropdown.SetSelectedIndex(shapeIndex);

        var circleRadius =
            definition.Emitter.Shape is CircleParticleEmissionShapeDefinition circle
                ? circle.Radius
                : 1.0f;

        var rectangleSize =
            definition.Emitter.Shape is RectangleParticleEmissionShapeDefinition rectangle
                ? rectangle.Size
                : new Engine.Core.Math.Vector2(1.0f, 1.0f);

        SetField("circleRadius", Format(circleRadius));
        SetField("rectangleWidth", Format(rectangleSize.X));
        SetField("rectangleHeight", Format(rectangleSize.Y));

        SetField("texturePath", definition.TexturePath);
        SetField("emissionRate", Format(definition.EmissionRatePerSecond));
        SetField("maxParticles", Format(definition.MaxParticles));
        SetField("layer", Format(definition.Layer));

        SetField("lifetimeMin", Format(definition.LifetimeSeconds.Min));
        SetField("lifetimeMax", Format(definition.LifetimeSeconds.Max));

        SetField("sizeMin", Format(definition.Size.Min));
        SetField("sizeMax", Format(definition.Size.Max));

        SetField("velocityMinX", Format(definition.Velocity.Min.X));
        SetField("velocityMinY", Format(definition.Velocity.Min.Y));
        SetField("velocityMaxX", Format(definition.Velocity.Max.X));
        SetField("velocityMaxY", Format(definition.Velocity.Max.Y));
        RefreshModifierRows(definition.Particle.Modifiers);
    }

    private void SetField(string key, string value)
    {
        if (key == "texturePath")
        {
            if (_texturePathPicker is not null)
            {
                _texturePathPicker.SelectedText = value;
            }

            return;
        }

        _fields[key].SetText(value);
    }

    private void ApplyChanges()
    {
        try
        {
            var current = _document.Definition;

            var shape = _shapeDropdown.SelectedText switch
            {
                "Point" =>
                    (ParticleEmissionShapeDefinition)
                    new PointParticleEmissionShapeDefinition(),

                "Circle" =>
                    new CircleParticleEmissionShapeDefinition(
                        ReadFloat("circleRadius")),

                "Rectangle" =>
                    new RectangleParticleEmissionShapeDefinition(
                        new Engine.Core.Math.Vector2(
                            ReadFloat("rectangleWidth"),
                            ReadFloat("rectangleHeight"))),

                _ => throw new InvalidDataException(
                    "Select a particle emission shape.")
            };

            var updated = new ParticleEffectDefinition(
                current.FormatVersion,
                ReadString("texturePath"),
                ReadInt("layer"),
                new ParticleEmitterDefinition(
                    ReadFloat("emissionRate"),
                    ReadInt("maxParticles"),
                    shape),
                    new ParticleSpawnDefinition(
                        new ParticleFloatRange(
                            ReadFloat("lifetimeMin"),
                            ReadFloat("lifetimeMax")),
                        new ParticleFloatRange(
                            ReadFloat("sizeMin"),
                            ReadFloat("sizeMax")),
                        new ParticleVector2Range(
                            new Engine.Core.Math.Vector2(
                                ReadFloat("velocityMinX"),
                                ReadFloat("velocityMinY")),
                            new Engine.Core.Math.Vector2(
                                ReadFloat("velocityMaxX"),
                                ReadFloat("velocityMaxY"))),
                        ReadModifierDefinitions()));

            _document.UpdateDefinition(updated);

            _messageLabel.Text = _document.IsDirty
                ? "Changes applied. Save the document to write the file."
                : "No changes to apply.";
        }
        catch (Exception exception)
        {
            _messageLabel.Text = exception.Message;
        }
    }

    private void ResetFields()
    {
        RefreshFields();
        _messageLabel.Text = "Fields restored from the current document.";
    }

    private string ReadString(string key)
    {
        var value = key == "texturePath"
            ? _texturePathPicker?.SelectedText?.Trim() ?? string.Empty
            : _fields[key].Text.Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            var fieldName = key == "texturePath"
                ? "Texture path"
                : _fields[key].Placeholder;

            throw new InvalidDataException(
                $"'{fieldName}' cannot be empty.");
        }

        return value;
    }

    private void AddShapeDropdown(UiStackPanel panel)
    {
        var row = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 8.0f,
            Height = 32.0f
        };

        row.AddChild(new UiLabel("Emission shape")
        {
            Width = 220.0f,
            Height = 30.0f,
            FontSize = 13.0f,
            VerticalAlignment = UiVerticalAlignment.Center
        });

        _shapeDropdown.Width = 280.0f;
        _shapeDropdown.Height = 30.0f;

        _shapeDropdown.AddOption("Point");
        _shapeDropdown.AddOption("Circle");
        _shapeDropdown.AddOption("Rectangle");

        _shapeDropdown.SelectionChanged += (_, selectedText) =>
        {
            UpdateShapeParameterFields(selectedText);
        };

        row.AddChild(_shapeDropdown);
        panel.AddChild(row);
    }
    private void UpdateShapeParameterFields(string shape)
    {
        SetShapeFieldVisible(
            "circleRadius",
            shape == "Circle");

        SetShapeFieldVisible(
            "rectangleWidth",
            shape == "Rectangle");

        SetShapeFieldVisible(
            "rectangleHeight",
            shape == "Rectangle");
    }

    private void AddModifierEditor(UiStackPanel panel)
    {
        var addRow = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 8.0f,
            Height = 32.0f
        };

        _modifierTypeDropdown.Width = 220.0f;
        _modifierTypeDropdown.Height = 30.0f;

        _modifierTypeDropdown.AddOption("Acceleration");
        _modifierTypeDropdown.AddOption("Drag");
        _modifierTypeDropdown.AddOption("Size Over Lifetime");
        _modifierTypeDropdown.SetSelectedIndex(0);

        var addButton = new UiButton("Add Modifier")
        {
            Width = 120.0f,
            Height = 30.0f
        };

        addButton.Clicked += AddSelectedModifier;

        addRow.AddChild(_modifierTypeDropdown);
        addRow.AddChild(addButton);

        panel.AddChild(addRow);
        panel.AddChild(_modifierRowsPanel);
    }

    private void AddSelectedModifier()
    {
        switch (_modifierTypeDropdown.SelectedText)
        {
            case "Acceleration":
                AddModifierRow(
                    "Acceleration",
                    new[] { "X", "Y" },
                    new[] { "Acceleration X", "Acceleration Y" },
                    0.0f,
                    0.0f);
                break;

            case "Drag":
                AddModifierRow(
                    "Drag",
                    new[] { "Coefficient" },
                    new[] { "Drag coefficient" },
                    1.0f);
                break;

            case "Size Over Lifetime":
                AddModifierRow(
                    "Size Over Lifetime",
                    new[] { "Start", "End" },
                    new[] { "Start multiplier", "End multiplier" },
                    1.0f,
                    0.0f);
                break;

            default:
                _messageLabel.Text = "Select a modifier type.";
                return;
        }

        _messageLabel.Text =
            "Modifier added. Apply Changes to commit it.";
    }

    private void AddModifierRow(
    string kind,
    string[] parameterLabels,
    string[] fieldNames,
    params float[] values)
    {
        var row = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 5.0f,
            Height = 32.0f
        };

        row.AddChild(new UiLabel(kind)
        {
            Width = 140.0f,
            Height = 30.0f,
            FontSize = 12.0f,
            VerticalAlignment = UiVerticalAlignment.Center
        });

        var fields = new List<UiTextBox>();

        for (var i = 0; i < values.Length; i++)
        {
            row.AddChild(new UiLabel(parameterLabels[i])
            {
                Width = 45.0f,
                Height = 30.0f,
                FontSize = 12.0f,
                VerticalAlignment = UiVerticalAlignment.Center
            });

            var field = new UiTextBox
            {
                Width = 75.0f,
                Height = 30.0f,
                FontSize = 12.0f,
                MaxLength = 64,
                Placeholder = fieldNames[i]
            };

            field.SetText(Format(values[i]));

            fields.Add(field);
            row.AddChild(field);
        }

        var editorRow = new ModifierEditorRow(
            kind,
            row,
            fields.ToArray());

        var removeButton = new UiButton("Remove")
        {
            Width = 70.0f,
            Height = 30.0f,
            FontSize = 12.0f
        };

        removeButton.Clicked += () => RemoveModifierRow(editorRow);

        row.AddChild(removeButton);

        _modifierRows.Add(editorRow);
        _modifierRowsPanel.AddChild(row);
    }

    private void RemoveModifierRow(ModifierEditorRow editorRow)
    {
        _modifierRows.Remove(editorRow);
        _modifierRowsPanel.RemoveChild(editorRow.Container);

        _messageLabel.Text =
            "Modifier removed. Apply Changes to commit it.";
    }

    private void RefreshModifierRows(
        IReadOnlyList<ParticleModifierDefinition> definitions)
    {
        foreach (var editorRow in _modifierRows.ToArray())
        {
            _modifierRowsPanel.RemoveChild(editorRow.Container);
        }

        _modifierRows.Clear();

        foreach (var definition in definitions)
        {
            switch (definition)
            {
                case AccelerationModifierDefinition acceleration:
                    AddModifierRow(
                        "Acceleration",
                        new[] { "X", "Y" },
                        new[] { "Acceleration X", "Acceleration Y" },
                        acceleration.Acceleration.X,
                        acceleration.Acceleration.Y);
                    break;

                case DragModifierDefinition drag:
                    AddModifierRow(
                        "Drag",
                        new[] { "Coefficient" },
                        new[] { "Drag coefficient" },
                        drag.Coefficient);
                    break;

                case SizeOverLifetimeModifierDefinition size:
                    AddModifierRow(
                        "Size Over Lifetime",
                        new[] { "Start", "End" },
                        new[] { "Start multiplier", "End multiplier" },
                        size.StartMultiplier,
                        size.EndMultiplier);
                    break;

                default:
                    throw new NotSupportedException(
                        $"Unsupported particle modifier: " +
                        $"'{definition.GetType().Name}'.");
            }
        }
    }

    private ParticleModifierDefinition[] ReadModifierDefinitions()
    {
        var definitions =
            new ParticleModifierDefinition[_modifierRows.Count];

        for (var i = 0; i < _modifierRows.Count; i++)
        {
            var row = _modifierRows[i];

            definitions[i] = row.Kind switch
            {
                "Acceleration" =>
                    new AccelerationModifierDefinition(
                        new Engine.Core.Math.Vector2(
                            ReadModifierFloat(row.Fields[0]),
                            ReadModifierFloat(row.Fields[1]))),

                "Drag" =>
                    new DragModifierDefinition(
                        ReadModifierFloat(row.Fields[0])),

                "Size Over Lifetime" =>
                    new SizeOverLifetimeModifierDefinition(
                        ReadModifierFloat(row.Fields[0]),
                        ReadModifierFloat(row.Fields[1])),

                _ => throw new NotSupportedException(
                    $"Unsupported particle modifier: '{row.Kind}'.")
            };
        }

        return definitions;
    }

    private static float ReadModifierFloat(UiTextBox field)
    {
        if (!float.TryParse(
                field.Text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var value) ||
            !float.IsFinite(value))
        {
            throw new InvalidDataException(
                $"'{field.Placeholder}' must be a finite number.");
        }

        return value;
    }

    private sealed class ModifierEditorRow
    {
        public ModifierEditorRow(
            string kind,
            UiStackPanel container,
            UiTextBox[] fields)
        {
            Kind = kind;
            Container = container;
            Fields = fields;
        }

        public string Kind { get; }

        public UiStackPanel Container { get; }

        public UiTextBox[] Fields { get; }
    }

    private void SetShapeFieldVisible(
        string key,
        bool visible)
    {
        if (!_fieldRows.TryGetValue(key, out var row))
        {
            return;
        }

        row.Visible = visible;

        // UiStackPanel учитывает размер даже невидимых детей.
        // Поэтому скрываемую строку нужно также схлопнуть.
        row.Height = visible ? 32.0f : 0.0f;
    }
    private float ReadFloat(string key)
    {
        if (!float.TryParse(
                _fields[key].Text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var value) ||
            !float.IsFinite(value))
        {
            throw new InvalidDataException(
                $"'{_fields[key].Placeholder}' must be a finite number.");
        }

        return value;
    }

    private int ReadInt(string key)
    {
        if (!int.TryParse(
                _fields[key].Text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var value))
        {
            throw new InvalidDataException(
                $"'{_fields[key].Placeholder}' must be an integer.");
        }

        return value;
    }

    private void OnDocumentChanged()
    {
        RefreshFields();
    }

    private static string Format(float value) =>
        value.ToString("G", CultureInfo.InvariantCulture);

    private static string Format(int value) =>
        value.ToString(CultureInfo.InvariantCulture);
}
