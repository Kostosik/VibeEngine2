namespace Engine.Graphics.Rendering;

public sealed class RenderPipeline
{
    private readonly List<RenderPass> _passes = new();

    public IReadOnlyList<RenderPass> Passes =>
        _passes;

    public int Count =>
        _passes.Count;

    public void AddPass(
        RenderPass pass)
    {
        _passes.Add(pass);
    }

    public void Clear()
    {
        _passes.Clear();
    }

    public void Execute(
     RenderQueue queue,
     IRenderPassExecutor executor)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(executor);

        if (_passes.Count == 0)
        {
            return;
        }

        queue.Sort();

        foreach (var pass in _passes)
        {
            executor.Begin(
                new RenderPassContext(
                    pass.Target,
                    pass.State,
                    pass.ClearColor));

            foreach (var item in queue.Items)
            {
                if (!pass.Layers.Contains(
                        item.Layer))
                {
                    continue;
                }

                executor.Execute(
                    item.Command);
            }

            executor.End();
        }
    }
}