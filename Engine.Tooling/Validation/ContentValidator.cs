using Engine.Content.Assets;
using Engine.Content.Loading;

namespace Engine.Tooling.Validation;

public sealed class ContentValidator :
    IValidator
{
    private readonly IContentCatalog _catalog;
    private readonly IContentLoaderRegistry _loaders;

    public ContentValidator(
        IContentCatalog catalog,
        IContentLoaderRegistry loaders)
    {
        ArgumentNullException.ThrowIfNull(
            catalog);

        ArgumentNullException.ThrowIfNull(
            loaders);

        _catalog = catalog;
        _loaders = loaders;
    }

    public ValidationResult Validate(
        ValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(
            context);

        var result =
            new ValidationResult();

        foreach (var asset in _catalog.GetAssets())
        {
            var loaders =
                _loaders.FindLoaders(
                    asset);

            if (loaders.Count == 0)
            {
                result.Add(
                    ValidationSeverity.Warning,
                    "CONTENT_NO_LOADER",
                    $"No content loader is registered for '{asset.Path}'.");
                continue;
            }

            var duplicateTypes =
                loaders
                    .GroupBy(
                        loader => loader.AssetType)
                    .Where(
                        group => group.Count() > 1);

            foreach (var group in duplicateTypes)
            {
                result.Add(
                    ValidationSeverity.Error,
                    "CONTENT_AMBIGUOUS_LOADER",
                    $"Multiple content loaders can load '{asset.Path}' as '{group.Key.Name}'.");
            }
        }

        return result;
    }
}