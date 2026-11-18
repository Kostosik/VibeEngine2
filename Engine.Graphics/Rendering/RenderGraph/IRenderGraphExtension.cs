using Engine.Graphics.Rendering;

namespace Engine.Graphics.Rendering.RenderGraph;

/// <summary>
/// Allows a graphics subsystem to contribute passes
/// to the render graph after a pipeline pass.
/// </summary>
public interface IRenderGraphExtension
{
    /// <summary>
    /// Unique extension name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Name of the RenderPipeline pass after which
    /// this extension should contribute its passes.
    /// </summary>
    string AfterPipelinePassName { get; }

    /// <summary>
    /// Execution order among extensions at the same insertion point.
    /// </summary>
    int Order { get; }


    /// <remarks>
    /// The first pass contributed by an extension must explicitly depend
    /// on context.PreviousPass.
    ///
    /// If the extension replaces the output of a post-process source pass,
    /// its final output-producing pass must call SetPostProcessSource().
    ///
    /// Passes with external side effects must call SetSideEffect().
    /// </remarks>
    /// <summary>
    /// Adds extension passes and resources to the graph.
    /// </summary>
    RenderGraphExtensionResult Build(
        RenderGraphBuilder builder,
        in RenderGraphExtensionContext context);
}