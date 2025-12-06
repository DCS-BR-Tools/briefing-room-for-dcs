using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Generator.UnitMaker;
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

        internal static int AddZone(
            ref DCSMission mission, 
            string UIName,
            Coordinates coordinates, int radius) => AddToList(ref mission, UIName, coordinates, radius);

        internal static void AddCTLDPickupZone(IBriefingRoom briefingRoom, ref DCSMission mission, Coordinates coordinates, bool onTop = false)
        {
            var coords = coordinates;
            if (!onTop)
            {
                var spawnPoints = new List<SpawnPointType> { SpawnPointType.LandLarge }.ToArray();
                Coordinates? newCoords = SpawnPointSelector.GetNearestSpawnPoint(mission, spawnPoints, coordinates);
                if (!newCoords.HasValue)
                    throw new BriefingRoomException(briefingRoom.Database, mission.LangKey, "Can't find suitable zone Coordinates!");
                coords = newCoords.Value;
                DrawingMaker.AddDrawing(ref mission, $"Supply_{mission.CTLDZoneCount}", DrawingType.TextBox, coords, "Text".ToKeyValuePair($"Supply Base"));
                DrawingMaker.AddDrawing(ref mission, $"Supply_zone_{mission.CTLDZoneCount}", DrawingType.Circle, coords, "Radius".ToKeyValuePair(500), "Colour".ToKeyValuePair(DrawingColour.White));
                mission.MapData.Add($"SUPPLY_{mission.CTLDZoneCount}", new List<double[]> { coords.ToArray() });
                var group = UnitGenerator.AddUnitGroup( briefingRoom, ref mission, UnitFamily.StaticStructureMilitary, 1, Side.Ally, "Static", "Static", coords, GroupFlags.Inert, new Dictionary<string, object>(), true);
                group.Value.DCSGroup.Units[0].Name = $"logistic{mission.CTLDZoneCount}";
            }
            AddToList(ref mission, $"pickzone{mission.CTLDZoneCount}", coords, 500);
            mission.CTLDZoneCount++;
        }

        internal static void AddAirbaseZones(IBriefingRoom briefingRoom,ref DCSMission mission, List<string> missionFeatures, DBEntryAirbase homeBase, List<DCSMissionStrikePackage> missionPackages)
        {
            if (!missionFeatures.Contains("CTLD"))
                return;
            AddCTLDPickupZone(briefingRoom, ref mission, homeBase.Coordinates);
            foreach (var package in missionPackages)
                AddCTLDPickupZone(briefingRoom, ref mission, package.Airbase.Coordinates);

        }


        private static int AddToList(ref DCSMission mission, string UIName, Coordinates coordinates, int radius)
        { 
            var zoneId = new Random().Next(100, 500);
            if(mission.LuaZones.Any(x => x.Contains($"[\"zoneId\"] = {zoneId},")))
                zoneId = new Random().Next(500, 900);
            string template = File.ReadAllText(Path.Combine(BRPaths.INCLUDE_LUA_MISSION, "Zone.lua"));
            GeneratorTools.ReplaceKey(ref template, "NAME", UIName);
            GeneratorTools.ReplaceKey(ref template, "RADIUS", radius);
            GeneratorTools.ReplaceKey(ref template, "X", coordinates.X);
            GeneratorTools.ReplaceKey(ref template, "Y", coordinates.Y);
            GeneratorTools.ReplaceKey(ref template, "zoneId", zoneId);
            mission.LuaZones.Add(template);
            return zoneId;
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