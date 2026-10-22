using Engine.Core.Math;
using Engine.UI.Core;

namespace Engine.UI.Input;

internal sealed class UiDragDropCoordinator
{
    private readonly UiRoot _root;

    private UiWidget? _candidateWidget;
    private IUiDragSource? _candidateSource;

    private UiWidget? _sourceWidget;
    private IUiDragSource? _source;
    private UiDragPayload? _payload;

    private UiWidget? _targetWidget;
    private IUiDropTarget? _target;

    private Vector2 _startPosition;
    private Vector2 _lastPosition;

    private bool _dragActive;

    private float _dragStartThreshold = 5.0f;

    public UiDragDropCoordinator(
        UiRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);

        _root = root;
    }

    public bool IsDragging => _dragActive;

    public UiWidget? SourceWidget => _sourceWidget;

    public float DragStartThreshold
    {
        get => _dragStartThreshold;

        set
        {
            if (!float.IsFinite(value) || value < 0.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value));
            }

            _dragStartThreshold = value;
        }
    }

    public void PointerDown(
        UiWidget? hit,
        Vector2 position,
        UiWidget? capturedWidget)
    {
        if (_dragActive)
        {
            FinishDrag(
                UiDragEndReason.Cancelled,
                position);
        }

        ClearCandidate();

        _lastPosition = position;

        // Не конфликтуем с уже захваченным указателем,
        // например с перемещением gizmo.
        if (capturedWidget is not null)
        {
            return;
        }

        _candidateWidget = FindDragSource(hit);

        _candidateSource =
            _candidateWidget as IUiDragSource;

        _startPosition = position;
    }

    public bool Update(
        Vector2 position,
        bool pointerDown,
        bool pointerReleased,
        UiWidget? capturedWidget,
        UiWidget? hoveredWidget)
    {
        _lastPosition = position;

        if (!_dragActive)
        {
            return TryStartDrag(
                position,
                pointerDown,
                pointerReleased,
                capturedWidget,
                hoveredWidget);
        }

        if (_sourceWidget is null ||
            _source is null ||
            _payload is null)
        {
            ResetSession();
            return false;
        }

        if (!IsAvailable(_sourceWidget))
        {
            FinishDrag(
                UiDragEndReason.Cancelled,
                position);

            return false;
        }

        // Если кнопка пропала без события отпускания,
        // завершаем операцию отменой.
        if (!pointerDown && !pointerReleased)
        {
            FinishDrag(
                UiDragEndReason.Cancelled,
                position);

            return false;
        }

        if (capturedWidget is not null &&
            !ReferenceEquals(
                capturedWidget,
                _sourceWidget))
        {
            FinishDrag(
                UiDragEndReason.Cancelled,
                position);

            return false;
        }

        UpdateDropTarget(
            position,
            hoveredWidget);

        _source.OnDragMoved(
            CreateContext(
                position,
                _targetWidget));

        return false;
    }

    public void PointerReleased(
        Vector2 position,
        UiWidget? hoveredWidget)
    {
        _lastPosition = position;

        if (!_dragActive)
        {
            ClearCandidate();
            return;
        }

        if (_sourceWidget is null ||
            !IsAvailable(_sourceWidget))
        {
            FinishDrag(
                UiDragEndReason.Cancelled,
                position);

            return;
        }

        var result = UiDragEndReason.Rejected;

        try
        {
            UpdateDropTarget(
                position,
                hoveredWidget);

            if (_target is not null &&
                _targetWidget is not null &&
                _target.OnDrop(
                    CreateContext(
                        position,
                        _targetWidget)))
            {
                result = UiDragEndReason.Dropped;
            }
        }
        finally
        {
            FinishDrag(
                result,
                position);
        }
    }

    public bool Cancel()
    {
        ClearCandidate();

        if (!_dragActive)
        {
            return false;
        }

        FinishDrag(
            UiDragEndReason.Cancelled,
            _lastPosition);

        return true;
    }

    private bool TryStartDrag(
        Vector2 position,
        bool pointerDown,
        bool pointerReleased,
        UiWidget? capturedWidget,
        UiWidget? hoveredWidget)
    {
        if (_candidateWidget is null ||
            _candidateSource is null)
        {
            return false;
        }

        if (!IsAvailable(_candidateWidget))
        {
            ClearCandidate();
            return false;
        }

        if (!pointerDown)
        {
            if (!pointerReleased)
            {
                ClearCandidate();
            }

            return false;
        }

        if (capturedWidget is not null)
        {
            ClearCandidate();
            return false;
        }

        var dx = position.X - _startPosition.X;
        var dy = position.Y - _startPosition.Y;

        var distanceSquared =
            dx * dx + dy * dy;

        var thresholdSquared =
            _dragStartThreshold * _dragStartThreshold;

        if (distanceSquared <= thresholdSquared)
        {
            return false;
        }

        var payload =
            _candidateSource.CreateDragPayload(
                _startPosition);

        if (payload is null)
        {
            ClearCandidate();
            return false;
        }

        _sourceWidget = _candidateWidget;
        _source = _candidateSource;
        _payload = payload;
        _dragActive = true;

        ClearCandidate();

        _source.OnDragStarted(
            CreateContext(
                position,
                null));

        UpdateDropTarget(
            position,
            hoveredWidget);

        _source.OnDragMoved(
            CreateContext(
                position,
                _targetWidget));

        return true;
    }

    private void UpdateDropTarget(
        Vector2 position,
        UiWidget? hoveredWidget)
    {
        var nextWidget =
            FindDropTarget(hoveredWidget);

        if (ReferenceEquals(
                nextWidget,
                _targetWidget))
        {
            if (_target is not null)
            {
                _target.OnDragOver(
                    CreateContext(
                        position,
                        _targetWidget));
            }

            return;
        }

        if (_target is not null)
        {
            _target.OnDragLeave(
                CreateContext(
                    position,
                    _targetWidget));
        }

        _targetWidget = nextWidget;
        _target = nextWidget as IUiDropTarget;

        if (_target is not null)
        {
            var context =
                CreateContext(
                    position,
                    _targetWidget);

            _target.OnDragEnter(context);
            _target.OnDragOver(context);
        }
    }

    private UiWidget? FindDragSource(
        UiWidget? widget)
    {
        for (var current = widget;
             current is not null;
             current = current.Parent)
        {
            if (current is IUiDragSource)
            {
                return current;
            }
        }

        return null;
    }

    private UiWidget? FindDropTarget(
        UiWidget? widget)
    {
        if (_payload is null)
        {
            return null;
        }

        for (var current = widget;
             current is not null;
             current = current.Parent)
        {
            if (current is IUiDropTarget target &&
                target.CanAcceptDrop(_payload))
            {
                return current;
            }
        }

        return null;
    }

    private bool IsAvailable(
        UiWidget widget)
    {
        for (var current = widget;
             current is not null;
             current = current.Parent)
        {
            if (!current.Visible ||
                !current.Enabled)
            {
                return false;
            }

            if (ReferenceEquals(
                    current,
                    _root))
            {
                return true;
            }
        }

        return false;
    }

    private UiDragContext CreateContext(
        Vector2 position,
        UiWidget? target)
    {
        return new UiDragContext(
            _payload!,
            _sourceWidget!,
            target,
            _startPosition,
            position);
    }

    private void FinishDrag(
        UiDragEndReason reason,
        Vector2 position)
    {
        if (!_dragActive)
        {
            ClearCandidate();
            return;
        }

        var context =
            CreateContext(
                position,
                _targetWidget);

        var source = _source;
        var target = _target;

        ResetSession();

        try
        {
            if (target is not null)
            {
                target.OnDragLeave(context);
            }
        }
        finally
        {
            source?.OnDragEnded(
                context,
                reason);
        }
    }

    private void ClearCandidate()
    {
        _candidateWidget = null;
        _candidateSource = null;
    }

    private void ResetSession()
    {
        _dragActive = false;

        _sourceWidget = null;
        _source = null;
        _payload = null;

        _targetWidget = null;
        _target = null;
    }
}