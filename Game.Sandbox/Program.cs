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

var sandboxActions = new SandboxInputActions();

DesktopGameHost.Run(
    (DesktopGameHostContext context) =>
        new SandboxGame(
            context,
            sandboxActions),
    new DesktopGameHostOptions
    {
        Title = "VibeEngine2 Sandbox",
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