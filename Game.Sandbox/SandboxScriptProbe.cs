using System.Text.Json;
using Engine.Core.Time;
using Engine.Scripting;

namespace Game.Sandbox;

public sealed class SandboxScriptProbe : IEntityScript
{
    public int AttachCount { get; private set; }

    public int UpdateCount { get; private set; }

    public int FixedUpdateCount { get; private set; }

    public string DisplayName { get; private set; } =
        "Script Probe";

    public string ConfigurationJson { get; private set; } =
        "{}";

    public void OnAttach(ScriptContext context)
    {
        AttachCount++;

        ConfigurationJson =
            context.ConfigurationJson;

        using var document =
            JsonDocument.Parse(ConfigurationJson);

        if (document.RootElement.TryGetProperty(
                "displayName",
                out var element) &&
            element.ValueKind == JsonValueKind.String)
        {
            DisplayName =
                element.GetString() ?? "Script Probe";
        }
    }

    public void Update(
        ScriptContext context,
        TimeSnapshot time)
    {
        UpdateCount++;
    }

    public void FixedUpdate(
        ScriptContext context,
        SimulationTime time)
    {
        FixedUpdateCount++;
    }
}