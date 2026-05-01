using System.IO;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS.Tests;

// When a template's RandomSeed is set, two consecutive generations should produce
// identical missions modulo the bits we don't currently seed (FluentRandomPicker
// briefing description ordering). The asserts here cover the deterministic surface:
// theater, briefing name, generated unit group names per coalition, and the
// objectives center the rest of the pipeline keys off.
[Collection("Database collection")]
public class DeterminismTests
{
    DatabaseFixture fixture;

    public DeterminismTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public void SameSeedYieldsIdenticalMission()
    {
        const int seed = 4242;

        var first = GenerateWithSeed(seed);
        var second = GenerateWithSeed(seed);

        Assert.Equal(first.TheaterID, second.TheaterID);
        Assert.Equal(first.Briefing.Name, second.Briefing.Name);
        Assert.Equal(first.GetValue("CountriesBlue"), second.GetValue("CountriesBlue"));
        Assert.Equal(first.GetValue("CountriesRed"), second.GetValue("CountriesRed"));
        Assert.Equal(first.GetValue("CountriesNeutral"), second.GetValue("CountriesNeutral"));
        // Briefing description routes through Toolbox.RandomWeightedFrom now (FluentRandomPicker is gone),
        // so it should be reproducible too.
        Assert.Equal(first.GetValue("DescriptionText"), second.GetValue("DescriptionText"));
    }

    [Fact]
    public void DifferentSeedsYieldDifferentMissions()
    {
        var withSeedA = GenerateWithSeed(1);
        var withSeedB = GenerateWithSeed(2);

        // Two different seeds should diverge somewhere in the unit Lua tables. We
        // check at least one of the three coalition tables differs to guard against
        // a degenerate seeding path that always lands on the same outputs.
        var differs =
            withSeedA.GetValue("CountriesBlue") != withSeedB.GetValue("CountriesBlue") ||
            withSeedA.GetValue("CountriesRed") != withSeedB.GetValue("CountriesRed") ||
            withSeedA.GetValue("CountriesNeutral") != withSeedB.GetValue("CountriesNeutral");
        Assert.True(differs, "Two different seeds produced identical unit tables; seeding likely isn't reaching the RNG.");
    }

    [Fact]
    public void RandomSeedRoundTripsThroughIni()
    {
        var template = new MissionTemplate(
            fixture.Db,
            $"{BriefingRoom.GetBriefingRoomRootPath()}\\testTemplates\\Caucasus.brt");
        template.RandomSeed = 9931;

        var path = Path.Combine(Path.GetTempPath(), $"br-seed-roundtrip-{System.Guid.NewGuid():N}.brt");
        try
        {
            template.SaveToFile(path);
            var reloaded = new MissionTemplate(fixture.Db, path);
            Assert.Equal(9931, reloaded.RandomSeed);

            // And that an unset seed survives the round trip as null, not 0.
            template.RandomSeed = null;
            template.SaveToFile(path);
            var reloadedUnset = new MissionTemplate(fixture.Db, path);
            Assert.Null(reloadedUnset.RandomSeed);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    DCSMission GenerateWithSeed(int seed)
    {
        var briefingRoom = new BriefingRoom(fixture.Db);
        var template = new MissionTemplate(
            fixture.Db,
            $"{BriefingRoom.GetBriefingRoomRootPath()}\\testTemplates\\Caucasus.brt");
        template.RandomSeed = seed;
        return briefingRoom.GenerateMission(template);
    }
}
