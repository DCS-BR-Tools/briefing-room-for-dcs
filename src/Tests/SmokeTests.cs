using BriefingRoom4DCS.Mission;

namespace BriefingRoom4DCS.Tests;

[Collection("Sequential")]
public class SmokeTests
{
    [Theory]
    [InlineData("Afghanistan")]
    [InlineData("Caucasus")]
    [InlineData("Falklands")]
    [InlineData("GermanyCW")]
    [InlineData("Iraq")]
    [InlineData("Kola")]
    [InlineData("MarianaIslands")]
    [InlineData("MarianaIslandsWWII")]
    [InlineData("Nevada")]
    [InlineData("Normandy")]
    [InlineData("PersianGulf")]
    [InlineData("SinaiMap")]
    [InlineData("Syria")]
    [InlineData("TheChannel")]
    public void SingleMission(string theaterID)
    {
        var briefingRoom = new BriefingRoom();

        DCSMission mission = briefingRoom.GenerateMission($"{BriefingRoom.GetBriefingRoomRootPath()}\\testTemplates\\{theaterID}.brt");

        Assert.NotNull(mission);
        Assert.Equal(theaterID, mission.TheaterID);
        Assert.NotNull(mission.Briefing.Name);
        Assert.NotNull(mission.Briefing.Description);
    }

    [Fact]
    public void Campaign()
    {
        var briefingRoom = new BriefingRoom();
        
        DCSCampaign campaign = briefingRoom.GenerateCampaign($"{BriefingRoom.GetBriefingRoomRootPath()}\\testTemplates\\Test.cbrt");

        Assert.NotNull(campaign);
        Assert.Equal(3, campaign.MissionCount);
        Assert.NotNull(campaign.Name);


        DCSMission mission = campaign.Missions.First();
        Assert.NotNull(mission);
        Assert.Equal("Caucasus", mission.TheaterID);
        Assert.NotNull(mission.Briefing.Name);
        Assert.NotNull(mission.Briefing.Description);
    }
}