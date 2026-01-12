# CreateObjective Deep Dive  (AI Created)

## Overview

The `CreateObjective` method is the core function responsible for generating individual mission objectives in Briefing Room for DCS World. This document provides a comprehensive analysis of how objectives are created, including the dispatcher mechanism, specialized objective types, and the complete data flow.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Entry Point and Dispatcher](#entry-point-and-dispatcher)
3. [Objective Generation Flow](#objective-generation-flow)
4. [Objective Type Implementations](#objective-type-implementations)
5. [Supporting Components](#supporting-components)
6. [Data Structures](#data-structures)
7. [Lua Integration](#lua-integration)

---

## Architecture Overview

### Component Hierarchy

```
ObjectiveGenerator
    ├── GenerateObjective (public entry)
    ├── CreateObjective (private dispatcher)
    └── Specialized Implementations
        ├── Basic.CreateObjective (default/fallback)
        ├── Escort.CreateObjective
        ├── Hold.CreateObjective
        ├── Transport.CreateObjective
        └── TransportDynamicCargo.CreateObjective
```

### Design Pattern

The system uses a **Strategy Pattern** where:
- `ObjectiveGenerator.CreateObjective()` acts as the dispatcher
- Specialized objective classes implement task-specific logic
- `Basic.CreateObjective()` provides the default implementation
- All implementations share a common method signature

---

## Entry Point and Dispatcher

### GenerateObjective Method

The public entry point that orchestrates the entire objective generation process:

```csharp
internal static Tuple<Coordinates, List<List<Waypoint>>> GenerateObjective(
    IBriefingRoom briefingRoom,
    DCSMission mission,
    MissionTemplateObjectiveRecord task,
    Coordinates lastCoordinates,
    ref int objectiveIndex)
```

**Responsibilities**:

1. **Extract Objective Configuration**
   ```csharp
   var (featuresID, targetDB, targetBehaviorDB, taskDB, objectiveOptions) = 
       GetObjectiveData(briefingRoom.Database, mission.LangKey, task);
   ```
   - Loads task definition (what to do: strike, CAP, escort, etc.)
   - Loads target definition (what to attack: buildings, vehicles, ships, etc.)
   - Loads behavior definition (how targets behave: stationary, patrolling, etc.)
   - Merges preset and custom features

2. **Determine Spawn Coordinates**
   ```csharp
   var useHintCoordinates = task.CoordinatesHint.ToString() != "0,0";
   lastCoordinates = useHintCoordinates ? task.CoordinatesHint : lastCoordinates;
   var objectiveCoordinates = GetSpawnCoordinates(
       briefingRoom.Database, 
       ref mission, 
       lastCoordinates, 
       mission.PlayerAirbase, 
       targetDB, 
       useHintCoordinates
   );
   ```
   - Uses hint coordinates if user specified them
   - Otherwise calculates based on distance from player airbase and previous objective
   - Validates spawn point type matches target requirements (land/sea/air)

3. **Create Main Objective**
   ```csharp
   waypointList.Add(CreateObjective(
       briefingRoom,
       task,
       taskDB,
       targetDB,
       targetBehaviorDB,
       ref objectiveIndex,
       ref objectiveCoordinates,
       objectiveOptions,
       ref mission,
       featuresID
   ));
   ```

4. **Process Sub-Tasks**
   ```csharp
   foreach (var subTasks in task.SubTasks)
   {
       objectiveIndex++;
       waypointList.Add(GenerateSubTask(...));
   }
   ```
   - Sub-tasks are secondary objectives related to the main one
   - Spawned near the main objective
   - Examples: defend a strike target, destroy escorts

5. **Calculate Objectives Center**
   ```csharp
   mission.ObjectivesCenter = 
       Coordinates.Sum(mission.ObjectiveCoordinates) / 
       mission.ObjectiveCoordinates.Count;
   ```

**Returns**: Tuple containing final coordinates and list of waypoint groups

### CreateObjective Dispatcher

The private dispatcher method that routes to specialized implementations:

```csharp
private static List<Waypoint> CreateObjective(
    IBriefingRoom briefingRoom,
    MissionTemplateSubTaskRecord task,
    DBEntryObjectiveTask taskDB,
    DBEntryObjectiveTarget targetDB,
    DBEntryObjectiveTargetBehavior targetBehaviorDB,
    ref int objectiveIndex,
    ref Coordinates objectiveCoords,
    ObjectiveOption[] objectiveOptions,
    ref DCSMission mission,
    string[] featuresID
)
{
    BriefingRoom.PrintToLog($"Generating objective {objectiveIndex} ...");
    return taskDB.ID switch
    {
        "Escort" => Escort.CreateObjective(...),
        "Hold" or "HoldSuperiority" => Hold.CreateObjective(...),
        "TransportTroops" or "TransportCargo" or "ExtractTroops" => Transport.CreateObjective(...),
        "TransportDynamicCargo" => TransportDynamicCargo.CreateObjective(...),
        _ => Basic.CreateObjective(...)  // Default fallback
    };
}
```

**Routing Logic**:
- Examines `taskDB.ID` to determine task type
- Routes to specialized implementation for complex tasks
- Falls back to `Basic.CreateObjective()` for standard objectives
- All specialized implementations have identical signatures

---

## Objective Generation Flow

### Common Steps (All Objective Types)

Regardless of objective type, all implementations follow this general pattern:

#### 1. Extract Unit Configuration

```csharp
var (luaUnit, unitCount, unitCountMinMax, objectiveTargetUnitFamilies, groupFlags) = 
    ObjectiveUtils.GetUnitData(task, targetDB, targetBehaviorDB, objectiveOptions);
```

**Extracts**:
- **luaUnit**: Lua template file for unit type
- **unitCount**: How many units to spawn
- **unitCountMinMax**: Min/max range for unit count
- **objectiveTargetUnitFamilies**: What type of units (tanks, ships, buildings, etc.)
- **groupFlags**: Behavior flags (invisible, hidden, etc.)

**GroupFlags Processing**:
```csharp
GroupFlags groupFlags = 0;
if (objectiveOptions.Contains(ObjectiveOption.Invisible)) 
    groupFlags |= GroupFlags.Invisible;
if (objectiveOptions.Contains(ObjectiveOption.ShowTarget)) 
    groupFlags = GroupFlags.NeverHidden;
else if (objectiveOptions.Contains(ObjectiveOption.HideTarget)) 
    groupFlags = GroupFlags.AlwaysHidden;
if (objectiveOptions.Contains(ObjectiveOption.EmbeddedAirDefense)) 
    groupFlags |= GroupFlags.EmbeddedAirDefense;
```

#### 2. Select Units

**Option A: Template Location** (for static targets like buildings)
```csharp
if (Constants.THEATER_TEMPLATE_LOCATION_MAP.Keys.Any(x => objectiveTargetUnitFamilies.Contains(x)) 
    && targetBehaviorDB.IsStatic)
{
    var locationType = Toolbox.RandomFrom(
        Constants.THEATER_TEMPLATE_LOCATION_MAP.Keys
            .Intersect(objectiveTargetUnitFamilies)
            .Select(x => Constants.THEATER_TEMPLATE_LOCATION_MAP[x])
            .ToList()
    );
    var templateLocation = SpawnPointSelector.GetNearestTemplateLocation(
        ref mission, locationType, objectiveCoordinates, true
    );
    if (templateLocation.HasValue)
    {
        objectiveCoordinates = templateLocation.Value.Coordinates;
        (units, unitDBs) = UnitGenerator.GetUnitsForTemplateLocation(
            briefingRoom, ref mission, templateLocation.Value, 
            taskDB.TargetSide, objectiveTargetUnitFamilies, ref extraSettings
        );
    }
}
```

**Option B: Random Unit Selection**
```csharp
if (units.Count == 0)
    (units, unitDBs) = UnitGenerator.GetUnits(
        briefingRoom, ref mission, objectiveTargetUnitFamilies, 
        unitCount, taskDB.TargetSide, groupFlags, 
        ref extraSettings, targetBehaviorDB.IsStatic
    );
```

**Validation**:
```csharp
if (units.Count == 0 || unitDBs.Count == 0)
    throw new BriefingRoomException(
        briefingRoom.Database, mission.LangKey, 
        "NoUnitsForTimePeriod", taskDB.TargetSide, objectiveTargetUnitFamily
    );
```

#### 3. Handle Airbase Placement

For objectives at airbases (aircraft on ground):

```csharp
if (Constants.AIRBASE_LOCATIONS.Contains(targetBehaviorDB.Location) 
    && targetDB.UnitCategory.IsAircraft())
{
    objectiveCoordinates = ObjectiveUtils.PlaceInAirbase(
        briefingRoom, ref mission, extraSettings, 
        targetBehaviorDB, objectiveCoordinates, unitCount, unitDB
    );
}
```

**PlaceInAirbase Logic**:
1. Finds suitable airbase (enemy coalition, not player's base)
2. Validates sufficient parking spots
3. Assigns parking spot IDs
4. Updates `extraSettings` with airbase and parking info
5. Returns parking spot coordinates

#### 4. Calculate Destination Point

For moving targets:

```csharp
Coordinates destinationPoint = objectiveCoordinates +
    (
        targetDB.UnitCategory switch
        {
            UnitCategory.Plane => Coordinates.CreateRandom(30, 60),
            UnitCategory.Helicopter => Coordinates.CreateRandom(10, 20),
            _ => objectiveTargetUnitFamily == UnitFamily.InfantryMANPADS || 
                 objectiveTargetUnitFamily == UnitFamily.Infantry 
                 ? Coordinates.CreateRandom(1, 5) 
                 : Coordinates.CreateRandom(5, 10)
        } * Toolbox.NM_TO_METERS
    );

if (targetDB.DCSUnitCategory == DCSUnitCategory.Vehicle)
    destinationPoint = ObjectiveUtils.GetNearestSpawnCoordinates(
        briefingRoom.Database, ref mission, destinationPoint, 
        targetDB.ValidSpawnPoints, false
    );
```

**Distance Ranges**:
- **Planes**: 30-60 NM from spawn point
- **Helicopters**: 10-20 NM
- **Infantry**: 1-5 NM
- **Vehicles**: 5-10 NM

#### 5. Configure Group Lua Template

```csharp
var groupLua = targetBehaviorDB.GroupLua[(int)targetDB.DCSUnitCategory];

// Special case: targets attacking player airbase
if (targetBehaviorDB.Location == DBEntryObjectiveTargetBehaviorLocation.PlayerAirbase)
{
    destinationPoint = mission.PlayerAirbase.Coordinates;
    if (objectiveTargetUnitFamily.GetUnitCategory().IsAircraft() 
        && taskDB.TargetSide == Side.Enemy)
    {
        groupLua = objectiveTargetUnitFamily switch
        {
            UnitFamily.PlaneAttack => "AircraftBomb",
            UnitFamily.PlaneBomber => "AircraftBomb",
            UnitFamily.PlaneStrike => "AircraftBomb",
            UnitFamily.PlaneFighter => "AircraftCAP",
            UnitFamily.PlaneInterceptor => "AircraftCAP",
            UnitFamily.HelicopterAttack => "AircraftBomb",
            _ => groupLua
        };
    }
}
```

**Lua Templates** (examples):
- `AircraftBomb`: Strike aircraft with bombing waypoints
- `AircraftCAP`: Fighter patrol pattern
- `AircraftOrbiting`: Circular patrol
- `GroundVehicles`: Ground unit convoy
- `StaticStructures`: Buildings and fortifications

#### 6. Set Movement Waypoints

For non-static groups:

```csharp
if (!targetBehaviorDB.IsStatic)
{
    extraSettings.Add("GroupX2", destinationPoint.X);
    extraSettings.Add("GroupY2", destinationPoint.Y);
}
```

#### 7. Configure Aircraft Spawn

```csharp
if (objectiveTargetUnitFamily.GetUnitCategory().IsAircraft() &&
    !groupFlags.HasFlag(GroupFlags.RadioAircraftSpawn) &&
    !Constants.AIR_ON_GROUND_LOCATIONS.Contains(targetBehaviorDB.Location))
{
    if (task.ProgressionActivation)
        groupFlags |= GroupFlags.ProgressionAircraftSpawn;
    else
        groupFlags |= GroupFlags.ImmediateAircraftSpawn;
}
```

**Spawn Modes**:
- **ImmediateAircraftSpawn**: Airborne from mission start
- **ProgressionAircraftSpawn**: Spawns when progression condition met
- **RadioAircraftSpawn**: Spawns on radio command

#### 8. Spawn Unit Group

```csharp
GroupInfo? targetGroupInfo = UnitGenerator.AddUnitGroup(
    briefingRoom,
    ref mission,
    units,
    taskDB.TargetSide,
    objectiveTargetUnitFamily,
    groupLua, luaUnit,
    unitCoordinates,
    groupFlags,
    extraSettings
);

if (!targetGroupInfo.HasValue)
    throw new BriefingRoomException(
        briefingRoom.Database, mission.LangKey, 
        "FailedToGenerateGroupObjective"
    );
```

**AddUnitGroup** handles:
- Creating DCS group structure
- Assigning unit IDs
- Setting waypoints from template
- Configuring AI behavior
- Adding to mission Lua tables

#### 9. Handle Progression System

For progressive objectives (activate based on conditions):

```csharp
if (task.ProgressionActivation)
{
    targetGroupInfo.Value.DCSGroups.ForEach((grp) =>
    {
        grp.LateActivation = true;
        grp.Visible = task.ProgressionOptions.Contains(
            ObjectiveProgressionOption.PreProgressionSpottable
        );
    });
}
```

**Progression Options**:
- **LateActivation**: Group exists but inactive
- **Visible**: Can be seen on F10 map before activation
- **ProgressionHiddenBrief**: Not mentioned in briefing until active

#### 10. Configure Aircraft Special Settings

```csharp
if (targetDB.UnitCategory.IsAircraft())
    targetGroupInfo.Value.DCSGroup.Waypoints.First().Tasks.Insert(0, 
        new DCSWrappedWaypointTask("SetUnlimitedFuel", 
            new Dictionary<string, object> { { "value", true } }
        )
    );
```

#### 11. Add Embedded Air Defenses

For static targets with air defense option:

```csharp
if (objectiveOptions.Contains(ObjectiveOption.EmbeddedAirDefense) 
    && (targetDB.UnitCategory == UnitCategory.Static))
{
    ObjectiveUtils.AddEmbeddedAirDefenseUnits(
        briefingRoom, ref mission, targetDB, targetBehaviorDB, 
        taskDB, objectiveCoordinates, groupFlags, extraSettings
    );
}
```

Spawns AAA/MANPADS near target to defend it.

#### 12. Enhance Waypoints

```csharp
targetGroupInfo.Value.DCSGroup.Waypoints = 
    targetBehaviorDB.ID.Contains("OnRoad") || targetBehaviorDB.ID.Contains("Idle") 
    ? targetGroupInfo.Value.DCSGroup.Waypoints 
    : DCSWaypoint.CreateExtraWaypoints(
        ref mission, 
        targetGroupInfo.Value.DCSGroup.Waypoints, 
        targetGroupInfo.Value.UnitDB.Families.First()
    );
```

Adds intermediate waypoints for natural movement.

#### 13. Assign Target Suffix

```csharp
var objectiveName = mission.WaypointNameGenerator.GetWaypointName();
var isStatic = objectiveTargetUnitFamily.GetUnitCategory() == UnitCategory.Static 
    || objectiveTargetUnitFamily.GetUnitCategory() == UnitCategory.Cargo;
ObjectiveUtils.AssignTargetSuffix(ref targetGroupInfo, objectiveName, isStatic);
```

Names groups with pattern: `GroupName-TGT-ALPHA` for tracking.

#### 14. Create Task String

```csharp
mission.Briefing.AddItem(DCSMissionBriefingItemType.TargetGroupName, 
    $"-TGT-{objectiveName}");

var length = isStatic 
    ? targetGroupInfo.Value.DCSGroups.Count 
    : targetGroupInfo.Value.UnitNames.Length;
var pluralIndex = length == 1 ? 0 : 1;

var taskString = GeneratorTools.ParseRandomString(
    taskDB.BriefingTask[pluralIndex].Get(mission.LangKey), mission
).Replace("\"", "''");

var unitDisplayName = targetGroupInfo.Value.UnitDB.UIDisplayName;

ObjectiveUtils.CreateTaskString(
    briefingRoom.Database, ref mission, pluralIndex, 
    ref taskString, objectiveName, objectiveTargetUnitFamily, 
    unitDisplayName, task, luaExtraSettings
);
```

Generates briefing text like: "Destroy the T-72 tank column at ALPHA"

#### 15. Create Lua Mission Logic

```csharp
ObjectiveUtils.CreateLua(
    ref mission, targetDB, taskDB, objectiveIndex, 
    objectiveName, targetGroupInfo, taskString, task, luaExtraSettings
);
```

**Creates**:
- Objective tracking table
- F10 menu entries
- Completion trigger scripts
- Unit name lists for scripting

#### 16. Add Briefing Remarks

```csharp
var remarksString = taskDB.BriefingRemarks.Get(mission.LangKey);
if (!string.IsNullOrEmpty(remarksString))
{
    string remark = Toolbox.RandomFrom(remarksString.Split(";"));
    GeneratorTools.ReplaceKey(ref remark, "ObjectiveName", objectiveName);
    GeneratorTools.ReplaceKey(ref remark, "DropOffDistanceMeters", 
        briefingRoom.Database.Common.DropOffDistanceMeters.ToString());
    GeneratorTools.ReplaceKey(ref remark, "UnitFamily", 
        briefingRoom.Database.Common.Names.UnitFamilies[(int)objectiveTargetUnitFamily]
            .Get(mission.LangKey).Split(",")[pluralIndex]);
    mission.Briefing.AddItem(DCSMissionBriefingItemType.Remark, remark);
}
```

Adds tactical hints like: "Use anti-radiation missiles against this target"

#### 17. Include Media Files

```csharp
foreach (string oggFile in taskDB.IncludeOgg)
    mission.AddMediaFile(
        $"{BRPaths.MIZ_RESOURCES_OGG}{oggFile}", 
        Path.Combine(BRPaths.INCLUDE_OGG, oggFile)
    );
```

Adds radio messages or sound effects.

#### 18. Generate Objective Features

```csharp
mission.AppendValue("ScriptObjectivesFeatures", "");
var featureList = taskDB.RequiredFeatures.Concat(featuresID).ToHashSet();

foreach (string featureID in featureList)
    FeaturesObjectives.GenerateMissionFeature(
        briefingRoom, ref mission, featureID, 
        objectiveName, objectiveIndex, targetGroupInfo.Value, 
        taskDB.TargetSide, objectiveOptions, 
        overrideCoords: targetBehaviorDB.ID.StartsWith("ToFrontLine") 
            ? objectiveCoordinates 
            : null
    );
```

**Features** can add:
- JTAC smoke markers
- Artillery support
- Reinforcement triggers
- Special victory conditions

#### 19. Create Waypoints

```csharp
mission.ObjectiveCoordinates.Add(objectiveCoordinates);
var objCoords = objectiveCoordinates;
var furthestWaypoint = targetGroupInfo.Value.DCSGroup.Waypoints.Aggregate(
    objectiveCoordinates, 
    (furthest, x) => objCoords.GetDistanceFrom(x.Coordinates) > 
        objCoords.GetDistanceFrom(furthest) ? x.Coordinates : furthest
);

var waypoint = ObjectiveUtils.GenerateObjectiveWaypoint(
    briefingRoom.Database, ref mission, task, 
    objectiveCoordinates, furthestWaypoint, objectiveName, 
    targetGroupInfo.Value.DCSGroups.Select(x => x.GroupId).ToList(), 
    hiddenMapMarker: task.ProgressionOptions.Contains(
        ObjectiveProgressionOption.ProgressionHiddenBrief
    )
);

mission.Waypoints.Add(waypoint);
objectiveWaypoints.Add(waypoint);
```

#### 20. Add Map Data

```csharp
mission.MapData.Add($"OBJECTIVE_AREA_{objectiveIndex}", 
    new List<double[]> { waypoint.Coordinates.ToArray() });

mission.ObjectiveTargetUnitFamilies.Add(objectiveTargetUnitFamily);

if (!targetGroupInfo.Value.UnitDB.IsAircraft)
    mission.MapData.Add(
        $"UNIT-{targetGroupInfo.Value.UnitDB.Families[0]}-" +
        $"{taskDB.TargetSide}-{targetGroupInfo.Value.GroupID}", 
        new List<double[]> { targetGroupInfo.Value.Coordinates.ToArray() }
    );
```

#### 21. Return Waypoints

```csharp
return objectiveWaypoints;
```

---

## Objective Type Implementations

### Basic (Default Implementation)

**Used For**: Most objective types
- Strike missions
- SEAD missions
- Anti-ship strikes
- Building destruction
- Ground unit destruction

**File**: `Objectives/Basic.cs`

**Special Logic**: None - follows standard flow exactly

**Example Tasks**:
- "Destroy the SAM site at ALPHA"
- "Strike the enemy airfield at BRAVO"
- "Destroy the enemy convoy at CHARLIE"

---

### Escort

**Used For**: Protecting friendly units during movement

**File**: `Objectives/Escort.cs`

**Special Logic**:

1. **Origin and Destination Calculation**
   ```csharp
   var (originAirbase, unitCoordinates) = 
       ObjectiveUtils.GetTransportOrigin(
           briefingRoom, ref mission, targetBehaviorDB.Location, 
           objectiveCoordinates, true, objectiveTargetUnitFamily.GetUnitCategory()
       );
   
   var (airbase, destinationPoint) = 
       ObjectiveUtils.GetTransportDestination(
           briefingRoom, ref mission, targetBehaviorDB.Location, 
           targetBehaviorDB.Destination, unitCoordinates, objectiveCoordinates, 
           task.TransportDistance, originAirbase?.DCSID ?? -1, 
           true, objectiveTargetUnitFamily.GetUnitCategory(), 
           targetBehaviorDB.ID.StartsWith("ToFrontLine")
       );
   ```

2. **Force Radio Spawn for Timing Control**
   ```csharp
   groupFlags |= GroupFlags.RadioAircraftSpawn;
   ```

3. **Hot Start Configuration**
   ```csharp
   if (originAirbase != null)
       extraSettings["HotStart"] = true;
   ```

4. **Landing Behavior**
   ```csharp
   if (airbase != null && objectiveTargetUnitFamily.GetUnitCategory().IsAircraft())
   {
       groupLua = "AircraftLanding";
       // Or bombing run if landing at enemy airbase
   }
   ```

5. **Create Threat Groups**
   ```csharp
   switch (targetDB.UnitCategory)
   {
       case UnitCategory.Plane:
       case UnitCategory.Helicopter:
           CreateThreat(briefingRoom, ref mission, unitCoordinates, 
               objectiveCoordinates, VIPGroupInfo, "CAP");
           break;
       case UnitCategory.Ship:
           if (playerHasPlanes && Toolbox.RollChance(AmountNR.High)) 
               CreateThreat(..., "CAS");
           if (Toolbox.RollChance(AmountNR.Average)) 
               CreateThreat(..., "Helo");
           if (Toolbox.RollChance(AmountNR.Low)) 
               CreateThreat(..., "Ship");
           break;
       default:
           // Ground threats
           break;
   }
   ```

6. **Pickup Waypoint**
   ```csharp
   var cargoWaypoint = ObjectiveUtils.GenerateObjectiveWaypoint(
       briefingRoom.Database, ref mission, task, 
       unitCoordinates, unitCoordinates, $"{objectiveName} Pickup", 
       scriptIgnore: true
   );
   mission.Waypoints.Add(cargoWaypoint);
   objectiveWaypoints.Add(cargoWaypoint);
   ```

7. **End Zone Trigger**
   ```csharp
   var zoneId = ZoneMaker.AddZone(
       ref mission, $"Escort End Zone {objectiveName}", 
       objectiveCoordinates, 
       VIPGroupInfo.Value.UnitDB.IsAircraft 
           ? briefingRoom.Database.Common.DropOffDistanceMeters * 10 
           : briefingRoom.Database.Common.DropOffDistanceMeters
   );
   TriggerMaker.AddEscortEndTrigger(
       ref mission, zoneId, VIPGroupInfo.Value.GroupID, objectiveIndex
   );
   ```

**Example**: "Escort the C-130 transport from ALPHA to BRAVO"

---

### Hold / Hold Superiority

**Used For**: Area defense missions

**File**: `Objectives/Hold.cs`

**Special Logic**:

1. **Transport Setup** (if applicable)
   ```csharp
   if (taskDB.UICategory.ContainsValue("Transport"))
   {
       if (targetBehaviorDB.ID.StartsWith("RelocateToNewPosition"))
       {
           // Find new position for units
           Coordinates? spawnPoint = SpawnPointSelector.GetRandomSpawnPoint(...);
           unitCoordinates = spawnPoint.Value;
       }
       else
       {
           // Get airbase parking spot
           var (_, _, spawnPoints) = SpawnPointSelector.GetAirbaseAndParking(...);
           unitCoordinates = spawnPoints.First();
       }
   }
   ```

2. **Inverse Waypoint Logic**
   ```csharp
   if (targetBehaviorDB.ID.StartsWith("RecoverToBase") || 
       (taskDB.IsEscort() & !targetBehaviorDB.ID.StartsWith("ToFrontLine")))
   {
       (unitCoordinates, objectiveCoordinates) = 
           (objectiveCoordinates, unitCoordinates);
       isInverseTransportWayPoint = true;
   }
   ```

3. **Infantry Embark Task**
   ```csharp
   if (targetDB.UnitCategory == UnitCategory.Infantry && 
       taskDB.UICategory.ContainsValue("Transport"))
   {
       var pos = unitCoordinates.CreateNearRandom(new MinMaxD(5, 50));
       targetGroupInfo.Value.DCSGroup.Waypoints.First().Tasks.Add(
           new DCSWaypointTask("EmbarkToTransport", 
               new Dictionary<string, object>{
                   {"x", pos.X},
                   {"y", pos.Y},
                   {"zoneRadius", briefingRoom.Database.Common.DropOffDistanceMeters}
               }, 
               _auto: false
           )
       );
   }
   ```

4. **Static Threats**
   ```csharp
   // Create opposing force groups in area
   CreateStaticThreats(briefingRoom, ref mission, objectiveCoordinates, 
       threatDistance, threatTypes);
   ```

5. **Patrol Zone**
   ```csharp
   var zoneId = ZoneMaker.AddZone(
       ref mission, $"Hold Zone {objectiveName}", 
       objectiveCoordinates, holdRadius
   );
   ```

**Example**: "Hold the area around ALPHA and prevent enemy reinforcements"

---

### Transport

**Used For**: Moving cargo or troops

**File**: `Objectives/Transport.cs`

**Special Logic**:

1. **Load/Unload Zones**
   ```csharp
   var loadZone = ZoneMaker.AddZone(
       ref mission, $"Load Zone {objectiveName}", 
       loadCoordinates, loadRadius
   );
   var unloadZone = ZoneMaker.AddZone(
       ref mission, $"Unload Zone {objectiveName}", 
       unloadCoordinates, unloadRadius
   );
   ```

2. **Cargo Unit Spawn**
   ```csharp
   // Spawn cargo/troops at pickup location
   GroupInfo? cargoGroupInfo = UnitGenerator.AddUnitGroup(
       briefingRoom, ref mission, cargoUnits, Side.Ally, 
       UnitFamily.Infantry, "Infantry", "Unit", 
       loadCoordinates, GroupFlags.None, cargoSettings
   );
   ```

3. **Transport Task Assignment**
   ```csharp
   // Add tasks to transport helicopter/plane
   targetGroupInfo.Value.DCSGroup.Waypoints[pickupWPIndex].Tasks.Add(
       new DCSWaypointTask("Embark", 
           new Dictionary<string, object> {
               {"groupsForEmbarking", cargoGroupInfo.Value.Name}
           }
       )
   );
   
   targetGroupInfo.Value.DCSGroup.Waypoints[dropoffWPIndex].Tasks.Add(
       new DCSWaypointTask("Disembark", new Dictionary<string, object>())
   );
   ```

**Example**: "Transport troops from ALPHA to BRAVO"

---

### Transport Dynamic Cargo

**Used For**: CTLD-style dynamic cargo operations

**File**: `Objectives/TransportDynamicCargo.cs`

**Special Logic**:

1. **CTLD Zone Creation**
   ```csharp
   // Creates special CTLD zones for pickup/dropoff
   var ctldPickupZone = CTLDMaker.AddCTLDZone(
       ref mission, objectiveName, pickupCoordinates, 
       CTLDZoneType.Pickup
   );
   var ctldDropoffZone = CTLDMaker.AddCTLDZone(
       ref mission, objectiveName, dropoffCoordinates, 
       CTLDZoneType.Dropoff
   );
   ```

2. **Cargo Type Selection**
   ```csharp
   var cargoType = Toolbox.RandomFrom(new[] { 
       "troops", "vehicles", "crates" 
   });
   ```

3. **CTLD Script Integration**
   ```csharp
   mission.AppendValue("ScriptCTLD", 
       $"ctld.createPickupZone('{objectiveName}', " +
       $"ctld.CargoType.{cargoType}, {pickupCoordinates.X}, " +
       $"{pickupCoordinates.Y})\n"
   );
   ```

**Example**: "Use CTLD to transport supplies from ALPHA to BRAVO"

---

## Supporting Components

### ObjectiveUtils Class

**File**: `Objectives/Utils.cs`

#### Key Methods

**GetUnitData**
```csharp
internal static (
    string luaUnit, 
    int unitCount, 
    MinMaxI unitCountMinMax, 
    List<UnitFamily> objectiveTargetUnitFamily, 
    GroupFlags groupFlags
) GetUnitData(
    MissionTemplateSubTaskRecord task, 
    DBEntryObjectiveTarget targetDB, 
    DBEntryObjectiveTargetBehavior targetBehaviorDB, 
    ObjectiveOption[] objectiveOptions
)
```

Extracts and processes unit configuration from database entries.

**PlaceInAirbase**
```csharp
internal static Coordinates PlaceInAirbase(
    IBriefingRoom briefingRoom, 
    ref DCSMission mission, 
    Dictionary<string, object> extraSettings, 
    DBEntryObjectiveTargetBehavior targetBehaviorDB, 
    Coordinates objectiveCoordinates, 
    int unitCount, 
    DBEntryJSONUnit unitDB, 
    bool Friendly = false
)
```

Places aircraft at an airbase:
1. Finds suitable airbase (coalition, distance)
2. Checks parking spot availability
3. Assigns parking spots
4. Returns parking coordinates

**AddEmbeddedAirDefenseUnits**
```csharp
internal static void AddEmbeddedAirDefenseUnits(
    IBriefingRoom briefingRoom, 
    ref DCSMission mission, 
    DBEntryObjectiveTarget targetDB, 
    DBEntryObjectiveTargetBehavior targetBehaviorDB, 
    DBEntryObjectiveTask taskDB, 
    Coordinates objectiveCoordinates, 
    GroupFlags groupFlags, 
    Dictionary<string, object> extraSettings
)
```

Spawns AAA/MANPADS near static targets for defense.

**CreateLua**
```csharp
internal static void CreateLua(
    ref DCSMission mission, 
    DBEntryObjectiveTarget targetDB, 
    DBEntryObjectiveTask taskDB, 
    int objectiveIndex, 
    string objectiveName, 
    GroupInfo? targetGroupInfo, 
    string taskString, 
    MissionTemplateSubTaskRecord task, 
    Dictionary<string, object> extraSettings
)
```

Generates Lua tables and scripts for objective:

**Creates**:
```lua
briefingRoom.mission.objectives[1] = {
    complete = false,
    failed = false,
    groupName = "TargetGroup-TGT-ALPHA",
    hideTargetCount = false,
    name = "ALPHA",
    targetCategory = Unit.Category.GROUND_UNIT,
    taskType = "Destroy",
    task = "Destroy the T-72 tanks at ALPHA",
    unitsCount = 4,
    unitNames = {"T-72-1", "T-72-2", "T-72-3", "T-72-4"},
    progressionHidden = false,
    progressionHiddenBrief = false,
    progressionCondition = "",
    startMinutes = 0,
    f10MenuText = "$LANG_OBJECTIVE$ ALPHA",
    f10Commands = {}
}
```

**CreateTaskString**
```csharp
internal static void CreateTaskString(
    IDatabase database, 
    ref DCSMission mission, 
    int pluralIndex, 
    ref string taskString, 
    string objectiveName, 
    UnitFamily objectiveTargetUnitFamily, 
    LanguageString unitDisplayName, 
    MissionTemplateSubTaskRecord task, 
    Dictionary<string, object> extraSettings
)
```

Creates human-readable briefing text with replacements:
- `$OBJECTIVENAME$` → "ALPHA"
- `$UNITFAMILY$` → "tanks"
- `$UNITDISPLAYNAME$` → "T-72"

**GenerateObjectiveWaypoint**
```csharp
internal static Waypoint GenerateObjectiveWaypoint(
    IDatabase database, 
    ref DCSMission mission, 
    MissionTemplateSubTaskRecord objectiveTemplate, 
    Coordinates objectiveCoordinates, 
    Coordinates ObjectiveDestinationCoordinates, 
    string objectiveName, 
    List<int> groupIds = null, 
    bool scriptIgnore = false, 
    bool hiddenMapMarker = false
)
```

Creates waypoint object with:
- Name (ALPHA, BRAVO, etc.)
- Coordinates
- Ground/air altitude
- Associated group IDs
- Map marker visibility

**GetNearestSpawnCoordinates**
```csharp
internal static Coordinates GetNearestSpawnCoordinates(
    IDatabase database, 
    ref DCSMission mission, 
    Coordinates coreCoordinates, 
    SpawnPointType[] validSpawnPoints, 
    bool remove = true
)
```

Finds closest valid spawn point to given coordinates.

**AssignTargetSuffix**
```csharp
internal static void AssignTargetSuffix(
    ref GroupInfo? targetGroupInfo, 
    string objectiveName, 
    Boolean isStatic
)
```

Appends `-TGT-ALPHA` suffix to group/unit names for identification.

**GetTransportOrigin**
```csharp
internal static Tuple<DBEntryAirbase?, Coordinates> GetTransportOrigin(
    IBriefingRoom briefingRoom,
    ref DCSMission mission,
    DBEntryObjectiveTargetBehaviorLocation Location,
    Coordinates objectiveCoordinates,
    bool isEscort = false,
    UnitCategory? unitCategory = null
)
```

Determines where transport/escort mission starts:
- Player airbase
- Another airbase
- Near airbase (ground area)
- Specific coordinates

**GetTransportDestination**
```csharp
internal static Tuple<DBEntryAirbase?, Coordinates> GetTransportDestination(
    IBriefingRoom briefingRoom,
    ref DCSMission mission,
    DBEntryObjectiveTargetBehaviorLocation originBehavior,
    DBEntryObjectiveTargetBehaviorLocation destinationBehavior,
    Coordinates originCoordinates,
    Coordinates objectiveCoordinates,
    MinMaxD transportDistance,
    int originAirbaseId,
    bool isEscort = false,
    UnitCategory? unitCategory = null,
    bool enemyAllowed = false
)
```

Calculates destination for transport/escort based on:
- Origin type
- Destination type
- Distance constraints
- Unit category (air/ground/sea)

---

## Data Structures

### DBEntryObjectiveTask

**File**: `Data/Entries/DBEntryObjectiveTask.cs`

Defines what the player must do.

**Properties**:

```csharp
internal string BriefingDescription { get; private set; }
// Links to briefing description template

internal LanguageString[] BriefingTask { get; private set; }
// [0] = singular, [1] = plural
// "Destroy the tank" vs "Destroy the tanks"

internal LanguageString BriefingRemarks { get; private set; }
// Tactical hints for player

internal string[] CompletionTriggersLua { get; private set; }
// Lua scripts that check objective completion

internal Side TargetSide { get; private set; }
// Which side the target belongs to (Ally/Enemy/Neutral)

internal UnitCategory[] ValidUnitCategories { get; private set; }
// What unit types can be used (Plane, Ship, Vehicle, etc.)

internal string[] IncludeOgg { get; private set; }
// Sound files to include

internal string[] RequiredFeatures { get; private set; }
// Required objective features (JTAC, smoke, etc.)
```

**Example Tasks**:
- `Destroy`: Kill all units
- `SEAD`: Destroy SAM sites
- `Strike`: Destroy static targets
- `CAP`: Destroy enemy aircraft
- `Escort`: Protect friendly units
- `CaptureLocation`: Take control of area

### DBEntryObjectiveTarget

**File**: `Data/Entries/DBEntryObjectiveTarget.cs`

Defines what units/objects are the target.

**Properties**:

```csharp
internal LanguageString[] BriefingName { get; private set; }
// Display name: "T-72 tank" vs "T-72 tanks"

internal UnitCategory UnitCategory { get; }
// Derived from first unit family

internal DCSUnitCategory DCSUnitCategory { get; }
// DCS-specific category

internal MinMaxI[] UnitCount { get; private set; }
// How many units per Amount setting (Low/Medium/High)

internal UnitFamily[] UnitFamilies { get; private set; }
// Specific unit families (VehicleAAA, ShipCarrier, etc.)

internal SpawnPointType[] ValidSpawnPoints { get; private set; }
// Where this can spawn (Land/Sea/Air)
```

**Example Targets**:
- `VehicleAAA`: Anti-aircraft artillery
- `BuildingMilitary`: Military structures
- `ShipWarship`: Naval vessels
- `PlaneTransport`: Transport aircraft
- `VehicleArmor`: Tanks and IFVs

### DBEntryObjectiveTargetBehavior

**File**: `Data/Entries/DBEntryObjectiveTargetBehavior.cs`

Defines how targets behave.

**Properties**:

```csharp
internal DBEntryObjectiveTargetBehaviorLocation Location { get; private set; }
// Where target spawns

internal DBEntryObjectiveTargetBehaviorLocation Destination { get; private set; }
// Where target moves to

internal string[] GroupLua { get; private set; }
// Lua template for group (indexed by DCSUnitCategory)

internal string[] UnitLua { get; private set; }
// Lua template for units

internal UnitCategory[] ValidUnitCategories { get; private set; }
// What unit types can use this behavior

internal string[] InvalidTasks { get; private set; }
// Tasks that can't use this behavior

internal bool IsStatic { get; set; }
// Whether units move or are stationary
```

**Behavior Locations**:
```csharp
enum DBEntryObjectiveTargetBehaviorLocation
{
    Default,              // Normal spawn
    Airbase,              // At an airbase
    AirbaseParkingNoHardenedShelter, // Open parking
    PlayerAirbase,        // Player's home base
    NearAirbase,          // Ground area near airbase
    ToFrontLine           // Moving to frontline
}
```

**Example Behaviors**:
- `Idle`: Stationary, no movement
- `Patrolling`: Circular patrol pattern
- `OnRoads`: Following road network
- `ToFrontLine`: Moving toward battle
- `Retreating`: Moving away from frontline

### MissionTemplateSubTaskRecord

The user's objective configuration.

**Properties**:

```csharp
internal string Task { get; set; }
// Task ID (Destroy, SEAD, etc.)

internal string Target { get; set; }
// Target ID (VehicleAAA, etc.)

internal string TargetBehavior { get; set; }
// Behavior ID (Idle, Patrolling, etc.)

internal Amount TargetCount { get; set; }
// Low/Medium/High

internal List<ObjectiveOption> Options { get; set; }
// Additional options

internal List<string> Features { get; set; }
// Feature IDs to add

internal Coordinates CoordinatesHint { get; set; }
// User-specified coordinates

internal bool ProgressionActivation { get; set; }
// Activate based on conditions

internal List<ObjectiveProgressionOption> ProgressionOptions { get; set; }
// Progression behavior

internal List<int> ProgressionDependentTasks { get; set; }
// Which objectives must complete first

internal bool ProgressionDependentIsAny { get; set; }
// OR vs AND logic for dependencies

internal string ProgressionOverrideCondition { get; set; }
// Custom Lua condition

internal List<MissionTemplateSubTaskRecord> SubTasks { get; set; }
// Secondary objectives

internal MinMaxD TransportDistance { get; set; }
// For transport/escort missions
```

### GroupInfo

Return value from `UnitGenerator.AddUnitGroup()`.

**Properties**:

```csharp
struct GroupInfo
{
    internal string Name { get; set; }
    // Group name
    
    internal int GroupID { get; set; }
    // Unique group ID
    
    internal Coordinates Coordinates { get; set; }
    // Spawn position
    
    internal List<DCSGroup> DCSGroups { get; set; }
    // List of DCS group objects (can be multiple for large groups)
    
    internal DCSGroup DCSGroup { get; }
    // Primary group
    
    internal DBEntryJSONUnit UnitDB { get; set; }
    // Unit database entry
    
    internal string[] UnitNames { get; set; }
    // Array of unit names
}
```

---

## Lua Integration

### Objective Lua Table

Each objective creates a Lua table for runtime tracking:

```lua
briefingRoom.mission.objectives[1] = {
    complete = false,           -- Has objective been completed
    failed = false,             -- Has objective failed
    groupName = "TargetGroup-TGT-ALPHA",  -- Target group name
    hideTargetCount = false,    -- Hide unit count from player
    name = "ALPHA",             -- Objective code name
    targetCategory = Unit.Category.GROUND_UNIT,  -- Unit category
    taskType = "Destroy",       -- Task type
    task = "Destroy the T-72 tanks at ALPHA",  -- Briefing text
    unitsCount = 4,             -- Number of units
    unitNames = {               -- List of unit names
        "T-72-1-TGT-ALPHA",
        "T-72-2-TGT-ALPHA",
        "T-72-3-TGT-ALPHA",
        "T-72-4-TGT-ALPHA"
    },
    progressionHidden = false,  -- Starts inactive
    progressionHiddenBrief = false,  -- Hidden in briefing
    progressionCondition = "",  -- Lua condition for activation
    startMinutes = 0,           -- When objective becomes active
    f10MenuText = "$LANG_OBJECTIVE$ ALPHA",  -- F10 menu text
    f10Commands = {}            -- F10 sub-menu commands
}
```

### Completion Trigger Lua

Each task type has associated Lua trigger files:

**Example**: `Include/Lua/ObjectiveTriggers/DestroySAM.lua`

```lua
-- Check if all units in objective are destroyed
local objectiveIndex = $OBJECTIVEINDEX$
local objective = briefingRoom.mission.objectives[objectiveIndex]

if not objective.complete and not objective.failed then
    local alive = 0
    for _, unitName in ipairs(objective.unitNames) do
        local unit = Unit.getByName(unitName)
        if unit and unit:isExist() and unit:getLife() > 1 then
            alive = alive + 1
        end
    end
    
    if alive == 0 then
        objective.complete = true
        briefingRoom.mission.markObjectiveComplete(objectiveIndex)
        trigger.action.outText(
            "Objective " .. objective.name .. " complete!", 
            15
        )
    end
end
```

**Trigger Types**:
- **DestroySAM**: All SAM units destroyed
- **DestroyStatic**: All static objects destroyed
- **Escort**: Unit reaches destination zone
- **Transport**: Cargo delivered to zone
- **Hold**: Area held for duration
- **SEAD**: All radars destroyed

### F10 Menu Integration

Objectives appear in F10 radio menu:

```lua
-- Main objectives menu
local objectivesMenu = missionCommands.addSubMenu("Objectives")

-- Individual objective entries
for i, objective in ipairs(briefingRoom.mission.objectives) do
    local statusText = objective.complete and "[COMPLETE] " 
                    or objective.failed and "[FAILED] " 
                    or "[ACTIVE] "
    
    local objectiveMenu = missionCommands.addSubMenu(
        statusText .. objective.f10MenuText, 
        objectivesMenu
    )
    
    -- Show status
    missionCommands.addCommand(
        "Status", 
        objectiveMenu, 
        showObjectiveStatus, 
        i
    )
    
    -- Show remaining targets
    if not objective.complete and not objective.failed then
        missionCommands.addCommand(
            "Remaining: " .. objective.unitsCount, 
            objectiveMenu, 
            showRemainingTargets, 
            i
        )
    end
end
```

### Progression System

For progressive objectives:

```lua
-- Check progression conditions
local objective = briefingRoom.mission.objectives[2]

if objective.progressionHidden then
    local conditionMet = false
    
    if objective.progressionCondition ~= "" then
        -- Custom Lua condition
        conditionMet = loadstring("return " .. objective.progressionCondition)()
    else
        -- Default: check if dependent objectives complete
        conditionMet = true
        for _, depIndex in ipairs(objective.progressionDependents) do
            if not briefingRoom.mission.objectives[depIndex].complete then
                conditionMet = false
                break
            end
        end
    end
    
    if conditionMet then
        -- Activate objective
        objective.progressionHidden = false
        objective.startMinutes = timer.getTime() / 60
        
        -- Activate groups
        for _, groupName in ipairs(objective.activationGroups) do
            local group = Group.getByName(groupName)
            if group then
                group:activate()
            end
        end
        
        -- Show notification
        if not objective.progressionHiddenBrief then
            trigger.action.outText(
                "New objective: " .. objective.task, 
                30
            )
        end
    end
end
```

---

## Summary

The `CreateObjective` system is a sophisticated, extensible framework that:

1. **Uses the Strategy Pattern** to route different task types to specialized implementations
2. **Shares common logic** through `ObjectiveUtils` helper methods
3. **Integrates deeply with DCS** through Lua scripting and triggers
4. **Supports progression** with conditional objective activation
5. **Provides flexibility** through database-driven configuration
6. **Handles edge cases** like airbase placement, transport missions, and escort protection
7. **Creates complete mission elements** including units, waypoints, briefings, and scripts

This modular design allows for easy extension - new objective types can be added by:
1. Creating a new class in `Objectives/` folder
2. Implementing the standard `CreateObjective` signature
3. Adding specialized logic as needed
4. Registering in the dispatcher switch statement
5. Creating associated database entries and Lua triggers

The system balances sophistication with maintainability, using shared utilities where possible while allowing task-specific customization where needed.
