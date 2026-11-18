namespace BriefingRoom4DCS.Tests;

[Collection("Database collection")]
public class LanguageKeyTests
{
    private readonly DatabaseFixture fixture;

    public LanguageKeyTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    internal static void AssertKeysInAllLanguages(DatabaseFixture fixture, params string[] keys)
    {
        var language = fixture.Db.Language;
        var languageCodes = language.AvailableLanguagesMap.Keys.Select(x => x.ToLowerInvariant()).ToList();
        Assert.NotEmpty(languageCodes);

        foreach (var key in keys)
        {
            Assert.True(language.LangMap.ContainsKey(key.ToUpperInvariant()), $"Missing translation key \"{key}\"");
            var translations = language.LangMap[key.ToUpperInvariant()];
            foreach (var languageCode in languageCodes)
                Assert.True(translations.ContainsKey(languageCode), $"Translation key \"{key}\" missing for language \"{languageCode}\"");
        }
    }

    [Fact]
    public void ObjectiveTaskConstraintErrorKeysExistInAllLanguages()
    {
        AssertKeysInAllLanguages(fixture, "TargetCategoryMismatch", "TargetNotAllowedForTask");
    }

    [Theory]
    [InlineData("TargetCategoryMismatch", 4)]
    [InlineData("TargetNotAllowedForTask", 3)]
    public void ObjectiveTaskConstraintErrorKeysFormatInAllLanguages(string key, int argumentCount)
    {
        var args = Enumerable.Range(0, argumentCount).Select(x => (object)$"ARG{x}").ToArray();
        foreach (var languageCode in fixture.Db.Language.AvailableLanguagesMap.Keys.Select(x => x.ToLowerInvariant()))
        {
            var message = fixture.Db.Language.Translate(languageCode, key, args);
            Assert.DoesNotContain("ERR_Missing_Translation_key", message);
            foreach (var arg in args)
                Assert.Contains((string)arg, message);
        }
    }
}

