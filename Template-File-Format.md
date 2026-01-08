# Template File Format Documentation (AI Created)

## Overview

Briefing Room for DCS World uses INI-style template files to define mission and campaign parameters. There are two types of template files:

- **Mission Templates** (`.brt` files) - Define individual missions
- **Campaign Templates** (`.cbrt` files) - Define multi-mission campaigns

## File Format

Both template types use standard INI format with sections denoted by `[SectionName]` and key-value pairs using `key=value` syntax.

---

## Understanding Template Files vs Database Files

This repository contains **two distinct types** of INI files that work together:

### 1. Template Files (.brt / .cbrt) - Mission/Campaign Configuration

**Location:** Root directory and user-created files  
**Purpose:** User-defined mission/campaign recipes

These are the files YOU create to specify what kind of mission you want. Template files contain:
- Player preferences (aircraft, start location, payload)
- Mission parameters (objectives, weather, time of day)
- References to database entries using IDs

**Example:** `Default.brt` specifies `theater=Caucasus`, `coalitionblue=USA`, `objective000.preset=AirbaseStrike`

### 2. Database Files (.ini / .json) - Content Definitions

**Location:** `Database/` and `DatabaseJSON/` folders  
**Purpose:** System-defined available options and content

These are pre-built content definitions that templates reference. Database files define:
- What theaters/maps are available
- What coalitions/countries exist
- What aircraft, units, and weapons are available
- Objective presets, weather patterns, mission features

**Example:** `Database/Theaters/Caucasus.ini` defines the Caucasus map properties  
**Example:** `Database/Coalitions/USA.ini` defines American units and briefing text  
**Example:** `Database/ObjectivePresets/AirbaseStrike.ini` defines how airbase strikes work

### The Relationship

Template files reference database files using IDs:

```
Template File (.brt)              Database Files (.ini)
──────────────────────           ───────────────────────────
theater=Caucasus           ──→   Database/Theaters/Caucasus.ini
coalitionblue=USA          ──→   Database/Coalitions/USA.ini
weatherpreset=Clear        ──→   Database/WeatherPresets/Clear.ini
objective000.preset=       ──→   Database/ObjectivePresets/
  AirbaseStrike                    AirbaseStrike.ini
objective000.task=         ──→   Database/ObjectiveTasks/
  DestroyAll                       DestroyAll.ini
objective000.target=       ──→   Database/ObjectiveTargets/
  VehicleAny                       VehicleAny.ini
missionfeatures=           ──→   Database/MissionFeatures/
  FriendlyAWACS                    FriendlyAWACS.ini
aircrafttype=F-16C_50      ──→   DatabaseJSON/UnitPlanes.json
```

**Think of it this way:**
- **Template files** = Your recipe for a specific mission
- **Database files** = The ingredients library defining what's available

### Database Categories

| Database Folder | Contains | Referenced By |
|----------------|----------|---------------|
| `Database/Coalitions/` | Countries and factions | `[Context] coalitionblue/red` |
| `Database/Theaters/` | Maps and terrain data | `[Context] theater` |
| `Database/WeatherPresets/` | Weather conditions | `[Environment] weatherpreset` |
| `Database/ObjectivePresets/` | Pre-configured objectives | `[Objectives] objective###.preset` |
| `Database/ObjectiveTasks/` | Task types (destroy, recon) | `[Objectives] objective###.task` |
| `Database/ObjectiveTargets/` | Target types (vehicles, SAMs) | `[Objectives] objective###.target` |
| `Database/ObjectiveTargetsBehaviors/` | Target behaviors | `[Objectives] objective###.targetbehavior` |
| `Database/MissionFeatures/` | Features (AWACS, tankers) | `[MissionFeatures] missionfeatures` |
| `Database/DCSMods/` | Required DCS mods | `[Mods] mods` |
| `DatabaseJSON/UnitPlanes.json` | Aircraft definitions | `playerflightgroup###.aircrafttype` |
| `DatabaseJSON/TheatersAirbases.json` | Airbase locations | `[FlightPlan] theaterstartingairbase` |
| `DatabaseJSON/Situations/*.json` | Geopolitical scenarios | `[Context] situation` |

**This documentation covers template files only.** To see what values are available for each field, browse the corresponding Database folders.

---

## Mission Template Format (.brt)

### Sections Overview

Mission templates consist of the following sections:

1. `[Context]` - Theater, era, and coalition settings
2. `[Briefing]` - Mission name and description
3. `[Environment]` - Weather and time settings
4. `[FlightPlan]` - Objective distances and airbases
5. `[Objectives]` - Mission objectives and targets
6. `[PlayerFlightGroups]` - Player aircraft configuration
7. `[AircraftPackages]` - Multi-flight coordination (optional)
8. `[Situation]` - Enemy and friendly force levels
9. `[CombinedArms]` - Combined Arms slot configuration
10. `[Options]` - Mission options and realism settings
11. `[MissionFeatures]` - Additional features (AWACS, tankers, etc.)
12. `[Mods]` - Required DCS mods
13. `[CarrierHints]` - Carrier positioning hints (optional)

---

### [Context] Section

Defines the geopolitical and temporal context of the mission.

#### Fields

| Field | Type | Description | Possible Values |
|-------|------|-------------|-----------------|
| `coalitionblue` | String | Blue coalition country/faction | Valid coalition ID (e.g., "USA", "Russia", "NATO") |
| `coalitionred` | String | Red coalition country/faction | Valid coalition ID |
| `decade` | Enum | Historical era | `Decade1940`, `Decade1950`, `Decade1960`, `Decade1970`, `Decade1980`, `Decade1990`, `Decade2000`, `Decade2010`, `Decade2020` |
| `playercoalition` | Enum | Player's coalition | `Blue`, `Red` |
| `theater` | String | Map/theater | Valid theater ID (e.g., "Caucasus", "PersianGulf", "Syria", "Nevada", "Normandy", "TheChannel", "MarianaIslands", "SinaiMap", "Afghanistan", "Kola", "Falklands", "Iraq", "GermanyCW") |
| `situation` | String (Optional) | Specific geopolitical situation | Valid situation ID for the theater, or empty string for random |

**Example:**
```ini
[context]
coalitionblue=USA
coalitionred=Russia
decade=Decade2020
playercoalition=Blue
theater=Caucasus
situation=
```

---

### [Briefing] Section

Mission briefing information.

#### Fields

| Field | Type | Description | Possible Values |
|-------|------|-------------|-----------------|
| `missionname` | String (Optional) | Custom mission name | Any string, or empty for auto-generated name |
| `missiondescription` | String (Optional) | Custom mission description | Any string, use `\n` for newlines |

**Example:**
```ini
[briefing]
missionname=Strike on Kobuleti
missiondescription=Destroy enemy air defenses\nProtect friendly forces
```

---

### [Environment] Section

Weather and time of day settings.

#### Fields

| Field | Type | Description | Possible Values |
|-------|------|-------------|-----------------|
| `season` | Enum | Season/month | `Random`, `Spring`, `Summer`, `Fall`, `Winter` |
| `timeofday` | Enum | Time of day | `Random`, `RandomDaytime`, `Dawn`, `Noon`, `Twilight`, `Night` |
| `weatherpreset` | String (Optional) | Weather preset | Valid weather preset ID (e.g., "Clear", "Overcast", "ScatteredClouds", "OvercastAndRain"), or empty for random |
| `wind` | Enum | Wind strength | `Random`, `Calm`, `LightBreeze`, `ModerateBreeze`, `StrongBreeze`, `Gale`, `Storm` |

**Example:**
```ini
[environment]
season=Summer
timeofday=Noon
weatherpreset=Clear
wind=LightBreeze
```

---

### [FlightPlan] Section

Flight plan parameters including objective distances and airbase selection.

#### Fields

| Field | Type | Description | Possible Values |
|-------|------|-------------|-----------------|
| `objectivedistancemax` | Integer | Maximum distance to first objective (NM) | 0 to system max (typically 999) |
| `objectivedistancemin` | Integer | Minimum distance to first objective (NM) | 0 to `objectivedistancemax` |
| `objectiveseparationmax` | Integer | Maximum distance between objectives (NM) | 0 to system max |
| `objectiveseparationmin` | Integer | Minimum distance between objectives (NM) | 0 to `objectiveseparationmax` |
| `borderlimit` | Integer | Maximum distance from coalition borders (NM) | System min to max (typically 10-500) |
| `theaterstartingairbase` | String (Optional) | Starting airbase | Valid airbase ID for the theater, or empty for auto-select |
| `theaterdestinationairbase` | String (Optional) | Recovery airbase | Valid airbase ID, `"home"` for same as starting, or empty for auto-select |

**Example:**
```ini
[flightplan]
objectivedistancemax=160
objectivedistancemin=40
objectiveseparationmax=100
objectiveseparationmin=10
borderlimit=100
theaterstartingairbase=
theaterdestinationairbase=home
```

---

### [Objectives] Section

Defines mission objectives. Each objective is a numbered entry (000, 001, 002, etc.).

#### Main Objective Fields

For each objective `objective###`:

| Field | Type | Description | Possible Values |
|-------|------|-------------|-----------------|
| `objective###.preset` | String | Objective preset | "Custom" or valid preset ID (e.g., "AirbaseStrike", "DeepStrike", "CombatAirPatrol", "DEAD", "AntiShipStrike", "CSAR", "TransportCargo", etc.) |
| `objective###.task` | String | Task type | Valid task ID (e.g., "DestroyAll", "Reconnaissance", "Escort", "Transport", "Intercept") |
| `objective###.target` | String | Target type | Valid target ID (e.g., "VehicleAny", "AircraftAny", "ShipAny", "BuildingMilitary", "SAMLong", "SAMMedium", etc.) |
| `objective###.targetbehavior` | String | Target behavior | Valid behavior ID (e.g., "Idle", "Airbase", "CarrierGroup", "Waypoints", "Air") |
| `objective###.targetcount` | Enum | Number of target units | `VeryLow`, `Low`, `Average`, `High`, `VeryHigh` |
| `objective###.features` | Comma-separated list (Optional) | Additional features | Valid feature IDs |
| `objective###.options` | Comma-separated list (Optional) | Objective options | `EmbeddedAirDefense`, `HideTarget`, `InaccurateWaypoint`, `ShowTarget`, `Invisible`, `NoAircraftWaypoint`, `FeaturesAsTargets` |
| `objective###.coordinatehint` | Coordinates (Optional) | Manual position | "X,Y" coordinates, or "0,0" for automatic |
| `objective###.transportdistancemin` | Integer (Optional) | Transport minimum distance | 0 to max, for transport missions |
| `objective###.transportdistancemax` | Integer (Optional) | Transport maximum distance | Min to max, for transport missions |

#### Progression Fields

For progressive/dynamic objectives:

| Field | Type | Description | Possible Values |
|-------|------|-------------|-----------------|
| `objective###.progression.activation` | Boolean (Optional) | Requires activation | `True`, `False` |
| `objective###.progression.dependenttasks` | Comma-separated integers (Optional) | Required previous tasks | Task indices (e.g., "0,1,2") |
| `objective###.progression.isany` | Boolean (Optional) | Require any dependent (not all) | `True`, `False` |
| `objective###.progression.options` | Comma-separated list (Optional) | Progression options | `PreProgressionSpottable`, `ProgressionHiddenBrief` |
| `objective###.progression.overridecondition` | String (Optional) | Custom Lua condition | Lua code string |

#### Sub-Task Fields

Objectives can have sub-tasks with similar fields, prefixed with `objective###.subtask###.`:

| Field | Type | Description |
|-------|------|-------------|
| `objective###.subtask###.task` | String | Sub-task type |
| `objective###.subtask###.target` | String | Sub-task target type |
| `objective###.subtask###.targetbehavior` | String | Sub-task target behavior |
| `objective###.subtask###.targetcount` | Enum | Sub-task target count |
| `objective###.subtask###.options` | List (Optional) | Sub-task options |
| `objective###.subtask###.preset` | String | Sub-task preset |
| ... (progression fields as above) | | |

**Example:**
```ini
[objectives]
objective000.preset=Custom
objective000.task=DestroyAll
objective000.target=VehicleAny
objective000.targetbehavior=Idle
objective000.targetcount=Average
objective000.coordinatehint=0,0
objective000.progression.activation=False
objective000.progression.dependenttasks=
objective000.progression.isany=False
objective000.progression.options=
objective000.progression.overridecondition=

objective001.preset=DeepStrike
objective001.task=DestroyAll
objective001.target=SAMLong
objective001.targetbehavior=Idle
objective001.targetcount=High
objective001.options=EmbeddedAirDefense
objective001.coordinatehint=0,0
objective001.subtask000.task=DestroyAll
objective001.subtask000.target=BuildingMilitary
objective001.subtask000.targetcount=Low
```

---

### [PlayerFlightGroups] Section

Defines player-controllable aircraft flights. Each flight group is numbered (000, 001, etc.).

#### Fields

For each flight group `playerflightgroup###`:

| Field | Type | Description | Possible Values |
|-------|------|-------------|-----------------|
| `playerflightgroup###.aircrafttype` | String | Aircraft model | Valid aircraft ID (e.g., "F-16C_50", "F/A-18C_hornet", "A-10C_2", "Su-25T", "M-2000C", "AV8BNA") |
| `playerflightgroup###.count` | Integer | Number of aircraft | 1 to 4 |
| `playerflightgroup###.aiwingmen` | Boolean | AI controls wingmen | `True` (player only), `False` (all player-controllable) |
| `playerflightgroup###.hostile` | Boolean | Hostile/OPFOR flight | `True`, `False` |
| `playerflightgroup###.payload` | String (Optional) | Loadout | "default" or valid payload name for the aircraft |
| `playerflightgroup###.country` | Enum | Country/flag | Valid Country enum value (e.g., "CombinedJointTaskForcesBlue", "CombinedJointTaskForcesRed", "USA", "Russia", etc.) |
| `playerflightgroup###.startlocation` | Enum | Start position | `ParkingCold`, `ParkingHot`, `Runway`, `Air` |
| `playerflightgroup###.carrier` | String (Optional) | Carrier name | Valid carrier unit ID or empty |
| `playerflightgroup###.returntocarrier` | Boolean (Optional) | Return to carrier | `True`, `False` |
| `playerflightgroup###.livery` | String (Optional) | Aircraft skin | "default" or valid livery name |
| `playerflightgroup###.overrideradiofrequency` | String (Optional) | Radio frequency | Frequency string (e.g., "251.000") or empty |
| `playerflightgroup###.overrideradioband` | Enum (Optional) | Radio band | `AM`, `FM` |
| `playerflightgroup###.overridecallsignname` | String (Optional) | Custom callsign | Callsign name or empty |
| `playerflightgroup###.overridecallsignnumber` | Integer (Optional) | Callsign number | 1-9 |

**Example:**
```ini
[playerflightgroups]
playerflightgroup000.aircrafttype=F-16C_50
playerflightgroup000.aiwingmen=False
playerflightgroup000.hostile=False
playerflightgroup000.count=2
playerflightgroup000.payload=default
playerflightgroup000.country=CombinedJointTaskForcesBlue
playerflightgroup000.startlocation=Runway
playerflightgroup000.livery=default
playerflightgroup000.carrier=
playerflightgroup000.returntocarrier=False
playerflightgroup000.overrideradiofrequency=
playerflightgroup000.overrideradioband=AM
playerflightgroup000.overridecallsignname=
playerflightgroup000.overridecallsignnumber=1
```

---

### [AircraftPackages] Section (Optional)

Defines multi-flight strike packages for coordinated operations.

#### Fields

For each package `aircraftpackage###`:

| Field | Type | Description | Possible Values |
|-------|------|-------------|-----------------|
| `aircraftpackage###.flightgroupindexes` | Comma-separated integers | Flight groups in package | Indices of player flight groups (e.g., "0,1,2") |
| `aircraftpackage###.objectiveindexes` | Comma-separated integers | Target objectives | Indices of objectives (e.g., "0,1") |
| `aircraftpackage###.startingairbase` | String | Departure airbase | "home", "homeDest", or valid airbase ID |
| `aircraftpackage###.destinationairbase` | String | Recovery airbase | "home", "homeDest", "strike", or valid airbase ID |

**Example:**
```ini
[aircraftpackages]
aircraftpackage000.flightgroupindexes=0,1
aircraftpackage000.objectiveindexes=0
aircraftpackage000.startingairbase=home
aircraftpackage000.destinationairbase=home
```

---

### [Situation] Section

Defines force levels and skill for both coalitions.

#### Fields

| Field | Type | Description | Possible Values |
|-------|------|-------------|-----------------|
| `enemyskill` | Enum | Enemy AI skill | `Random`, `VeryLow`, `Low`, `Average`, `High`, `VeryHigh` |
| `enemyairdefense` | Enum | Enemy air defense amount | `Random`, `None`, `VeryLow`, `Low`, `Average`, `High`, `VeryHigh` |
| `enemyairforce` | Enum | Enemy CAP presence | `Random`, `None`, `VeryLow`, `Low`, `Average`, `High`, `VeryHigh` |
| `friendlyskill` | Enum | Friendly AI skill | Same as enemy skill |
| `friendlyairdefense` | Enum | Friendly air defense | Same as enemy air defense |
| `friendlyairforce` | Enum | Friendly CAP presence | Same as enemy air force |

**Example:**
```ini
[situation]
enemyskill=High
enemyairdefense=Average
enemyairforce=Low
friendlyskill=Average
friendlyairdefense=VeryLow
friendlyairforce=VeryLow
```

---

### [CombinedArms] Section

Combined Arms slot configuration.

#### Fields

| Field | Type | Description | Possible Values |
|-------|------|-------------|-----------------|
| `commanderblue` | Integer | Blue commander slots | 0 to max (typically 0-10) |
| `commanderred` | Integer | Red commander slots | 0 to max |
| `jtacblue` | Integer | Blue JTAC slots | 0 to max |
| `jtacred` | Integer | Red JTAC slots | 0 to max |

**Example:**
```ini
[combinedarms]
commanderblue=0
commanderred=0
jtacblue=0
jtacred=0
```

---

### [Options] Section

Mission options and realism settings.

#### Fields

| Field | Type | Description | Possible Values |
|-------|------|-------------|-----------------|
| `fogofwar` | Enum | Fog of war setting | `All`, `AlliesOnly`, `KnownUnitsOnly`, `SelfOnly`, `None` |
| `mission` | Comma-separated list | Mission options | See Mission Options table below |
| `realism` | Comma-separated list | Realism options | See Realism Options table below |
| `unitbanlist` | Comma-separated list (Optional) | Banned units | Unit IDs to exclude from mission |
| `airbasedynamicspawn` | Enum | Dynamic airbase spawning | `All`, `Friendly`, `StrikePackages`, `HomeAirbase`, `None` |
| `carrierdynamicspawn` | Boolean | Dynamic carrier spawning | `True`, `False` |
| `dsallowhotstart` | Boolean | Allow hot start for dynamic | `True`, `False` |
| `airbasedynamiccargo` | Enum | Dynamic cargo at airbases | `All`, `Friendly`, `None` |
| `carrierdynamiccargo` | Boolean | Dynamic cargo on carriers | `True`, `False` |

#### Mission Options

Available values for `mission` field (comma-separated):

| Option | Description |
|--------|-------------|
| `AllowLowPoly` | Allow low-polygon models |
| `BlockSuppliers` | Block specific suppliers for units |
| `CombinedArmsPilotControl` | Allow CA to control aircraft |
| `EndMissionAutomatically` | Auto-end on objectives complete |
| `EndMissionOnCommand` | End via F10 menu command |
| `ImperialUnitsForBriefing` | Use feet/nautical miles |
| `InvertCountriesCoalitions` | Swap coalition ownership |
| `MarkWaypoints` | Show waypoint markers on map |
| `NoBDA` | Disable battle damage assessment |
| `RadioMessagesTextOnly` | Text messages only, no audio |
| `SeaLevelRefCloud` | Cloud base at sea level |
| `HighCloud` | Raise cloud base by 2000m |
| `SpawnAnywhere` | Ignore coalition territories |
| `TargetOnlyBDA` | BDA for targets only |

#### Realism Options

Available values for `realism` field (comma-separated):

| Option | Description |
|--------|-------------|
| `BirdStrikes` | Enable bird strikes |
| `DisableDCSRadioAssists` | Disable simplified radio |
| `HideLabels` | Hide aircraft labels |
| `NoBDA` | Disable battle damage assessment |
| `NoCheats` | Disable cheats |
| `NoCrashRecovery` | Disable crash recovery |
| `NoEasyComms` | Disable easy communications |
| `NoExternalViews` | Disable external views |
| `NoGameMode` | Disable game mode features |
| `NoOverlays` | Hide overlays |
| `NoPadlock` | Disable padlock view |
| `RandomFailures` | Enable random failures |
| `RealisticGEffects` | Realistic G-force effects |
| `WakeTurbulence` | Enable wake turbulence |

**Example:**
```ini
[options]
fogofwar=All
mission=AllowLowPoly,CombinedArmsPilotControl,ImperialUnitsForBriefing,MarkWaypoints
realism=DisableDCSRadioAssists,NoBDA
unitbanlist=
airbasedynamicspawn=None
carrierdynamicspawn=False
dsallowhotstart=False
airbasedynamiccargo=Friendly
carrierdynamiccargo=True
```

---

### [MissionFeatures] Section

Additional mission features to enable.

#### Fields

| Field | Type | Description | Possible Values |
|-------|------|-------------|-----------------|
| `missionfeatures` | Comma-separated list | Feature IDs | Valid feature IDs from database (see Features table below) |

#### Common Features

| Feature ID | Description |
|------------|-------------|
| `FriendlyAWACS` | Friendly AWACS aircraft |
| `FriendlyTankerBasket` | Probe & drogue tanker |
| `FriendlyTankerBoom` | Boom refueling tanker |
| `EnemyAWACS` | Enemy AWACS |
| `EnemyTanker` | Enemy tanker |
| `CTLD` | Complete Tactical Lift and Delivery |
| `CSAR` | Combat Search and Rescue |
| `ImprovementsResponsiveAircraftActivator` | Responsive AI spawning |
| `FogOfWarBlue` | Blue coalition fog of war |
| `FogOfWarRed` | Red coalition fog of war |

*(Note: Check Database/MissionFeatures/ folder for complete list)*

**Example:**
```ini
[missionfeatures]
missionfeatures=FriendlyAWACS,FriendlyTankerBasket,FriendlyTankerBoom
```

---

### [Mods] Section

Required DCS mods/modules.

#### Fields

| Field | Type | Description | Possible Values |
|-------|------|-------------|-----------------|
| `mods` | Comma-separated list (Optional) | Mod/module IDs | Valid mod IDs from database |

**Example:**
```ini
[mods]
mods=
```

Or with mods:
```ini
[mods]
mods=A-4E-C,MB-339
```

---

### [CarrierHints] and [CarrierHintsNames] Sections (Optional)

Manual carrier positioning hints.

#### Fields

Carriers are indexed and positioned using two parallel sections:

```ini
[carrierhints]
hint000=123456.5,234567.8
hint001=345678.9,456789.1

[carrierhintsnames]
hint000=CVN-71 Theodore Roosevelt
hint001=LHA-1 Tarawa
```

Where coordinates are X,Y positions on the map.

---

## Campaign Template Format (.cbrt)

Campaign templates extend mission templates with additional settings for multi-mission campaigns.

### Additional Sections

Campaign templates include all mission template sections PLUS:

1. `[Environment]` - Campaign-wide environment settings
2. `[Missions]` - Campaign structure configuration
3. `[CampaignOptions]` - Campaign-specific options

### [Environment] Section (Campaign)

Campaign-specific environment settings.

#### Fields

| Field | Type | Description | Possible Values |
|-------|------|-------------|-----------------|
| `badweatherchance` | Enum | Frequency of bad weather | `VeryLow`, `Low`, `Average`, `High`, `VeryHigh` |
| `nightmissionchance` | Enum | Frequency of night missions | `VeryLow`, `Low`, `Average`, `High`, `VeryHigh` |

**Example:**
```ini
[environment]
badweatherchance=VeryLow
nightmissionchance=VeryLow
```

---

### [Missions] Section

Campaign structure and mission generation parameters.

#### Fields

| Field | Type | Description | Possible Values |
|-------|------|-------------|-----------------|
| `count` | Integer | Number of missions | Min to max (typically 1-20) |
| `difficultyvariation` | Enum | Difficulty progression | `Random`, `Constant`, `EasyToHard`, `HardToEasy`, `Rollercoaster` |
| `objectives` | Comma-separated list | Allowed objective types | Valid objective preset IDs or "Random" |
| `objectivecount` | Enum | Objectives per mission | `VeryLow`, `Low`, `Average`, `High`, `VeryHigh` |
| `objectivevariationdistance` | Enum | Objective position variance | Same as objectivecount |
| `airbasevariationdistance` | Enum | Airbase variance | Same as objectivecount |
| `persistentairbases` | Boolean | Persistent airbase state | `True`, `False` |
| `targetcount` | Enum | Targets per objective | Same as objectivecount |
| `progression` | Enum | Campaign progression type | `None`, `Linear`, `Branching` |

**Example:**
```ini
[missions]
count=5
difficultyvariation=Random
objectives=AirbaseStrike,DeepStrike,CombatAirPatrol,AntiShipStrike
objectivecount=Average
objectivevariationdistance=Average
airbasevariationdistance=Average
persistentairbases=False
targetcount=Average
progression=None
```

---

### [CampaignOptions] Section

Campaign-specific options.

#### Fields

| Field | Type | Description | Possible Values |
|-------|------|-------------|-----------------|
| `staticsituation` | Boolean | Static geopolitical situation | `True`, `False` |

**Example:**
```ini
[campaignoptions]
staticsituation=False
```

---

## Complete Example: Mission Template

```ini
[context]
coalitionblue=USA
coalitionred=Russia
decade=Decade2020
playercoalition=Blue
theater=Caucasus
situation=

[briefing]
missionname=
missiondescription=

[environment]
season=Random
timeofday=RandomDaytime
weatherpreset=
wind=Random

[flightplan]
objectivedistancemax=160
objectivedistancemin=40
objectiveseparationmax=100
objectiveseparationmin=10
borderlimit=100
theaterstartingairbase=
theaterdestinationairbase=home

[objectives]
objective000.preset=Custom
objective000.task=DestroyAll
objective000.target=VehicleAny
objective000.targetbehavior=Idle
objective000.targetcount=Average
objective000.coordinatehint=0,0
objective000.progression.activation=False
objective000.progression.dependenttasks=
objective000.progression.isany=False
objective000.progression.options=
objective000.progression.overridecondition=

[playerflightgroups]
playerflightgroup000.aircrafttype=F-16C_50
playerflightgroup000.aiwingmen=False
playerflightgroup000.hostile=False
playerflightgroup000.count=2
playerflightgroup000.payload=default
playerflightgroup000.country=CombinedJointTaskForcesBlue
playerflightgroup000.startlocation=Runway
playerflightgroup000.livery=default
playerflightgroup000.carrier=
playerflightgroup000.returntocarrier=False
playerflightgroup000.overrideradiofrequency=
playerflightgroup000.overrideradioband=AM
playerflightgroup000.overridecallsignname=
playerflightgroup000.overridecallsignnumber=1

[situation]
enemyskill=Random
enemyairdefense=Random
enemyairforce=Random
friendlyskill=Random
friendlyairdefense=Random
friendlyairforce=Random

[combinedarms]
commanderblue=0
commanderred=0
jtacblue=0
jtacred=0

[options]
fogofwar=All
mission=AllowLowPoly,ImperialUnitsForBriefing,MarkWaypoints
realism=DisableDCSRadioAssists,NoBDA
unitbanlist=
airbasedynamicspawn=None
carrierdynamicspawn=False
dsallowhotstart=False
airbasedynamiccargo=Friendly
carrierdynamiccargo=True

[missionfeatures]
missionfeatures=FriendlyAWACS,FriendlyTankerBasket,FriendlyTankerBoom

[mods]
mods=
```

---

## Complete Example: Campaign Template

```ini
[context]
coalitionblue=USA
coalitionred=Russia
decade=Decade2020
playercoalition=Blue
theater=Caucasus
situation=

[briefing]
campaignname=Operation Desert Thunder
missiondescription=

[environment]
badweatherchance=VeryLow
nightmissionchance=VeryLow

[flightplan]
objectivedistancemax=160
objectivedistancemin=40

[missions]
count=5
difficultyvariation=Random
objectives=AirbaseStrike,DeepStrike,CombatAirPatrol,DEAD
objectivecount=Average
objectivevariationdistance=Average
airbasevariationdistance=Average
persistentairbases=False
targetcount=Average
progression=None

[playerflightgroups]
playerflightgroup000.aircrafttype=F-16C_50
playerflightgroup000.aiwingmen=False
playerflightgroup000.hostile=False
playerflightgroup000.count=2
playerflightgroup000.payload=default
playerflightgroup000.country=CombinedJointTaskForcesBlue
playerflightgroup000.startlocation=Runway
playerflightgroup000.livery=default

[situation]
enemyskill=Random
enemyairdefense=Random
enemyairforce=Random
friendlyskill=Random
friendlyairdefense=Random
friendlyairforce=Random

[combinedarms]
commanderblue=0
commanderred=0
jtacblue=0
jtacred=0

[options]
fogofwar=All
mission=AllowLowPoly,ImperialUnitsForBriefing,MarkWaypoints
realism=DisableDCSRadioAssists,NoBDA
airbasedynamicspawn=None
carrierdynamicspawn=False
dsallowhotstart=False
airbasedynamiccargo=Friendly
carrierdynamiccargo=True

[missionfeatures]
missionfeatures=FriendlyAWACS,FriendlyTankerBasket

[mods]
mods=

[campaignoptions]
staticsituation=False
```

---

## Quick Reference: Enum Values

### Coalition
- `Blue`
- `Red`

### Decade
- `Decade1940` through `Decade2020` (increments of 10)

### Season
- `Random`, `Spring`, `Summer`, `Fall`, `Winter`

### TimeOfDay
- `Random`, `RandomDaytime`, `Dawn`, `Noon`, `Twilight`, `Night`

### Wind
- `Random`, `Calm`, `LightBreeze`, `ModerateBreeze`, `StrongBreeze`, `Gale`, `Storm`

### Amount / AmountNR / AmountR
- **Amount**: `VeryLow`, `Low`, `Average`, `High`, `VeryHigh`
- **AmountNR**: `Random`, `None`, + all Amount values
- **AmountR**: `Random` + all Amount values

### FogOfWar
- `All`, `AlliesOnly`, `KnownUnitsOnly`, `SelfOnly`, `None`

### PlayerStartLocation
- `ParkingCold`, `ParkingHot`, `Runway`, `Air`

### DsAirbase (Dynamic Spawn)
- `All`, `Friendly`, `StrikePackages`, `HomeAirbase`, `None`

### RadioModulation
- `AM`, `FM`

---

## Notes

1. **Case Sensitivity**: Field names are case-insensitive, but values (especially IDs) may be case-sensitive
2. **Empty Values**: Empty strings ("") often mean "auto-select" or "disabled"
3. **Arrays**: Comma-separated values with no spaces (e.g., `value1,value2,value3`)
4. **Comments**: Lines starting with `;` or `#` are comments
5. **Coordinates**: Format is `X,Y` with decimal precision (e.g., `123456.5,234567.8`)
6. **Database IDs**: Many fields reference database entries - check the Database/ folder for valid IDs
7. **Validation**: Invalid values may fall back to defaults or cause generation errors

---

## Finding Valid Values

### Database Locations

- **Coalitions**: `Database/Coalitions/*.ini`
- **Theaters**: `Database/Theaters/*.ini`
- **Situations**: `DatabaseJSON/Situations/*.json`
- **Weather Presets**: `Database/WeatherPresets/*.ini`
- **Objective Presets**: `Database/ObjectivePresets/*.ini`
- **Mission Features**: `Database/MissionFeatures/*.ini`
- **Aircraft**: `DatabaseJSON/UnitPlanes.json`
- **Airbases**: `DatabaseJSON/TheatersAirbases.json`
- **Mods**: `Database/DCSMods/*.ini`

### GUI Tools

Use the Briefing Room GUI or web interface to:
- Browse available options
- Generate template files
- Validate configurations
- Export working templates as starting points

---

## Version Compatibility

This documentation is based on Briefing Room for DCS World as of January 2026. Field names and available values may change in future versions. Always refer to the generated default templates and database files for the most current information.
