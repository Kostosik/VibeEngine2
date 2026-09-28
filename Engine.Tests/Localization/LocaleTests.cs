using Engine.Localization;
using Xunit;

namespace Engine.Tests.Localization;

public sealed class LocaleTests
{
    [Fact]
    public void Locale_NormalizesLanguageAndRegion()
    {
        var locale =
            new Locale(
                "ru_ru");

        Assert.Equal(
            "ru",
            locale.Language);

        Assert.Equal(
            "RU",
            locale.Region);

        Assert.Equal(
            "ru-RU",
            locale.Code);
    }

    [Fact]
    public void Locale_WithLanguageOnly_HasNoRegion()
    {
        var locale =
            new Locale(
                "en");

        Assert.Equal(
            "en",
            locale.Code);

        Assert.Null(
            locale.Region);
    }
}