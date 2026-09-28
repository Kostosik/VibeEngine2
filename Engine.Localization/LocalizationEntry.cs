namespace Engine.Localization;

public readonly record struct LocalizationEntry(
    LocalizationKey Key,
    string Text);