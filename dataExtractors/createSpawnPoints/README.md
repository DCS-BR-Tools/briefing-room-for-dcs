# Create Spawn Points - Data Extractor (AI Generated)

## Overview

This Node.js application generates DCS (Digital Combat Simulator) mission files that visualize spawn points for different theater maps. It takes spawn point coordinate data and generates a mission file with ground units placed at those locations, organized by spawn point type and clustered for better visualization in the DCS mission editor.

## Purpose

The tool is designed to:
- Visualize spawn point locations on DCS theater maps
- Generate test missions that can be opened in the DCS Mission Editor
- Help validate spawn point data by displaying them as ground units and colored circles
- Organize large numbers of spawn points into manageable clusters

## Prerequisites

- Node.js installed
- npm (Node Package Manager)

## Installation

```bash
npm install
```

This will install the required dependencies:
- `@turf/clusters-kmeans` - K-means clustering algorithm for geospatial data
- `@turf/helpers` - Helper functions for creating GeoJSON objects

## Usage

```bash
npm start <theater-name>
```

or

```bash
node app.js <theater-name>
```

### Example

```bash
npm start Caucasus
```

This will:
1. Read spawn point data from `../../DatabaseJSON/TheaterSpawnPoints/Caucasus.json.gz`
2. Process and cluster the spawn points
3. Generate a mission file at `./out/mission.lua`

## How It Works

### 1. Input Data Loading

The application reads three template files:
- **emptyMission.lua** - A complete DCS mission template with placeholder variables
- **unit.lua** - Template for a ground unit group
- **point.lua** - Template for individual waypoints in a unit's route

It also reads spawn point data from the theater-specific JSON file located at:
```
../../DatabaseJSON/TheaterSpawnPoints/<theater-name>.json
```

### 2. Spawn Point Classification

Spawn points are categorized into three types (based on the `BRtype` property):
- **LandSmall** - Small land spawn areas (visualized in Red)
- **LandMedium** - Medium land spawn areas (visualized in Gray)
- **LandLarge** - Large land spawn areas (visualized in Blue)

### 3. K-Means Clustering

Due to DCS limitations on the number of units per mission, spawn points are clustered using the K-means algorithm:
- Points are grouped into clusters of approximately 50 points each
- Number of clusters = `Math.ceil(total_points / 50)`
- This prevents overwhelming the mission editor with too many individual units

### 4. Mission Generation Process

The app performs the following transformations:

1. **Theater Assignment**: Replaces `$THEATER$` placeholder with the actual theater name
2. **Map Centering**: Sets the mission editor's initial view to the first LandLarge spawn point
3. **Unit Group Creation**: For each cluster of spawn points:
   - Creates a ground unit group (using a Grad FDDM vehicle as a placeholder)
   - Assigns unique group and unit IDs
   - Creates waypoints for each spawn point in the cluster
   
4. **Placeholder Replacement**: Populates the mission template with generated units:
   - `$LandSmall$` - Replaced with all small spawn point unit groups (Red coalition)
   - `$LandMedium$` - Replaced with all medium spawn point unit groups (Neutral coalition)
   - `$LandLarge$` - Replaced with all large spawn point unit groups (Blue coalition)

### 5. Output

The final mission file is written to `./out/mission.lua` and can be opened directly in the DCS Mission Editor.

## File Structure

```
createSpawnPoints/
├── app.js                 # Main application logic
├── emptyMission.lua       # DCS mission template
├── unit.lua               # Ground unit group template
├── point.lua              # Waypoint template
├── package.json           # Node.js dependencies
├── package-lock.json      # Locked dependency versions
└── out/                   # Output directory (generated)
    └── mission.lua        # Generated mission file
```

## Template Variables

### emptyMission.lua
- `$THEATER$` - Theater/map name (e.g., "Caucasus", "Nevada", "Syria")
- `$MAPX$` - X coordinate for mission editor centering
- `$MAPY$` - Y coordinate for mission editor centering
- `$LandSmall$` - Placeholder for small spawn point units
- `$LandMedium$` - Placeholder for medium spawn point units
- `$LandLarge$` - Placeholder for large spawn point units

### unit.lua
- `$IDX$` - Local index within the spawn point type
- `$GLOBIDX$` - Global index across all unit groups
- `$X$` - X coordinate of the unit group
- `$Y$` - Y coordinate of the unit group
- `$POINTS$` - Waypoint list for the route

### point.lua
- `$IDX$` - Waypoint index
- `$X$` - X coordinate of the waypoint
- `$Y$` - Y coordinate of the waypoint
- `$FIRST$` - Boolean indicating if this is the first waypoint (for ETA locking)

## Visualization in DCS

When you open the generated mission in DCS Mission Editor, you'll see:

1. **Ground Units**: Each cluster represented by a Grad FDDM vehicle with a route showing all spawn points
2. **Colored Circles** (on the "Author" drawing layer):
   - **Red circles** (14.8m radius) - Small spawn points
   - **Gray circles** (92.5m radius) - Medium spawn points
   - **Blue circles** (277.5m radius) - Large spawn points

## Notes

- The app uses Grad FDDM vehicles as placeholder units - these are just for visualization
- If a spawn point type has no data, it defaults to using the first LandLarge spawn point
- The clustering algorithm ensures no single mission becomes too large for DCS to handle
- Generated missions are read-only templates for validation purposes

## Troubleshooting

**Error: Cannot find spawn point file**
- Ensure the theater name is spelled correctly and matches a file in `DatabaseJSON/TheaterSpawnPoints/`
- Theater names are case-sensitive

**Mission won't load in DCS**
- Check that the theater/map is installed in your DCS installation
- Verify the output file at `./out/mission.lua` was generated successfully

**No output file created**
- Ensure the `out/` directory exists (create it manually if needed)
- Check file permissions for write access
