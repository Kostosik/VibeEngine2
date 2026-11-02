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
    private readonly UiButton _duplicateButton;
    private readonly UiButton _multiSelectButton;
    private readonly UiButton _clearSelectionButton;
    private bool _multiSelectMode;
    private readonly UiOverlayLayer? _overlay;
    private readonly UiContextMenu? _contextMenu;
    private readonly Action? _copySelection;
    private readonly Action<Guid>? _pasteSelection;
    private readonly Func<bool>? _canPasteSelection;
    private Guid _contextTargetId;
    public EditorUiHierarchyPanelView(
        EditorUiDocument document,
        UiOverlayLayer? overlays = null,
        Action? copySelection = null,
        Action<Guid>? pasteSelection = null,
        Func<bool>? canPasteSelection = null)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        _document =
            document;

        _overlay = overlays;
        _copySelection = copySelection;
        _pasteSelection = pasteSelection;
        _canPasteSelection = canPasteSelection;

        if (overlays is not null)
        {
            _contextMenu = new UiContextMenu(overlays);

            _contextMenu.AddItem(
                "Duplicate",
                DuplicateSelected,
                HasSingleEditableSelection);

            _contextMenu.AddItem(
                "Copy",
                () => _copySelection?.Invoke(),
                HasSingleEditableSelection);

            _contextMenu.AddItem(
                "Paste",
                () => _pasteSelection?.Invoke(_contextTargetId),
                () => _canPasteSelection?.Invoke() == true);

            _contextMenu.AddItem(
                "Delete",
                DeleteSelected,
                HasSingleEditableSelection);
        }

        Background =
            new UiColor(
                28,
                28,
                28,
                255);

        Padding =
            new UiThickness(
                6.0f);

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

        _duplicateButton = new UiButton("Duplicate")
        {
            Width = 220.0f,
            Height = 32.0f,
            Enabled = false
        };

        _duplicateButton.Clicked += DuplicateSelected;


        toolbar.AddChild(
            _addDropdown);

        _multiSelectButton = new UiButton("Multi-select: Off")
        {
            Width = 220.0f,
            Height = 32.0f
        };

        _multiSelectButton.Clicked += () =>
        {
            _multiSelectMode = !_multiSelectMode;

            _multiSelectButton.Text =
                _multiSelectMode
                    ? "Multi-select: On"
                    : "Multi-select: Off";
        };

        _clearSelectionButton = new UiButton("Clear Selection")
        {
            Width = 220.0f,
            Height = 32.0f
        };

        _clearSelectionButton.Clicked += () =>
        {
            _document.Selection.Clear();
            Refresh();
        };

        toolbar.AddChild(_multiSelectButton);
        toolbar.AddChild(_clearSelectionButton);

        toolbar.AddChild(CreateAlignmentRow(
    "Left",
    "Right",
    EditorUiAlignment.Left,
    EditorUiAlignment.Right));

        toolbar.AddChild(CreateAlignmentRow(
            "Top",
            "Bottom",
            EditorUiAlignment.Top,
            EditorUiAlignment.Bottom));

        toolbar.AddChild(CreateAlignmentRow(
            "Center H",
            "Center V",
            EditorUiAlignment.HorizontalCenter,
            EditorUiAlignment.VerticalCenter));
        toolbar.AddChild(CreateDistributionRow(
    "Distribute H",
    "Distribute V"));
        toolbar.AddChild(_duplicateButton);
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

        var scrollContent = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 6.0f,

            HorizontalAlignment =
                UiHorizontalAlignment.Stretch,

            VerticalAlignment =
                UiVerticalAlignment.Top
        };

        // Панель инструментов и список находятся
        // внутри одной прокручиваемой области.
        scrollContent.AddChild(toolbar);
        scrollContent.AddChild(_items);

        var scrollView = new UiScrollView
        {
            VerticalScrolling = true,
            HorizontalScrolling = false,

            ShowVerticalScrollBar = true,
            ShowHorizontalScrollBar = false,

            ScrollSpeed = 40.0f,

            HorizontalAlignment =
                UiHorizontalAlignment.Stretch,

            VerticalAlignment =
                UiVerticalAlignment.Stretch
        };

        scrollView.SetContent(scrollContent);

        AddChild(scrollView);
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

    private UiStackPanel CreateAlignmentRow(
    string firstText,
    string secondText,
    EditorUiAlignment firstAlignment,
    EditorUiAlignment secondAlignment)
    {
        var row = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 4.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        UiButton CreateButton(
            string text,
            EditorUiAlignment alignment)
        {
            var button = new UiButton(text)
            {
                Width = 106.0f,
                Height = 30.0f,
                FontSize = 11.0f
            };

            button.Clicked += () =>
            {
                if (EditorUiLayoutAlignment.Align(
                        _document,
                        alignment))
                {
                    Refresh();
                }
            };

            return button;
        }

        row.AddChild(CreateButton(
            firstText,
            firstAlignment));

        row.AddChild(CreateButton(
            secondText,
            secondAlignment));

        return row;
    }

    private void AddElementRow(
        EditorUiElement element,
        int depth)
    {
        var row = new EditorUiHierarchyElementRow(
            _document,
            element,
            depth,
            _overlay);

        var button = row.Button;

        button.Clicked += () =>
        {
            if (_multiSelectMode)
            {
                if (!_document.Selection.Remove(element.Id))
                {
                    _document.Selection.Add(element.Id);
                }
            }
            else
            {
                _document.Selection.Set(element.Id);
            }

            Refresh();
        };

        button.RightClicked += pointer =>
        {
            if (!_document.Selection.Contains(element.Id))
            {
                _document.Selection.Set(element.Id);
            }

            _contextTargetId = element.Id;

            Refresh();

            _contextMenu?.Open(pointer.Position);
        };

        row.Dropped += Refresh;

        _items.AddChild(row);
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

    public void ReleaseResources()
    {
        _contextMenu?.Close();

        if (_contextMenu is not null)
        {
            _overlay?.Hide(_contextMenu);
        }
    }

    private UiStackPanel CreateDistributionRow(
    string horizontalText,
    string verticalText)
    {
        var row = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 4.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        UiButton CreateButton(
            string text,
            EditorUiDistributionAxis axis)
        {
            var button = new UiButton(text)
            {
                Width = 106.0f,
                Height = 30.0f,
                FontSize = 11.0f
            };

            button.Clicked += () =>
            {
                if (EditorUiLayoutAlignment.Distribute(
                        _document,
                        axis))
                {
                    Refresh();
                }
            };

            return button;
        }

        row.AddChild(CreateButton(
            horizontalText,
            EditorUiDistributionAxis.Horizontal));

        row.AddChild(CreateButton(
            verticalText,
            EditorUiDistributionAxis.Vertical));

        return row;
    }

    private bool HasSingleEditableSelection()
    {
        if (_document.Selection.Count != 1)
        {
            return false;
        }

        var id = _document.Selection.Items.First();

        return id != _document.Root.Id &&
               _document.ContainsElement(id);
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

    public void DuplicateSelection()
    {
        DuplicateSelected();
    }

    private void DuplicateSelected()
    {
        if (_document.Selection.Count != 1)
        {
            return;
        }

        var selectedId = _document.Selection.Items.First();

        if (selectedId == _document.Root.Id)
        {
            return;
        }

        var command = new DuplicateEditorUiElementCommand(
            _document,
            selectedId);

        _document.Execute(command);

        _document.Selection.Set(
            command.DuplicatedElement.Id);

        Refresh();
    }

    private void UpdateDeleteState()
    {
        _deleteButton.Enabled =
            _document.Selection.Count == 1 &&
            _document.Selection.Items.First() !=
            _document.Root.Id;
        _duplicateButton.Enabled =
            _document.Selection.Count == 1 &&
            _document.Selection.Items.First() != _document.Root.Id;
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
            EditorUiElementType.ScrollView,
            EditorUiElementType.Slider
        };
    }
}