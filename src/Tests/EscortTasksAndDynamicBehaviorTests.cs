using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Generator.Mission.Objectives;
using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS.Tests;

[Collection("Database collection")]
public class EscortTasksAndDynamicBehaviorTests
{
    private readonly DatabaseFixture fixture;

    public EscortTasksAndDynamicBehaviorTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public void EnumerateObjectiveIndexesMatchesFlatGeneratorOrder()
    {
        var obj0 = new MissionTemplateObjectiveRecord(new MissionTemplateObjective(fixture.Db) { SubTasks = [new MissionTemplateSubTask(fixture.Db)] });
        var obj1 = new MissionTemplateObjectiveRecord(new MissionTemplateObjective(fixture.Db) { SubTasks = [new MissionTemplateSubTask(fixture.Db)] });

        var enumerated = ObjectiveUtils.EnumerateObjectiveIndexes([obj0, obj1]).ToList();

        Assert.Equal(
            [(0, 0, (MissionTemplateSubTaskRecord)obj0), (0, 1, obj0.SubTasks[0]), (1, 2, (MissionTemplateSubTaskRecord)obj1), (1, 3, obj1.SubTasks[0])],
            enumerated);
    }

    [Theory]
    [InlineData("EscortPlane", true)]
    [InlineData("EscortHelicopters", true)]
    [InlineData("EscortGroundForces", false)]
    public void DynamicEscortBehaviorLoadsFromPresetIni(string presetId, bool expected)
    {
        var preset = fixture.Db.GetEntry<DBEntryObjectivePreset>(presetId);
        Assert.NotNull(preset);
        Assert.Equal(expected, preset.DynamicEscortBehavior);
    }
}

