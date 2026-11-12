namespace Engine.Graphics.Rendering;

public sealed class RenderPipeline
{
    private readonly List<RenderPass> _passes = new();

    public IReadOnlyList<RenderPass> Passes => _passes;

    public int Count => _passes.Count;

    public PostProcessChain PostProcessing { get; } = new();

    public void AddPass(RenderPass pass)
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

        var postProcessingEnabled =
            PostProcessing.HasEnabledEffects;

        var sourcePassCount = 0;

        foreach (var pass in _passes)
        {
            if (pass.IsPostProcessSource)
                sourcePassCount++;
        }

        if (sourcePassCount > 1 ||
            (postProcessingEnabled && sourcePassCount != 1))
        {
            throw new InvalidOperationException(
                "An enabled post-process chain requires exactly one " +
                "render pass marked as its source.");
        }

        IPostProcessRenderPassExecutor? postProcessExecutor = null;

        // Source pass теперь нужен не только для эффектов,
        // но и для правильной презентации RenderTarget.
        if (sourcePassCount > 0)
        {
            postProcessExecutor =
                executor as IPostProcessRenderPassExecutor
                ?? throw new NotSupportedException(
                    "The render pass executor does not support post-processing.");
        }

        if (_passes.Count == 0)
            return;

        queue.Sort();

        foreach (var pass in _passes)
        {
            var context = new RenderPassContext(
                pass.Target,
                pass.State,
                pass.ClearColor)
            {
                IsPostProcessSource = pass.IsPostProcessSource,
                ContextData = pass.ContextData
            };

            executor.Begin(context);

            try
            {
                foreach (var item in queue.Items)
                {
                    if (!pass.Layers.Contains(item.Layer))
                        continue;

                    if (item.HasWorldTextureData)
                    {
                        executor.Execute(
                            queue.GetWorldTextureData(
                                item.WorldTextureDataIndex));
                    }
                    else
                    {
                        executor.Execute(item.Command);
                    }
                }
            }
            finally
            {
                executor.End();
            }

            // Всегда презентуем результат World pass.
            // Если эффекты отключены, выполняется только копирование.
            if (context.IsPostProcessSource)
            {
                postProcessExecutor!.ExecutePostProcessing(
                    PostProcessing);
            }
        }
    }
}