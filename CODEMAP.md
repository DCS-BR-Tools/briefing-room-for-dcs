# BriefingRoom for DCS — CodeMap

A high-level structural overview of the codebase to help developers and AI agents orient quickly.

---

## Repository Layout

```
briefing-room-for-dcs/
├── src/                        # All .NET source code
│   ├── BriefingRoom/           # Core library (the engine)
│   ├── Web/                    # Blazor Server web front-end
│   ├── Desktop/                # WinForms/Blazor hybrid desktop app
│   ├── CommandLine/            # CLI entry point
│   ├── Tests/                  # xUnit test suite
│   ├── UpdateService/          # Auto-update service
│   └── Updater/                # Updater utility
├── Database/                   # INI configuration files (settings, names, presets)
├── DatabaseJSON/               # JSON data files (units, theaters, templates)
├── Include/
│   ├── Lua/                    # Lua scripts embedded in DCS .miz files
│   └── Html/                   # HTML briefing templates
├── .github/workflows/          # CI/CD (build, release, Docker)
└── docs/                       # GitHub Pages documentation
```

---

## Core Library: `src/BriefingRoom/`

The library is the sole engine. All frontends (Web, Desktop, CLI) call it through `IBriefingRoom`.

### Entry Point

| File | Role |
|------|------|
| `IBriefingRoom.cs` | Public interface — all frontends program against this |
| `BriefingRoom.cs` | Concrete implementation; bootstraps Database, exposes `GenerateMission` / `GenerateCampaign` |
| `BriefingRoomException.cs` | Custom exception for user-readable errors |

### `Data/` — Database & Configuration

Loads and caches all INI/JSON configuration into strongly-typed objects.

```
Data/
├── Database.cs             # Central registry; lazy-loads DBEntry subclasses
├── IDatabase.cs            # Interface used by Generator
├── DatabaseTools.cs        # INI/JSON parsing helpers
├── Constants.cs            # Global constants (paths, magic strings)
├── Common/                 # Shared config tables (air defence levels, CAP, names…)
│   ├── DBCommon.cs         # Root aggregate of all "common" tables
│   ├── DBCommonAirDefense.cs
│   ├── DBCommonCAP.cs
│   ├── DBCommonCarrierGroups.cs
│   ├── DBCommonFrontLine.cs
│   ├── DBCommonNames.cs
│   ├── DBCommonWind.cs
│   └── DBLanguage.cs       # Translation key→string lookup
├── Entries/                # One class per database entity type
│   ├── DBEntry.cs          # Abstract base; loaded from INI or JSON
│   ├── DBEntryTheater.cs   # Theater geography, airbases, spawn zones
│   ├── DBEntryCoalition.cs # Blue/Red coalition unit lists
│   ├── DBEntryObjective*.cs # Target, task, behavior, flags, preset
│   ├── DBEntryFeature*.cs  # Mission & objective feature scripts
│   ├── DBEntryWeatherPreset.cs
│   └── DBEntryUnitRadioPreset.cs
├── Enums/                  # Shared enumerations
│   ├── SpawnPointType.cs
│   ├── DBEntryUnitFlags.cs
│   └── …
└── JSON/                   # JSON deserialization models (units, theaters, etc.)
```

**Data flow:** `Database` → lazy-loads `DBEntry` subclasses from `Database/` (INI) and `DatabaseJSON/` (JSON) → cached in `Dictionary<Type, Dictionary<string, DBEntry>>`.

---

### `Template/` — Mission / Campaign Templates

User-authored templates that drive generation. Serialized as `.brt` (mission) and `.cbrt` (campaign) JSON files.

```
Template/
├── IBaseTemplate.cs            # Shared interface
├── BaseTemplate.cs             # Common fields (theater, coalitions, date, weather…)
├── MissionTemplate.cs          # Single-mission template
├── MissionTemplateFlightGroup.cs
├── MissionTemplateObjective.cs
├── MissionTemplatePackage.cs
├── MissionTemplateGroup.cs
├── MissionTemplateSubTask.cs
├── CampaignTemplate.cs         # Campaign-level wrapper
├── Enums/                      # Template-specific enums (side, country, payload…)
└── Records/                    # Immutable record copies used during generation
```

Templates are read-only during generation; they are converted into `MissionTemplateRecord` (a record) before being passed to the generator.

---

### `Generator/` — Mission Generation Engine

Orchestrates all generation stages. The generator is stateless (all state lives in `DCSMission`).

```
Generator/
├── MissionGenerator/           # Primary orchestrator
│   ├── Generator.cs            # Entry point: runs STAGE_ORDER pipeline
│   ├── AirDefense.cs           # Places SAM/AAA groups
│   ├── Airbases.cs             # Selects player & enemy airbases
│   ├── Briefing.cs             # Generates briefing text
│   ├── CombatAirPatrols.cs     # Spawns CAP flights
│   ├── CarrierGroup.cs         # Carrier group placement
│   ├── Countries.cs            # Assigns units to DCS countries
│   ├── Features.cs             # Injects mission/objective feature scripts
│   ├── FlightPlan.cs           # Waypoint generation
│   ├── FrontLine.cs            # Front-line ground forces
│   ├── Options.cs              # Mission options (labels, map marks…)
│   ├── PlayerFlightGroups.cs   # Player aircraft groups
│   ├── Temporal.cs             # Date/time/season
│   ├── Warehouses.cs           # Airbase warehouse (ammo/fuel)
│   ├── Weather.cs              # Weather & wind
│   └── Objectives/             # Objective sub-generators
│       ├── ObjectiveGenerator.cs   # Drives per-objective loop
│       ├── Basic.cs            # Standard destroy/recon/strike
│       ├── Escort.cs           # Escort objectives
│       ├── Hold.cs             # Hold zone objectives
│       ├── Transport.cs        # Transport objectives
│       ├── TransportDynamicCargo.cs
│       ├── ObjectiveContext.cs # Per-objective shared state
│       └── ObjectiveCreationHelpers.cs
├── UnitMaker/                  # Spawns and configures unit groups
│   ├── UnitGenerator.cs        # Selects unit types from DB
│   ├── UnitMakerSpawnPointSelector.cs  # Finds valid spawn coords
│   ├── UnitMakerCallsignGenerator.cs   # NATO/custom callsigns
│   ├── UnitMakerGroupFlags.cs
│   └── UnitCallsign.cs
├── CampaignGenerator.cs        # Drives multi-mission campaign generation
├── BrowserManager.cs           # Opens URLs (briefing, map) in browser
├── DrawingMaker.cs             # Adds map drawings/markings
├── FlightPathWaypoint.cs       # Waypoint value object
├── GeneratorTools.cs           # Shared generation utilities
├── Imagery.cs                  # Mission imagery/kneeboard
├── TriggerMaker.cs             # DCS trigger construction
└── ZoneMaker.cs                # DCS trigger zone construction
```

**Generation pipeline (STAGE_ORDER):**
`Situation → Airbase → WorldPreload → FrontLine → Objective → Carrier → PlayerFlightGroups → CAPResponse → AirDefense → MissionFeatures`

---

### `Mission/` — Output Model

The in-memory representation of a generated DCS mission before it is serialized to `.miz`.

```
Mission/
├── DCSMission.cs               # Root output object; holds all Lua tables
├── DCSMissionBriefing.cs       # Structured briefing (HTML + plain text)
├── DCSMissionFlightBriefing.cs # Per-flight-group briefing data
├── DCSMissionPackage.cs        # Package (multi-flight) briefing
├── DCSMissionState.cs          # Serializable state for rollback/retry
├── DCSCampaign.cs              # Campaign = ordered list of DCSMission
└── DCSLuaObjects/              # Lua table models
    ├── DCSGroup.cs             # A DCS group (air/ground/ship)
    ├── DCSUnit.cs              # A single unit within a group
    ├── DCSWaypoint.cs          # A waypoint
    ├── DCSWaypointTask.cs      # Task attached to a waypoint
    └── DCSWrappedWaypointTask.cs
```

---

### `Library/` — Shared Utilities

```
Library/
├── Toolbox.cs          # Math, random, collection helpers; global RNG
├── Coordinates.cs      # Lat/lon ↔ DCS X/Y conversions
├── INIFile.cs          # INI file parser
├── INIFileSection.cs   # Single INI section
├── LanguageString.cs   # Localised string wrapper
├── MizMaker.cs         # Serialises DCSMission → .miz zip archive
├── ShapeManager.cs     # Theater boundary shapes (GeoJSON)
├── BRPaths.cs          # Canonical file-system paths
├── MinMaxD/I.cs        # Clamped numeric range value objects
└── RadioChannel.cs     # Radio frequency value object
```

---

## Frontends

### Web — `src/Web/`
Blazor Server SPA.

| File | Role |
|------|------|
| `Program.cs` / `Startup.cs` | DI setup, middleware, `IBriefingRoom` singleton |
| `App.razor` | Blazor root component |
| `Controllers/GeneratorController.cs` | REST endpoint — file download of generated `.miz` |
| `Pages/_Host.cshtml` | Blazor host page |

### Desktop — `src/Desktop/`
WinForms window hosting a Blazor WebView (same UI as Web).

| File | Role |
|------|------|
| `Program.cs` | Entry point; sets up WinForms window |
| `BriefingRoomBlazorWrapper.cs` | WinForms form containing the WebView |
| `App.razor` | Blazor root (shared with Web) |

### CommandLine — `src/CommandLine/`
Minimal CLI that calls `IBriefingRoom.GenerateMission` / `GenerateCampaign` and writes the `.miz` to disk.

---

## Data Files

### `Database/` — INI Configuration

| Path | Content |
|------|---------|
| `Common.ini` | Shared settings (unit spacing, radio…) |
| `AirDefense.ini` | SAM/AAA density levels |
| `CAP.ini` | CAP intercept settings |
| `Briefing.ini` | Briefing phrase fragments |
| `Names.ini` | Unit/waypoint name pools |
| `Wind.ini` | Wind speed tables |
| `Coalitions/` | Per-coalition unit lists and defaults |
| `Theaters/` | Theater metadata (spawn zones, airbases…) |
| `ObjectiveTargets/` | Target type definitions |
| `ObjectiveTasks/` | Task type definitions |
| `MissionFeatures/` | Mission-wide feature scripts |
| `ObjectiveFeatures/` | Per-objective feature scripts |
| `ObjectivePresets/` | Preset objective bundles |
| `Language/` | Translation keys in multiple languages |
| `WeatherPresets/` | Preset weather profiles |

### `DatabaseJSON/` — JSON Data

| File | Content |
|------|---------|
| `UnitPlanes.json` | Fixed-wing aircraft definitions |
| `UnitHelicopters.json` | Rotary-wing definitions |
| `UnitShips.json` | Naval unit definitions |
| `UnitCars.json` | Ground vehicle definitions |
| `UnitFortifications.json` | Static fortification definitions |
| `UnitCargo.json` | Cargo unit definitions |
| `UnitHeliports.json` | Heliport definitions |
| `UnitWarehouses.json` | Warehouse definitions |
| `TheatersAirbases.json` | Airbase coordinates per theater |
| `TheaterSpawnPoints/` | Per-theater spawn point grids |
| `TheaterTerrainBounds/` | Theater boundary polygons |
| `TheaterTemplateLocations/` | Named strategic locations per theater |
| `Templates.json` | Built-in mission templates |
| `Layouts.json` | Mission layout presets |
| `WeaponsByDate.json` | Weapon availability by decade |
| `Situations/` | Pre-defined tactical situation data |

---

## Lua Scripts — `Include/Lua/`

Embedded into the `.miz` at generation time.

```
Include/Lua/
├── Mission.lua             # Core mission bootstrap
├── Dictionary.lua          # DCS string dictionary entries
├── MapResource.lua         # Map resource declarations
├── Options.lua             # DCS option flags
├── Warehouses.lua          # Airbase warehouse data
├── Mission/
│   ├── Script/             # Per-stage mission scripts (triggers, scoring…)
│   ├── Drawing/            # Map drawings
│   ├── TrigRules/          # Trigger rules Lua
│   ├── WaypointPlayer.lua  # Player waypoint logic
│   └── Zone.lua            # Trigger zones
├── MissionFeatures/        # Optional feature scripts (CTLD, CSAR, Skynet IADS…)
│   ├── CTLD.lua
│   ├── CSAR.lua
│   ├── SkynetIni.lua / skynet-iads-compiled.lua
│   ├── EWRS.lua
│   ├── Moose_ATIS.lua
│   └── … (30+ feature scripts)
├── ObjectiveFeatures/      # Per-objective scripts
└── ObjectiveTriggers/      # Objective completion trigger Lua
```

---

## Tests — `src/Tests/`

| File | What it tests |
|------|---------------|
| `SmokeTests.cs` | End-to-end generation of representative missions |
| `DeterminismTests.cs` | Same seed → identical mission output |
| `MissionStateTests.cs` | Rollback/retry state serialization |
| `MapDataContractTests.cs` | Map data API contracts |
| `DBFixture.cs` | Shared `IBriefingRoom` fixture for all tests |

---

## Key Data-Flow Summary

```
User Template (.brt / .cbrt)
        │
        ▼
  BriefingRoom.GenerateMission()
        │
        ├─► Database  ──────────────────────────────────┐
        │   (INI + JSON → DBEntry objects)               │
        │                                                 │
        ▼                                                 │
  MissionGenerator.Generate()                            │
        │  (10-stage pipeline)                           │
        │  Each stage reads Database ◄───────────────────┘
        │  and writes into DCSMission
        │
        ▼
  DCSMission (in-memory Lua object tree)
        │
        ▼
  MizMaker.Build() → .miz (zip archive)
        │
        ▼
  Returned to frontend (Web download / CLI file write / Desktop save dialog)
```

---

## CI/CD — `.github/workflows/`

| Workflow | Trigger | What it does |
|----------|---------|--------------|
| `build.yml` | Push / PR | Build + run tests |
| `release.yml` | Tag push | Publish EXE + Docker image + GitHub Release |
| `docker.yml` | Manual / tag | Build & push Docker image |
