using BriefingRoom4DCS.Mission;

namespace BriefingRoom4DCS.Tests;

[Collection("Sequential")]
public class SmokeTests
{
    [Fact]
    public void SingleMission()
    {
        new BriefingRoom(nukeDB: true);
        DCSMission mission = BriefingRoom.GenerateMission($"{BriefingRoom.GetBriefingRoomRootPath()}\\Default.brt");

        Assert.NotNull(mission);
        Assert.Equal("Caucasus", mission.TheaterID);
        Assert.NotNull(mission.Briefing.Name);
        Assert.NotNull(mission.Briefing.Description);
    }

    [Fact]
    public void Campaign()
    {
        new BriefingRoom(nukeDB: true);
        DCSCampaign campaign = BriefingRoom.GenerateCampaign($"{BriefingRoom.GetBriefingRoomRootPath()}\\Default.cbrt");

        Assert.NotNull(campaign);
        Assert.Equal(5, campaign.MissionCount);
        Assert.NotNull(campaign.Name);


        DCSMission mission = campaign.Missions.First();
        Assert.NotNull(mission);
        Assert.Equal("Caucasus", mission.TheaterID);
        Assert.NotNull(mission.Briefing.Name);
        Assert.NotNull(mission.Briefing.Description);
    }
}