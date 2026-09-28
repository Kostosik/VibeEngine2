using Engine.Localization;
using Xunit;

namespace Engine.Tests.Localization;

public sealed class LocalizationManagerTests
{
    [Fact]
    public void Get_UsesCurrentLocale()
    {
        var manager =
            new LocalizationManager(
                new Locale("en-US"));

        var table =
            new LocalizationTable(
                new Locale("en-US"));

        table.Set(
            "menu.play",
            "Play");

        manager.AddTable(
            table);

        Assert.Equal(
            "Play",
            manager.Get("menu.play"));
    }

    [Fact]
    public void Get_UsesFallbackLocale()
    {
        var manager =
            new LocalizationManager(
                new Locale("ru-RU"));

        manager.SetFallbackLocale(
            new Locale("en-US"));

        var table =
            new LocalizationTable(
                new Locale("en-US"));

        table.Set(
            "menu.play",
            "Play");

        manager.AddTable(
            table);

        Assert.Equal(
            "Play",
            manager.Get("menu.play"));
    }

    [Fact]
    public void Get_ReturnsKeyWhenTranslationIsMissing()
    {
        var manager =
            new LocalizationManager(
                new Locale("en-US"));

        Assert.Equal(
            "menu.missing",
            manager.Get("menu.missing"));
    }

    [Fact]
    public void Format_FormatsLocalizedText()
    {
        var manager =
            new LocalizationManager(
                new Locale("en-US"));

        var table =
            new LocalizationTable(
                new Locale("en-US"));

        table.Set(
            "welcome",
            "Hello, {0}!");

        manager.AddTable(
            table);

        Assert.Equal(
            "Hello, Player!",
            manager.Format(
                "welcome",
                "Player"));
    }

    [Fact]
    public void Get_UsesCurrentLanguageWhenRegionalLocaleIsMissing()
    {
        var manager =
            new LocalizationManager(
                new Locale("ru-RU"));

        var table =
            new LocalizationTable(
                new Locale("ru"));

        table.Set(
            "menu.play",
            "Играть");

        manager.AddTable(
            table);

        Assert.Equal(
            "Играть",
            manager.Get("menu.play"));
    }

    [Fact]
    public void Get_PrefersFallbackExactLocaleOverFallbackLanguage()
    {
        var manager =
            new LocalizationManager(
                new Locale("ru-RU"));

        manager.SetFallbackLocale(
            new Locale("en-US"));

        var fallbackLanguage =
            new LocalizationTable(
                new Locale("en"));

        fallbackLanguage.Set(
            "menu.play",
            "Play");

        var fallbackExact =
            new LocalizationTable(
                new Locale("en-US"));

        fallbackExact.Set(
            "menu.play",
            "Play Now");

        manager.AddTable(
            fallbackLanguage);

        manager.AddTable(
            fallbackExact);

        Assert.Equal(
            "Play Now",
            manager.Get("menu.play"));
    }
}