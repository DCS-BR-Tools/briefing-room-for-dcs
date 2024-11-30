using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS.Tests;

[Collection("Sequential")]
public class DCSMissionStateTests
{
    [Fact]
    public void RevertOneState()
    {
        new BriefingRoom(nukeDB: true);
        var template = new MissionTemplate($"{BriefingRoom.GetBriefingRoomRootPath()}\\Default.brt");
        var templateRecord = new MissionTemplateRecord(template);
        var mission = new DCSMission("en",templateRecord);
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
        new BriefingRoom(nukeDB: true);
        var template = new MissionTemplate($"{BriefingRoom.GetBriefingRoomRootPath()}\\Default.brt");
        var templateRecord = new MissionTemplateRecord(template);
        var mission = new DCSMission("en", templateRecord);
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