using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS.Tests;

[Collection("Database collection")]
public class DCSMissionStateTests
{
    DatabaseFixture fixture;

    public DCSMissionStateTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }
    [Fact]
    public void RevertOneState()
    {
        var br = new BriefingRoom(fixture.Db);
        var template = new MissionTemplate(fixture.Db, $"{BriefingRoom.GetBriefingRoomRootPath()}\\Default.brt");
        var templateRecord = new MissionTemplateRecord(fixture.Db, template);
        var mission = new DCSMission(fixture.Db, "en", templateRecord);
        mission.SaveStage(MissionStageName.Initialization);

        mission.GroupID++;
        mission.UnitID++;
        mission.MapData.Add("A", new List<double[]>());

        Assert.NotNull(mission);
        Assert.Equal(2, mission.GroupID);
        Assert.Equal(2, mission.UnitID);
        Assert.Single(mission.MapData);

        mission.RevertStage(1);

        Assert.Equal(1, mission.GroupID);
        Assert.Equal(1, mission.UnitID);
        Assert.Empty(mission.MapData);
    }


    [Fact]
    public void RevertTwoStates()
    {
        var br = new BriefingRoom(fixture.Db);
        var template = new MissionTemplate(fixture.Db, $"{BriefingRoom.GetBriefingRoomRootPath()}\\Default.brt");
        var templateRecord = new MissionTemplateRecord(fixture.Db, template);
        var mission = new DCSMission(fixture.Db, "en", templateRecord);
        mission.SaveStage(MissionStageName.Initialization);

        mission.GroupID++;
        mission.UnitID++;
        mission.MapData.Add("A", new List<double[]>());

        mission.SaveStage(MissionStageName.Situation);

        mission.GroupID++;
        mission.UnitID++;
        mission.MapData.Add("B", new List<double[]>());


        Assert.NotNull(mission);
        Assert.Equal(3, mission.GroupID);
        Assert.Equal(3, mission.UnitID);
        Assert.Equal(2, mission.MapData.Count);

        mission.RevertStage(2);

        Assert.Equal(1, mission.GroupID);
        Assert.Equal(1, mission.UnitID);
        Assert.Empty(mission.MapData);
    }

}