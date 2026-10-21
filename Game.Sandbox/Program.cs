using Engine.Content.Loading;
using Engine.Core.Application;
using Engine.Core.Assets;
using Engine.Graphics.Fonts;
using Engine.Graphics.Resources;
using Engine.Hosting.Desktop;
using Engine.Input;
using Engine.Scripting;
using Engine.Serialization.Content;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Game;
using Engine.Serialization.UI;
using Engine.Tooling.Debugging;
using Engine.UI.Assets;
using Engine.UI.Core;
using Engine.UI.Styling;
using Game.Sandbox;

if (args.Any(
        arg => string.Equals(
            arg,
            "--engine-game-probe",
            StringComparison.OrdinalIgnoreCase)))
{
    DesktopGameHost.Run(
        runtime => new EngineGameProbe(runtime),
        new DesktopGameHostOptions
        {
            Title = "VibeEngine EngineGame Probe",
            Width = 1280,
            Height = 720
        });

    return;

}


if (!SandboxNetworkRoleOptions.TryParse(
        args,
        out var multiplayerOptions,
        out var parseError))
{
    Console.Error.WriteLine(
        $"Invalid Game.Sandbox arguments: {parseError}");

    Console.Error.WriteLine(
        "Server: --server --bind 127.0.0.1 --port 27015");

    Console.Error.WriteLine(
        "Client: --client --host 127.0.0.1 " +
        "--port 27015 --local-port 27016");

    Environment.ExitCode = 2;
    return;
}


var sandboxActions = new SandboxInputActions();

DesktopGameHost.Run(

(DesktopGameHostContext context) =>
    new SandboxGame(
        context,
        sandboxActions,
        multiplayerOptions),

    new DesktopGameHostOptions
    {

        Title = multiplayerOptions is null
    ? "VibeEngine2 Sandbox"
    : multiplayerOptions.Role == SandboxNetworkRole.Server
        ? "VibeEngine2 Sandbox - Server"
        : "VibeEngine2 Sandbox - Client",

        Width = 1280,
        Height = 720,

        InputActions = sandboxActions.ActionMap,

        ConfigureContentLoaders = loaders =>
        {
            loaders.Register(
                new BinaryContentLoader<UiAsset>(
                    new UiAssetSerializer(),
                    asset => asset.Extension.Equals(
                        ".ui",
                        StringComparison.OrdinalIgnoreCase)));

            loaders.Register(
                new JsonScriptDefinitionContentLoader());
        }
    });