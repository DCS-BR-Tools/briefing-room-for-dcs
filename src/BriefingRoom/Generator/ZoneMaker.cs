using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BriefingRoom4DCS.Generator
{
    internal static class ZoneMaker
    {

        internal static void AddZone(
            ref DCSMission mission, 
            string UIName,
            Coordinates coordinates, int radius) => AddToList(ref mission, UIName, coordinates, radius);

        internal static void AddCTLDPickupZone(ref DCSMission mission, Coordinates coordinates, bool onTop = false)
        {
            var coords = coordinates;
            if (!onTop)
            {
                var spawnPoints = new List<SpawnPointType> { SpawnPointType.LandLarge }.ToArray();
                Coordinates? newCoords = UnitMakerSpawnPointSelector.GetNearestSpawnPoint(ref mission, spawnPoints, coordinates);
                if (!newCoords.HasValue)
                    throw new BriefingRoomException(mission.LangKey, "Can't find suitable zone Coordinates!");
                coords = newCoords.Value;
                DrawingMaker.AddDrawing(ref mission, $"Supply_{mission.CTLDZoneCount}", DrawingType.TextBox, coords, "Text".ToKeyValuePair($"Supply Base"));
                DrawingMaker.AddDrawing(ref mission, $"Supply_zone_{mission.CTLDZoneCount}", DrawingType.Circle, coords, "Radius".ToKeyValuePair(500), "Colour".ToKeyValuePair(DrawingColour.White));
                mission.MapData.Add($"SUPPLY_{mission.CTLDZoneCount}", new List<double[]> { coords.ToArray() });
                var group = UnitMaker.AddUnitGroup(ref mission, UnitFamily.StaticStructureMilitary, 1, Side.Ally, "Static", "Static", coords, UnitMakerGroupFlags.Inert, new Dictionary<string, object>(), true);
                group.Value.DCSGroup.Units[0].Name = $"logistic{mission.CTLDZoneCount}";
            }
            AddToList(ref mission, $"pickzone{mission.CTLDZoneCount}", coords, 500);
            mission.CTLDZoneCount++;
        }

        internal static void AddAirbaseZones(ref DCSMission mission, List<string> missionFeatures, DBEntryAirbase homeBase, List<DCSMissionStrikePackage> missionPackages)
        {
            if (!missionFeatures.Contains("CTLD"))
                return;
            AddCTLDPickupZone(ref mission, homeBase.Coordinates);
            foreach (var package in missionPackages)
                AddCTLDPickupZone(ref mission, package.Airbase.Coordinates);

        }


        private static void AddToList(ref DCSMission mission, string UIName, Coordinates coordinates, int radius)
        {
            string template = File.ReadAllText(Path.Combine(BRPaths.INCLUDE_LUA_MISSION, "Zone.lua"));
            GeneratorTools.ReplaceKey(ref template, "NAME", UIName);
            GeneratorTools.ReplaceKey(ref template, "RADIUS", radius);
            GeneratorTools.ReplaceKey(ref template, "X", coordinates.X);
            GeneratorTools.ReplaceKey(ref template, "Y", coordinates.Y);
            GeneratorTools.ReplaceKey(ref template, "zoneId", new Random().Next(100, 500));
            mission.LuaZones.Add(template);
        }

        internal static string GetLuaZones(ref DCSMission mission)
        {
            string luaDrawings = "";

            var index = 1;
            foreach (var zone in mission.LuaZones)
            {
                var zoneLua = zone;
                GeneratorTools.ReplaceKey(ref zoneLua, "Index", index);
                luaDrawings += $"{zoneLua}\n";
                index++;
            }
            return luaDrawings;
        }
    }
}