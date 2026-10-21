# Local aircraft imports

Aircraft JSON loading builds a `LocalAircraftFiles` index of the configured DCS
Saved Games directory once per aircraft data file. No DCS installation files are
scanned, and user Lua is never executed.

- `Liveries/<aircraft>/` supports exact unit IDs (case-insensitive), loose livery
  directories containing `description.lua`, and ZIP archives.
- DCS livery folder names are not always unit IDs. `LiveryAliases` in
  `Data/LocalAircraftFiles.cs` maps known folder names to unit IDs, currently
  `A-10CII` to `A-10C_2`. Exact and alias folders are merged without duplicate
  livery entries. The original folder name remains searchable as well as its
  mapped unit ID. Additional mismatches require an explicit alias; folder names
  alone cannot reliably identify an aircraft.
- `MissionEditor/UnitPayloads/*.lua` files are indexed by both their filename
  and their `unitType` assignment. Standard DCS quoted-key and bare-key
  assignments on their own line are supported. Both lookups are additive:
  metadata does not replace filename matching, even when the two disagree.
  Files without metadata still match by filename. A path is imported only once
  per aircraft when both names match, including case-only differences.

Built-in liveries and payloads still come from the aircraft JSON database.
Changing the Saved Games path resets the database and rebuilds the index when
aircraft are next loaded. Files added while running are picked up on database
reload or application restart.
