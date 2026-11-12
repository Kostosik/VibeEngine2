using Engine.Graphics.Rendering;

namespace Engine.Graphics3D.Lighting;

public sealed class SceneLighting3D : IRenderPassContextData
{
    public const int MaxPointLights = 4;

    public DirectionalLight3D DirectionalLight { get; }

    public IReadOnlyList<PointLight3D> PointLights { get; }

    public static SceneLighting3D Default { get; } = new(
        DirectionalLight3D.Default);

    public SceneLighting3D(
        DirectionalLight3D directionalLight,
        IEnumerable<PointLight3D>? pointLights = null)
    {
        var lights = pointLights?.ToArray()
            ?? Array.Empty<PointLight3D>();

        if (lights.Length > MaxPointLights)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pointLights),
                $"Maximum supported point lights: {MaxPointLights}.");
        }

        DirectionalLight = directionalLight;
        PointLights = Array.AsReadOnly(lights);
    }
}