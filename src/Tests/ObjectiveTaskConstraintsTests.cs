using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Generator.Mission.Objectives;
using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS.Tests;

[Collection("Database collection")]
public class ObjectiveTaskConstraintsTests
{
    private readonly DatabaseFixture fixture;

    public ObjectiveTaskConstraintsTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public void ObjectiveNullCheck_ThrowsBriefingRoomException_WhenEntriesAreNull()
    {
        var target = fixture.Db.GetEntry<DBEntryObjectiveTarget>("AirDefenseShortRangeAAA");
        var behavior = fixture.Db.GetEntry<DBEntryObjectiveTargetBehavior>("Idle");
        var task = fixture.Db.GetEntry<DBEntryObjectiveTask>("DestroyAll");

        Assert.NotNull(target);
        Assert.NotNull(behavior);
        Assert.NotNull(task);

        var targetEx = Assert.Throws<BriefingRoomException>(() =>
            ObjectiveUtils.ObjectiveNullCheck(fixture.Db, "en", null, behavior, task, targetId: "MissingTarget"));
        Assert.Contains("MissingTarget", targetEx.Message);

        var behaviorEx = Assert.Throws<BriefingRoomException>(() =>
            ObjectiveUtils.ObjectiveNullCheck(fixture.Db, "en", target, null, task, targetBehaviorId: "MissingBehavior"));
        Assert.Contains("MissingBehavior", behaviorEx.Message);

        var taskEx = Assert.Throws<BriefingRoomException>(() =>
            ObjectiveUtils.ObjectiveNullCheck(fixture.Db, "en", target, behavior, null));
        Assert.Contains("unknown", taskEx.Message);
    }

    [Fact]
    public void ObjectiveNullCheck_ThrowsTargetCategoryMismatch_WhenCategoryNotAllowed()
    {
        var target = fixture.Db.GetEntry<DBEntryObjectiveTarget>("PlaneAny");
        var behavior = fixture.Db.GetEntry<DBEntryObjectiveTargetBehavior>("Idle");
        var task = LoadTaskFromIni(fixture.Db, "TestGroundTask", """
[GUI]
DisplayName=Test Ground Task

[Briefing]
Description=Destroy
Task.Singular=Destroy $UNITFAMILY$ at $OBJECTIVENAME$.
Task.Plural=Destroy $UNITFAMILY$ at $OBJECTIVENAME$.

[ObjectiveTask]
CompletionTriggersLua=DestroyAll
TargetSide=Enemy
ValidUnitCategories=Vehicle
""");

        var ex = Assert.Throws<BriefingRoomException>(() =>
            ObjectiveUtils.ObjectiveNullCheck(fixture.Db, "en", target, behavior, task));

        Assert.Contains(target.UIDisplayName.Get("en"), ex.Message);
        Assert.Contains(task.UIDisplayName.Get("en"), ex.Message);
        Assert.Contains("Vehicle", ex.Message);
    }

    [Fact]
    public void ObjectiveNullCheck_ThrowsTargetNotAllowedForTask_WithResolvedDisplayNames()
    {
        var allowedTarget = fixture.Db.GetEntry<DBEntryObjectiveTarget>("PlaneAny");
        var disallowedTarget = fixture.Db.GetEntry<DBEntryObjectiveTarget>("PlaneFighter");
        var behavior = fixture.Db.GetEntry<DBEntryObjectiveTargetBehavior>("Idle");
        var task = LoadTaskFromIni(fixture.Db, "TestPlaneSpecificTask", """
[GUI]
DisplayName=Test Plane Task

[Briefing]
Description=Destroy
Task.Singular=Destroy $UNITFAMILY$ at $OBJECTIVENAME$.
Task.Plural=Destroy $UNITFAMILY$ at $OBJECTIVENAME$.

[ObjectiveTask]
CompletionTriggersLua=DestroyAll
TargetSide=Enemy
ValidUnitCategories=Plane
ValidTargetIDs=PlaneAny
""");

        ObjectiveUtils.ObjectiveNullCheck(fixture.Db, "en", allowedTarget, behavior, task);

        var ex = Assert.Throws<BriefingRoomException>(() =>
            ObjectiveUtils.ObjectiveNullCheck(fixture.Db, "en", disallowedTarget, behavior, task));

        Assert.Contains(disallowedTarget.UIDisplayName.Get("en"), ex.Message);
        Assert.Contains(task.UIDisplayName.Get("en"), ex.Message);
        Assert.Contains(allowedTarget.UIDisplayName.Get("en"), ex.Message);
        Assert.DoesNotContain("PlaneAny", ex.Message);
    }

    [Fact]
    public void GetUnsuitabilityReason_EnforcesValidPlayerUnitCategories()
    {
        var db = new OverrideDatabase(fixture.Db);

        var heliOnlyTask = LoadTaskFromIni(db, "TestHeliOnlyTask", """
[GUI]
DisplayName=Test Heli Task

[Briefing]
Description=Destroy
Task.Singular=Destroy $UNITFAMILY$ at $OBJECTIVENAME$.
Task.Plural=Destroy $UNITFAMILY$ at $OBJECTIVENAME$.

[ObjectiveTask]
CompletionTriggersLua=DestroyAll
TargetSide=Enemy
ValidPlayerUnitCategories=Helicopter
""");
        db.AddOverride(heliOnlyTask);

        var planeOnlyTask = LoadTaskFromIni(db, "TestPlaneOnlyTask", """
[GUI]
DisplayName=Test Plane Task

[Briefing]
Description=Destroy
Task.Singular=Destroy $UNITFAMILY$ at $OBJECTIVENAME$.
Task.Plural=Destroy $UNITFAMILY$ at $OBJECTIVENAME$.

[ObjectiveTask]
CompletionTriggersLua=DestroyAll
TargetSide=Enemy
ValidPlayerUnitCategories=Plane
""");
        db.AddOverride(planeOnlyTask);

        var heliPreset = LoadPresetFromIni(db, "TestHeliPreset", $"""
[GUI]
DisplayName=Test Heli Preset
Category=Air-To-Ground

[ObjectivePreset]
Targets=AirDefenseShortRangeAAA
TargetsBehaviors=Idle
Task={heliOnlyTask.ID}
""");
        db.AddOverride(heliPreset);

        var planePreset = LoadPresetFromIni(db, "TestPlanePreset", $"""
[GUI]
DisplayName=Test Plane Preset
Category=Air-To-Ground

[ObjectivePreset]
Targets=AirDefenseShortRangeAAA
TargetsBehaviors=Idle
Task={planeOnlyTask.ID}
""");
        db.AddOverride(planePreset);

        var fixedWingOnly = new ObjectivePresetSuitabilityProfile(true, false, false);
        var rotorOnly = new ObjectivePresetSuitabilityProfile(false, true, false);
        var mixed = new ObjectivePresetSuitabilityProfile(true, true, false);

        Assert.Equal(ObjectivePresetUnsuitabilityReason.AircraftTypeMismatch,
            ObjectivePresetSuitability.GetUnsuitabilityReason(db, heliPreset.ID, fixedWingOnly));
        Assert.Equal(ObjectivePresetUnsuitabilityReason.None,
            ObjectivePresetSuitability.GetUnsuitabilityReason(db, heliPreset.ID, rotorOnly));
        Assert.Equal(ObjectivePresetUnsuitabilityReason.None,
            ObjectivePresetSuitability.GetUnsuitabilityReason(db, heliPreset.ID, mixed));

        Assert.Equal(ObjectivePresetUnsuitabilityReason.AircraftTypeMismatch,
            ObjectivePresetSuitability.GetUnsuitabilityReason(db, planePreset.ID, rotorOnly));
        Assert.Equal(ObjectivePresetUnsuitabilityReason.None,
            ObjectivePresetSuitability.GetUnsuitabilityReason(db, planePreset.ID, fixedWingOnly));
    }

    private DBEntryObjectiveTask LoadTaskFromIni(IDatabase database, string idPrefix, string iniText)
    {
        return LoadEntryFromIni<DBEntryObjectiveTask>(database, idPrefix, iniText);
    }

    private DBEntryObjectivePreset LoadPresetFromIni(IDatabase database, string idPrefix, string iniText)
    {
        return LoadEntryFromIni<DBEntryObjectivePreset>(database, idPrefix, iniText);
    }

    private T LoadEntryFromIni<T>(IDatabase database, string idPrefix, string iniText) where T : DBEntry, new()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"br-test-{Guid.NewGuid():N}.ini");
        try
        {
            File.WriteAllText(tempFile, iniText);
            var entry = new T();
            var loaded = entry.Load(database, $"{idPrefix}{Guid.NewGuid():N}", tempFile);
            Assert.True(loaded);
            return entry;
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    private sealed class OverrideDatabase : IDatabase
    {
        private readonly IDatabase inner;
        private readonly Dictionary<(Type, string), DBEntry> overrides = new();

        public OverrideDatabase(IDatabase inner)
        {
            this.inner = inner;
        }

        public void AddOverride<T>(T entry) where T : DBEntry
        {
            overrides[(typeof(T), entry.ID)] = entry;
        }

        public DatabaseCommon Common { get => inner.Common; set => inner.Common = value; }
        public DatabaseLanguage Language { get => inner.Language; set => inner.Language = value; }
        public string DCSSaveGamePath => inner.DCSSaveGamePath;

        public void Reset() => inner.Reset();
        public bool EntryExists<T>(string id) where T : DBEntry, new() => overrides.ContainsKey((typeof(T), id)) || inner.EntryExists<T>(id);
        public string[] GetAllEntriesIDs<T>() where T : DBEntry, new() => inner.GetAllEntriesIDs<T>();
        public T[] GetAllEntries<T>() where T : DBEntry, new() => inner.GetAllEntries<T>();
        public ST[] GetAllEntries<T, ST>() where T : DBEntry, new() where ST : DBEntry => inner.GetAllEntries<T, ST>();
        public T GetEntry<T>(string id) where T : DBEntry, new() => overrides.TryGetValue((typeof(T), id), out var entry) ? (T)entry : inner.GetEntry<T>(id);
        public ST GetEntry<T, ST>(string id) where T : DBEntry, new() where ST : DBEntry => inner.GetEntry<T, ST>(id);
        public T[] GetEntries<T>(params string[] ids) where T : DBEntry, new() => inner.GetEntries<T>(ids);
        public List<T> GetEntries<T>(List<string> ids) where T : DBEntry, new() => inner.GetEntries<T>(ids);
        public string CheckID<T>(string id, string? defaultID = null, bool allowEmptyStr = false, List<string>? allowedValues = null) where T : DBEntry, new() =>
            overrides.ContainsKey((typeof(T), id)) ? id : inner.CheckID<T>(id, defaultID, allowEmptyStr, allowedValues);
        public string[] CheckIDs<T>(params string[] ids) where T : DBEntry, new() => inner.CheckIDs<T>(ids);
        public bool SetDCSSaveGamePath(string path) => inner.SetDCSSaveGamePath(path);
    }
}
