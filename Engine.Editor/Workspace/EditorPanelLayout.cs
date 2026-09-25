namespace Engine.Editor.Workspace;

public sealed class EditorPanelLayout
{
    public EditorPanelLayout(
        string panelId,
        EditorDockArea area = EditorDockArea.Center)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            panelId);

        PanelId = panelId;
        Area = area;
    }

    public string PanelId { get; }

    public EditorDockArea Area { get; private set; }

    public int Order { get; private set; }

    public float Size { get; private set; }

    public bool IsActive { get; private set; }

    public void SetArea(
        EditorDockArea area)
    {
        Area = area;
    }

    public void SetOrder(
        int order)
    {
        if (order < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(order));
        }

        Order = order;
    }

    public void SetSize(
        float size)
    {
        if (size < 0.0f ||
            float.IsNaN(size) ||
            float.IsInfinity(size))
        {
            throw new ArgumentOutOfRangeException(
                nameof(size));
        }

        Size = size;
    }

    public void SetActive(
        bool active)
    {
        IsActive = active;
    }
}