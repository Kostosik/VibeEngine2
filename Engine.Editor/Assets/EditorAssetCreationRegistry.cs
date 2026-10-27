namespace Engine.Editor.Assets;

public sealed class EditorAssetCreationRegistry
{
    private readonly List<EditorAssetCreationDefinition> _definitions = new();

    public IReadOnlyList<EditorAssetCreationDefinition> Definitions =>
        _definitions.AsReadOnly();

    public void Register(EditorAssetCreationDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (_definitions.Any(
                item => string.Equals(
                    item.Id,
                    definition.Id,
                    StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"Asset creation type '{definition.Id}' is already registered.");
        }

        _definitions.Add(definition);
    }

    public EditorAssetCreationDefinition GetRequired(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        return _definitions.FirstOrDefault(
                   item => string.Equals(
                       item.Id,
                       id,
                       StringComparison.OrdinalIgnoreCase))
               ?? throw new KeyNotFoundException(
                   $"Asset creation type '{id}' is not registered.");
    }

    public static EditorAssetCreationRegistry CreateDefault()
    {
        var registry = new EditorAssetCreationRegistry();

        registry.Register(
            new EditorAssetCreationDefinition(
                id: "folder",
                displayName: "Folder",
                defaultName: "New Folder",
                kind: EditorAssetCreationKind.Directory,
                extension: string.Empty));

        registry.Register(
            new EditorAssetCreationDefinition(
                id: "particle-effect",
                displayName: "Particle Effect",
                defaultName: "New Particle Effect",
                kind: EditorAssetCreationKind.TextFile,
                extension: ".particle.json",
                    contentFactory: static _ => """
                    {
                      "formatVersion": 2,
                      "texturePath": "Textures/particle.png",
                      "layer": 0,
                      "emitter": {
                        "ratePerSecond": 12,
                        "maxParticles": 128,
                        "shape": {
                          "type": "point"
                        }
                      },
                      "particle": {
                        "lifetimeSeconds": {
                          "min": 0.8,
                          "max": 1.4
                        },
                        "size": {
                          "min": 2,
                          "max": 6
                        },
                        "velocity": {
                          "min": {
                            "x": -8,
                            "y": -24
                          },
                          "max": {
                            "x": 8,
                            "y": -4
                          }
                        },
                        "modifiers": []
                      }
                    }
                    """));

        registry.Register(
    new EditorAssetCreationDefinition(
        id: "animation-clip",
        displayName: "Animation Clip",
        defaultName: "New Animation",
        kind: EditorAssetCreationKind.TextFile,
        extension: ".animation.json",
        contentFactory: static _ => """
        {
          "formatVersion": 1,
          "durationSeconds": 1.0,
          "tracks": []
        }
        """));
        registry.Register(
    new EditorAssetCreationDefinition(
        id: "sprite-animation",
        displayName: "Sprite Animation",
        defaultName: "New Sprite Animation",
        kind: EditorAssetCreationKind.TextFile,
        extension: ".sprite-animation.json",
        contentFactory: static _ => """
        {
          "formatVersion": 1,
          "frameDurationSeconds": 0.1,
          "loop": true,
          "frames": []
        }
        """));
        registry.Register(
            new EditorAssetCreationDefinition(
                id: "script-definition",
                displayName: "Script Definition",
                defaultName: "New Script",
                kind: EditorAssetCreationKind.TextFile,
                extension: ".script.json",
                contentFactory: CreateScriptDefinitionTemplate));

        registry.Register(
            new EditorAssetCreationDefinition(
                id: "world-document",
                displayName: "World",
                defaultName: "New World",
                kind: EditorAssetCreationKind.GeneratedFile,
                extension: ".world"));

        registry.Register(
            new EditorAssetCreationDefinition(
                id: "ui-document",
                displayName: "UI Document",
                defaultName: "New UI",
                kind: EditorAssetCreationKind.GeneratedFile,
                extension: ".ui"));

        return registry;
    }

    private static string CreateScriptDefinitionTemplate(string name)
    {
        var slug = new string(
            name.ToLowerInvariant()
                .Select(
                    character => char.IsAsciiLetterOrDigit(character)
                        ? character
                        : '_')
                .ToArray())
            .Trim('_');

        while (slug.Contains("__", StringComparison.Ordinal))
        {
            slug = slug.Replace(
                "__",
                "_",
                StringComparison.Ordinal);
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "new_script";
        }

        return $$"""
        {
          "formatVersion": 1,
          "scriptId": "game.{{slug}}",
          "defaultConfiguration": {}
        }
        """;
    }
}