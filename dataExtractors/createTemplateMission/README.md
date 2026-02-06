# Create Template Mission - Data Extractor

## Overview

This Node.js application generates DCS (Digital Combat Simulator) mission files from location templates for different theater maps. It loads location template data from JSON files and creates a mission file with ground units placed at those locations, organized by group and unit type for visualization in the DCS mission editor.

## Purpose

The tool is designed to:
- Visualize location template objects on DCS theater maps
- Generate test missions that can be opened in the DCS Mission Editor
- Help validate location template data by displaying them as ground units
- Organize locations into groups for easier visualization

## Prerequisites

- Node.js installed

## Installation
No installation required. All dependencies are built-in.

## Usage

```bash
node app.js <theater-name>
```

### Example

```bash
node app.js SinaiMap
```

This will:
1. Read location template data from `../../DatabaseJSON/TheaterTemplateLocations/SinaiMap.json`
2. Create all the groups and units
3. Generate a mission file at `./out/mission.lua`

## How It Works

### 1. Input Data Loading

The application reads three template files:
- **emptyMission.lua** - A complete DCS mission template with placeholder variables
- **unit.lua** - Template for a ground unit
- **group.lua** - Template for a group of units

It reads location template data from the theater-specific JSON file located at:
```
../../DatabaseJSON/TheaterTemplateLocations/<theater-name>.json
```

### 2. Group and Unit Generation

For each location template in the JSON file:
- A group is created at the template's coordinates
- Each location in the template's `locations` array becomes a unit, placed relative to the group
- Unit heading and type are set from the JSON data
- Unique group and unit indices are assigned

### 3. Mission Generation Process

The app performs the following transformations:
1. **Theater Assignment**: Replaces `$THEATER$` placeholder with the actual theater name
2. **Map Centering**: Sets the mission editor's initial view to the first location template
3. **Group Creation**: For each template, creates a group and its units
4. **Placeholder Replacement**: Populates the mission template with generated groups and units:
   - `$GROUPS$` - Replaced with all generated groups

### 4. Output

The final mission file is written to `./out/mission.lua` and can be opened directly in the DCS Mission Editor.

## File Structure

```
createTemplateMission/
├── app.js                 # Main application logic
├── emptyMission.lua       # DCS mission template
├── unit.lua               # Ground unit template
├── group.lua              # Group template
├── package.json           # Node.js dependencies
├── package-lock.json      # Locked dependency versions
└── out/                   # Output directory (generated)
   └── mission.lua        # Generated mission file
```

## Template Variables

### emptyMission.lua
- `$THEATER$` - Theater/map name (e.g., "SinaiMap", "Caucasus")
- `$MAPX$` - X coordinate for mission editor centering (first template location)
- `$MAPY$` - Y coordinate for mission editor centering (first template location)
- `$GROUPS$` - Placeholder for all generated groups

### group.lua
- `$X$` - X coordinate of the group
- `$Y$` - Y coordinate of the group
- `$LOCTYPE$` - Location type
- `$GLOBIDX$` - Global group index
- `$IDX$` - Local group index
- `$UNITS$` - Placeholder for all units in the group

### unit.lua
- `$X$` - X coordinate of the unit (relative to group)
- `$Y$` - Y coordinate of the unit (relative to group)
- `$HEADING$` - Heading of the unit
- `$GLOBIDX$` - Global group index
- `$TYPE$` - Unit type
- `$IDX$` - Unit index within the group

## Visualization in DCS

When you open the generated mission in DCS Mission Editor, you'll see:

1. **Ground Units**: Each location template is represented as a group, with units placed at their relative coordinates and headings
2. **Unit Types**: Units are assigned their original type from the JSON data

## Notes

- The app places units at their relative coordinates and headings for visualization
- All generated missions are templates for validation purposes

## Troubleshooting

**Error: Cannot find location template file**
- Ensure the theater name is spelled correctly and matches a file in `DatabaseJSON/TheaterTemplateLocations/`
- Theater names are case-sensitive

**Mission won't load in DCS**
- Check that the theater/map is installed in your DCS installation
- Verify the output file at `./out/mission.lua` was generated successfully

**No output file created**
- Ensure the `out/` directory exists (create it manually if needed)
- Check file permissions for write access
