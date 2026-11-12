
using Engine.Graphics.Resources;

namespace Engine.Graphics.Rendering;

public static class FullscreenShaderSource
{
    public const string SourceTextureUniform = "uSource";
    public const string ResolutionUniform = "uResolution";

    // GLSL fullscreen triangle.
    public const string GlslVertex = """
        #version 330 core

        out vec2 vTexCoord;

        void main()
        {
            vec2 position;

            if (gl_VertexID == 0)
                position = vec2(-1.0, -1.0);
            else if (gl_VertexID == 1)
                position = vec2(3.0, -1.0);
            else
                position = vec2(-1.0, 3.0);

            vTexCoord = position * 0.5 + 0.5;
            gl_Position = vec4(position, 0.0, 1.0);
        }
        """;

    // Compatibility alias for existing GLSL consumers.
    public const string Vertex = GlslVertex;

    // HLSL fullscreen triangle.
    public const string HlslVertex = """
        struct FullscreenVertexOutput
        {
            float4 Position : SV_POSITION;
            float2 TexCoord : TEXCOORD0;
        };

        FullscreenVertexOutput VSMain(uint vertexId : SV_VertexID)
        {
            FullscreenVertexOutput output;

            float2 uv = float2(
                float((vertexId << 1) & 2),
                float(vertexId & 2));

            output.Position = float4(
                uv.x * 2.0f - 1.0f,
                1.0f - uv.y * 2.0f,
                0.0f,
                1.0f);

            output.TexCoord = uv;

            return output;
        }
        """;

    /// <summary>
    /// Creates a shader description with GLSL and HLSL variants.
    /// </summary>
    public static ShaderDescription CreateDescription(
        string glslFragmentSource,
        string hlslFragmentSource)
    {
        return new ShaderDescription(
            new ShaderVariant(
                ShaderLanguage.Glsl,
                GlslVertex,
                glslFragmentSource),

            new ShaderVariant(
                ShaderLanguage.Hlsl,
                HlslVertex,
                hlslFragmentSource,
                "VSMain",
                "PSMain"));
    }
}
