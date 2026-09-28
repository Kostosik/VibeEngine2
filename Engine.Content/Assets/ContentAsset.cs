using Engine.Core.Assets;

namespace Engine.Content.Assets;

public sealed record ContentAsset(
    AssetPath Path,
    string Extension,
    long SizeBytes,
    DateTime LastModifiedUtc);