
using System.Globalization;
using Engine.Editor.Documents;
using Engine.Graphics.Commands;
using Engine.Particles;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

public sealed class ParticleEffectEditorPanelView : UiPanel
{
    private readonly ParticleEffectDocument _document;
    private readonly Dictionary<string, UiTextBox> _fields = new();
    private readonly UiLabel _messageLabel;

    public ParticleEffectEditorPanelView(
        ParticleEffectDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _document = document;

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

        var scroll = new UiScrollView
        {
            Height = 440.0f,
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

        scroll.SetContent(fieldsPanel);

        layout.AddChild(scroll);

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

        AddField(fieldsPanel, "texturePath", "Texture path");
        AddField(fieldsPanel, "emissionRate", "Emission rate / second");
        AddField(fieldsPanel, "maxParticles", "Maximum particles");
        AddField(fieldsPanel, "layer", "Render layer");

        AddField(fieldsPanel, "lifetimeMin", "Lifetime minimum (seconds)");
        AddField(fieldsPanel, "lifetimeMax", "Lifetime maximum (seconds)");

        AddField(fieldsPanel, "sizeMin", "Size minimum");
        AddField(fieldsPanel, "sizeMax", "Size maximum");

        AddField(fieldsPanel, "velocityMinX", "Velocity minimum X");
        AddField(fieldsPanel, "velocityMinY", "Velocity minimum Y");
        AddField(fieldsPanel, "velocityMaxX", "Velocity maximum X");
        AddField(fieldsPanel, "velocityMaxY", "Velocity maximum Y");

        _document.Changed += OnDocumentChanged;

        RefreshFields();
    }

    public void ReleaseResources()
    {
        _document.Changed -= OnDocumentChanged;
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
        panel.AddChild(row);
    }

    private void RefreshFields()
    {
        var definition = _document.Definition;

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
    }

    private void SetField(string key, string value)
    {
        _fields[key].SetText(value);
    }

    private void ApplyChanges()
    {
        try
        {
            var current = _document.Definition;

            var updated = new ParticleEffectDefinition(
                current.FormatVersion,
                ReadString("texturePath"),
                ReadFloat("emissionRate"),
                ReadInt("maxParticles"),
                ReadInt("layer"),
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
                        ReadFloat("velocityMaxY"))));

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
        var value = _fields[key].Text.Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException(
                $"'{_fields[key].Placeholder}' cannot be empty.");
        }

        return value;
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
