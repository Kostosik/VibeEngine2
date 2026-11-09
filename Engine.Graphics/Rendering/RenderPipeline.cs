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

        IPostProcessRenderPassExecutor? postProcessExecutor = null;

        if (postProcessingEnabled)
        {
            postProcessExecutor =
                executor as IPostProcessRenderPassExecutor
                ?? throw new NotSupportedException(
                    "The render pass executor does not support post-processing.");

            var sourcePassCount = 0;

            foreach (var pass in _passes)
            {
                if (pass.IsPostProcessSource)
                    sourcePassCount++;
            }

            if (sourcePassCount != 1)
            {
                throw new InvalidOperationException(
                    "An enabled post-process chain requires exactly one " +
                    "render pass marked as its source.");
            }
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
                IsPostProcessSource =
                    postProcessingEnabled && pass.IsPostProcessSource
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

            // Обрабатываем мир сразу после завершения World Pass.
            // UI и Debug проходят позже и остаются поверх результата.
            if (context.IsPostProcessSource)
            {
                postProcessExecutor!.ExecutePostProcessing(
                    PostProcessing);
            }
        }
    }
}