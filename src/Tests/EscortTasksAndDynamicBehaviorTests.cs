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

    [Theory]
    [InlineData("ToFrontLineFromPlayerAirbase", "A-10C_2", "AttackObjective")]
    [InlineData("ToFrontLineFromAirbase", "A-10C_2", "AttackObjectiveFromAirbase")]
    [InlineData("ParkedOnAirBase", "A-10C_2", "AttackObjectiveFromAirbase")]
    [InlineData("ToFrontLine", "A-10C_2", "AttackObjectiveFromAir")]
    [InlineData("Patrolling", "A-10C_2", "AttackObjectiveFromAir")]
    [InlineData("ToFrontLineFromAirbase", "C-130", "InsertAtObjectiveFromAirbase")]
    [InlineData("ToFrontLineFromPlayerAirbase", "C-130", "InsertAtObjective")]
    [InlineData("ToFrontLine", "C-130", "InsertAtObjectiveFromAir")]
    public void ResolveDynamicEscortBehaviorPreservesOriginType(
        string sourceBehaviorId,
        string aircraftId,
        string expectedBehaviorId)
    {
        var sourceBehavior = fixture.Db.GetEntry<DBEntryObjectiveTargetBehavior>(sourceBehaviorId);
        var aircraft = fixture.Db.GetEntry<DBEntryJSONUnit, DBEntryAircraft>(aircraftId);
        Assert.NotNull(sourceBehavior);
        Assert.NotNull(aircraft);

        var resolved = Escort.ResolveDynamicEscortBehaviorId(sourceBehavior.Location, aircraft);

        Assert.Equal(expectedBehaviorId, resolved);
    }

    [Fact]
    public void EscortHVAPresetUsesMovingDestinationBehaviors()
    {
        var preset = fixture.Db.GetEntry<DBEntryObjectivePreset>("EscortHVA");
        Assert.NotNull(preset);
        Assert.NotEmpty(preset.TargetsBehaviors);
        Assert.DoesNotContain("Patrolling", preset.TargetsBehaviors);
        Assert.DoesNotContain("IdleCAP", preset.TargetsBehaviors);

        foreach (var behaviorId in preset.TargetsBehaviors)
        {
            var behavior = fixture.Db.GetEntry<DBEntryObjectiveTargetBehavior>(behaviorId);
            Assert.NotNull(behavior);
            Assert.False(behavior.IsStatic);
            Assert.DoesNotContain("Escort", behavior.InvalidTasks);
            Assert.DoesNotContain("EscortHVA", behavior.InvalidTasks);
        }
    }
}

