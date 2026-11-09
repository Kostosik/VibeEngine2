namespace Engine.Graphics.Rendering;

/// <summary>
/// Implemented by render backends that support post-processing.
/// </summary>
public interface IPostProcessRenderPassExecutor
{
    void ExecutePostProcessing(PostProcessChain chain);
}