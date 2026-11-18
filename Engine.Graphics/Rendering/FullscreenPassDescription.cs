using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering;

/// <summary>
/// Backend-neutral description of a fullscreen operation.
/// Input and output resources are managed by the render graph.
/// </summary>
public sealed class FullscreenPassDescription
{
    public FullscreenPassDescription(
        ShaderHandle shader,
        Action<IShaderParameterWriter>? applyParameters = null,
        IEnumerable<FullscreenPassTextureInput>? inputs = null)
    {
        if (!shader.IsValid)
        {
            throw new ArgumentException(
                "A valid shader handle is required.",
                nameof(shader));
        }

        var inputArray = inputs?.ToArray()
            ?? Array.Empty<FullscreenPassTextureInput>();

        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var input in inputArray)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(input.Name);

            if (!input.Resource.IsValid)
            {
                throw new ArgumentException(
                    "Every fullscreen input must reference a valid render-graph resource.",
                    nameof(inputs));
            }

            if (!names.Add(input.Name))
            {
                throw new ArgumentException(
                    $"Fullscreen input name '{input.Name}' is duplicated.",
                    nameof(inputs));
            }
        }

        Shader = shader;
        ApplyParameters = applyParameters;
        Inputs = Array.AsReadOnly(inputArray);
    }

    public ShaderHandle Shader { get; }

    public Action<IShaderParameterWriter>? ApplyParameters { get; }

    public IReadOnlyList<FullscreenPassTextureInput> Inputs { get; }
}