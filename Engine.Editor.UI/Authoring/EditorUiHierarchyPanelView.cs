using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Authoring;

public sealed class EditorUiHierarchyPanelView :
    UiPanel
{
    private readonly EditorUiDocument _document;
    private readonly UiStackPanel _items;
    private readonly UiDropdown _addDropdown;
    private readonly UiButton _deleteButton;

    public EditorUiHierarchyPanelView(
        EditorUiDocument document)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        _document =
            document;

        Background =
            new UiColor(
                28,
                28,
                28,
                255);

        Padding =
            new UiThickness(
                6.0f);

        var content =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Vertical,

                Spacing = 6.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Stretch
            };

        var toolbar =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Vertical,

                Spacing = 4.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Top
            };

        _addDropdown =
            new UiDropdown
            {
                Width = 220.0f,
                Height = 36.0f
            };

        foreach (var type in GetCreatableTypes())
        {
            _addDropdown.AddOption(
                type.ToString());
        }

        _addDropdown.SelectionChanged +=
            OnAddTypeSelected;

        _deleteButton =
            new UiButton(
                "Delete")
            {
                Width = 220.0f,
                Height = 32.0f,
                Enabled = false
            };

        _deleteButton.Clicked +=
            DeleteSelected;

        toolbar.AddChild(
            _addDropdown);

        toolbar.AddChild(
            _deleteButton);

        _items =
            new UiStackPanel
            {
                Orientation =
                    UiOrientation.Vertical,

                Spacing = 2.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Top
            };

        var scrollView =
            new UiScrollView
            {
                VerticalScrolling = true,
                HorizontalScrolling = false,
                ScrollSpeed = 32.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                VerticalAlignment =
                    UiVerticalAlignment.Stretch
            };

        scrollView.SetContent(
            _items);

        content.AddChild(
            toolbar);

        content.AddChild(
            scrollView);

        AddChild(
            content);
    }

    public EditorUiDocument Document =>
        _document;

    public void Refresh()
    {
        _items.ClearChildren();

        UpdateDeleteState();

        foreach (var element in
                 _document.EnumerateDepthFirst())
        {
            AddElementRow(
                element,
                GetDepth(element));
        }

        if (_document.Root.Children.Count == 0)
        {
            _items.AddChild(
                new UiLabel(
                    "No UI elements")
                {
                    FontSize = 13.0f,

                    Color =
                        new UiColor(
                            150,
                            150,
                            150,
                            255),

                    Margin =
                        new UiThickness(
                            8.0f,
                            4.0f,
                            4.0f,
                            4.0f)
                });
        }
    }

    private void AddElementRow(
        EditorUiElement element,
        int depth)
    {
        var selected =
            _document.Selection.Contains(
                element.Id);

        var prefix =
            selected
                ? "● "
                : "  ";

        var indentation =
            depth * 14.0f;

        var button =
            new UiButton(
                prefix +
                GetElementLabel(element))
            {
                Height = 30.0f,

                HorizontalAlignment =
                    UiHorizontalAlignment.Stretch,

                Margin =
                    new UiThickness(
                        indentation,
                        0.0f,
                        0.0f,
                        0.0f)
            };

        button.Clicked +=
            () =>
            {
                _document.Selection.Set(
                    element.Id);

                Refresh();
            };

        _items.AddChild(
            button);
    }

    private void OnAddTypeSelected(
        int index,
        string text)
    {
        var types =
            GetCreatableTypes();

        if (index < 0 ||
            index >= types.Count)
        {
            return;
        }

        var type =
            types[index];

        var parentId =
            GetSelectedParentId();

        var element =
            _document.AddElement(
                type,
                parentId);

        _document.Selection.Set(
            element.Id);

        _addDropdown.ClearOptions();

        foreach (var availableType in
                 types)
        {
            _addDropdown.AddOption(
                availableType.ToString());
        }

        Refresh();
    }

    private void DeleteSelected()
    {
        if (_document.Selection.Count != 1)
        {
            return;
        }

        var selectedId =
            _document.Selection.Items.First();

        if (selectedId ==
            _document.Root.Id)
        {
            return;
        }

        if (!_document.RemoveElement(
                selectedId))
        {
            return;
        }

        Refresh();
    }

    private void UpdateDeleteState()
    {
        _deleteButton.Enabled =
            _document.Selection.Count == 1 &&
            _document.Selection.Items.First() !=
            _document.Root.Id;
    }

    private Guid GetSelectedParentId()
    {
        if (_document.Selection.Count != 1)
        {
            return _document.Root.Id;
        }

        var selectedId =
            _document.Selection.Items.First();

        return _document.ContainsElement(
                selectedId)
            ? selectedId
            : _document.Root.Id;
    }

    private static string GetElementLabel(
        EditorUiElement element)
    {
        return $"{element.Name} [{element.Type}]";
    }

    private static int GetDepth(
        EditorUiElement element)
    {
        var depth = 0;
        var current = element.Parent;

        while (current is not null)
        {
            depth++;
            current = current.Parent;
        }

        return depth;
    }

    private static IReadOnlyList<EditorUiElementType>
        GetCreatableTypes()
    {
        return new[]
        {
            EditorUiElementType.Panel,
            EditorUiElementType.Label,
            EditorUiElementType.Button,
            EditorUiElementType.Image,
            EditorUiElementType.TextBox,
            EditorUiElementType.Toggle,
            EditorUiElementType.Dropdown,
            EditorUiElementType.ScrollView
        };
    }
}