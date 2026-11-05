using Engine.ECS.Prefabs;
using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

public sealed class PrefabDocumentView : UiPanel
{
    private readonly PrefabAsset _asset;
    private readonly UiStackPanel _content;

    private PrefabEntityId _selectedEntityId;

    public PrefabDocumentView(PrefabAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        _asset = asset;
        _selectedEntityId = asset.RootEntity;

        Background = new UiColor(30, 30, 30, 255);
        Padding = new UiThickness(8.0f);

        _content = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 5.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        var scrollView = new UiScrollView
        {
            VerticalScrolling = true,
            HorizontalScrolling = false,
            ShowHorizontalScrollBar = false,
            ShowVerticalScrollBar = true,
            ScrollSpeed = 32.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        scrollView.SetContent(_content);
        AddChild(scrollView);

        Refresh();
    }

    private void Refresh()
    {
        _content.ClearChildren();

        _content.AddChild(new UiLabel(_asset.Name)
        {
            FontSize = 20.0f,
            Color = UiColor.White
        });

        _content.AddChild(new UiLabel(
            $"Entities: {_asset.Entities.Count}")
        {
            FontSize = 13.0f,
            Color = new UiColor(170, 170, 170, 255)
        });

        _content.AddChild(new UiLabel(
            $"Root: Entity {_asset.RootEntity.Value}")
        {
            FontSize = 13.0f,
            Color = new UiColor(170, 170, 170, 255)
        });

        _content.AddChild(new UiLabel("Hierarchy")
        {
            FontSize = 16.0f,
            Color = UiColor.White
        });

        foreach (var entity in _asset.Entities)
        {
            var isRoot = entity.Id == _asset.RootEntity;
            var isSelected = entity.Id == _selectedEntityId;

            var prefix = isSelected ? "> " : "  ";
            var rootSuffix = isRoot ? " [Root]" : string.Empty;

            var button = new UiButton(
                $"{prefix}Entity {entity.Id.Value}{rootSuffix}")
            {
                Height = 30.0f,
                FontSize = 13.0f,
                HorizontalAlignment = UiHorizontalAlignment.Stretch
            };

            var entityId = entity.Id;

            button.Clicked += () =>
            {
                _selectedEntityId = entityId;
                Refresh();
            };

            _content.AddChild(button);
        }

        _content.AddChild(new UiLabel("Selected Entity")
        {
            FontSize = 16.0f,
            Color = UiColor.White
        });

        var selectedEntity = _asset.Entities.FirstOrDefault(
            entity => entity.Id == _selectedEntityId);

        if (selectedEntity is null)
        {
            _content.AddChild(new UiLabel("No entity selected")
            {
                FontSize = 13.0f,
                Color = new UiColor(170, 170, 170, 255)
            });

            return;
        }

        _content.AddChild(new UiLabel(
            $"Entity {selectedEntity.Id.Value}")
        {
            FontSize = 14.0f,
            Color = new UiColor(210, 210, 210, 255)
        });

        _content.AddChild(new UiLabel(
            $"Components: {selectedEntity.Components.Count}")
        {
            FontSize = 13.0f,
            Color = new UiColor(170, 170, 170, 255)
        });

        foreach (var component in selectedEntity.Components)
        {
            _content.AddChild(new UiLabel(
                component.ComponentType.Name)
            {
                FontSize = 13.0f,
                Color = new UiColor(190, 200, 210, 255)
            });
        }

        if (selectedEntity.Components.Count == 0)
        {
            _content.AddChild(new UiLabel("No components")
            {
                FontSize = 13.0f,
                Color = new UiColor(140, 140, 140, 255)
            });
        }
    }
}