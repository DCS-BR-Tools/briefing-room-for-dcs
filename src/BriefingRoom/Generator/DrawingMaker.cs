using BriefingRoom4DCS.Mission;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BriefingRoom4DCS.Generator
{
    internal static class DrawingMaker
    {

        internal static void AddDrawing(
            ref DCSMission mission,
            string UIName,
            DrawingType type,
            Coordinates coordinates,
            params KeyValuePair<string, object>[] extraSettings)
        {
            switch (type)
            {
                case DrawingType.Free:
                    AddFree(ref mission, UIName, coordinates, extraSettings);
                    break;
                case DrawingType.Circle:
                    AddOval(ref mission, UIName, coordinates, extraSettings);
                    break;
                case DrawingType.TextBox:
                    AddTextBox(ref mission, UIName, coordinates, extraSettings);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), $"Not expected drawing value: {type}");

            };
        }

        private static void AddFree(ref DCSMission mission, string UIName, Coordinates coordinates, params KeyValuePair<string, object>[] extraSettings)
        {
            string drawingLuaTemplate = File.ReadAllText(Path.Combine(BRPaths.INCLUDE_LUA_MISSION, "Drawing", "Free.lua"));
            string freePosLuaTemplate = File.ReadAllText(Path.Combine(BRPaths.INCLUDE_LUA_MISSION, "Drawing", "FreePos.lua"));

            var points = "";
            var index = 1;
            var templateLua = "";
            foreach (Coordinates pos in (List<Coordinates>)extraSettings.First(x => x.Key == "Points").Value)
            {
                templateLua = new String(freePosLuaTemplate);
                GeneratorTools.ReplaceKey(ref templateLua, "Index", index);
                GeneratorTools.ReplaceKey(ref templateLua, "X", pos.X);
                GeneratorTools.ReplaceKey(ref templateLua, "Y", pos.Y);
                points += $"{templateLua}\n";
                index++;
            }

            //Make last point first point
            templateLua = new String(freePosLuaTemplate);
            GeneratorTools.ReplaceKey(ref templateLua, "Index", index);
            GeneratorTools.ReplaceKey(ref templateLua, "X", 0);
            GeneratorTools.ReplaceKey(ref templateLua, "Y", 0);
            points += $"{templateLua}\n";

            GeneratorTools.ReplaceKey(ref drawingLuaTemplate, "POINTS", points);
            DrawingColour colour = (DrawingColour)(extraSettings.FirstOrDefault(x => x.Key == "Colour").Value ?? DrawingColour.Red);
            DrawingColour fillColour = (DrawingColour)(extraSettings.FirstOrDefault(x => x.Key == "FillColour").Value ?? DrawingColour.RedFill);
            AddToList(ref mission, UIName, drawingLuaTemplate, coordinates, colour, fillColour);
        }

        private static void AddOval(ref DCSMission mission, string UIName, Coordinates coordinates, params KeyValuePair<string, object>[] extraSettings)
        {
            string drawingLuaTemplate = File.ReadAllText(Path.Combine(BRPaths.INCLUDE_LUA_MISSION, "Drawing", "Circle.lua"));
            GeneratorTools.ReplaceKey(ref drawingLuaTemplate, "Radius", extraSettings.First(x => x.Key == "Radius").Value);
            DrawingColour colour = (DrawingColour)(extraSettings.FirstOrDefault(x => x.Key == "Colour").Value ?? DrawingColour.Red);
            DrawingColour fillColour = (DrawingColour)(extraSettings.FirstOrDefault(x => x.Key == "FillColour").Value ?? DrawingColour.Clear);
            AddToList(ref mission, UIName, drawingLuaTemplate, coordinates, colour, fillColour);
        }

        private static void AddTextBox(ref DCSMission mission, string UIName, Coordinates coordinates, params KeyValuePair<string, object>[] extraSettings)
        {
            string drawingLuaTemplate = File.ReadAllText(Path.Combine(BRPaths.INCLUDE_LUA_MISSION, "Drawing", "TextBox.lua"));
            GeneratorTools.ReplaceKey(ref drawingLuaTemplate, "Text", extraSettings.First(x => x.Key == "Text").Value);
            DrawingColour colour = (DrawingColour)(extraSettings.FirstOrDefault(x => x.Key == "Colour").Value ?? DrawingColour.Black);
            DrawingColour fillColour = (DrawingColour)(extraSettings.FirstOrDefault(x => x.Key == "FillColour").Value ?? DrawingColour.WhiteBackground);
            AddToList(ref mission, UIName, drawingLuaTemplate, coordinates, colour, fillColour);
        }

        private static void AddToList(ref DCSMission mission, string UIName, string template, Coordinates coordinates, DrawingColour colour, DrawingColour fillColour)
        {
            GeneratorTools.ReplaceKey(ref template, "UIName", UIName);
            GeneratorTools.ReplaceKey(ref template, "X", coordinates.X);
            GeneratorTools.ReplaceKey(ref template, "Y", coordinates.Y);
            GeneratorTools.ReplaceKey(ref template, "Colour", colour.ToValue());
            GeneratorTools.ReplaceKey(ref template, "FillColour", fillColour.ToValue());
            mission.LuaDrawings.Add(template);
        }

        internal static void AddTheaterZones(ref DCSMission mission)
        {
            DrawWaterAndIslands(ref mission);
            if (mission.TemplateRecord.OptionsMission.Contains("SpawnAnywhere") || mission.TemplateRecord.ContextSituation == "None")
                return;
            var invertCoalition = mission.TemplateRecord.OptionsMission.Contains("InvertCountriesCoalitions");
            var hideBorders = mission.TemplateRecord.OptionsMission.Contains("HideBorders");
            var red = mission.SituationDB.GetRedZones(invertCoalition);
            var redColour = hideBorders ? DrawingColour.Clear : DrawingColour.RedFill;

            var blue = mission.SituationDB.GetBlueZones(invertCoalition);
            var blueColour = hideBorders ? DrawingColour.Clear : DrawingColour.BlueFill;
            foreach (var zone in red)
            {
                AddFree(
                    ref mission,
                    "Red Control",
                    zone.First(),
                    "Points".ToKeyValuePair(zone.Select(coord => coord - zone.First()).ToList()),
                    "Colour".ToKeyValuePair(redColour),
                    "FillColour".ToKeyValuePair(redColour));
                mission.MapData.Add($"RED_{red.IndexOf(zone)}", zone.Select(x => x.ToArray()).ToList());
            }
            foreach (var zone in blue)
            {

                AddFree(
                    ref mission,
                    "Blue Control",
                    zone.First(),
                    "Points".ToKeyValuePair(zone.Select(coord => coord - zone.First()).ToList()),
                    "Colour".ToKeyValuePair(blueColour),
                    "FillColour".ToKeyValuePair(blueColour));
                mission.MapData.Add($"BLUE_{blue.IndexOf(zone)}", zone.Select(x => x.ToArray()).ToList());
            }

            if (mission.SituationDB.NoSpawnZones.Count > 0)
            {
                var noSpawn = mission.SituationDB.NoSpawnZones;
                var noSpawnColour = hideBorders ? DrawingColour.Clear : DrawingColour.GreenFill;
                foreach (var zone in noSpawn)
                {
                    AddFree(
                        ref mission,
                        "Neutural (NoSpawning)",
                        zone.First(),
                        "Points".ToKeyValuePair(zone.Select(coord => coord - zone.First()).ToList()),
                        "Colour".ToKeyValuePair(noSpawnColour),
                        "FillColour".ToKeyValuePair(noSpawnColour));
                    mission.MapData.Add($"NOSPAWN_{noSpawn.IndexOf(zone)}", zone.Select(x => x.ToArray()).ToList());
                }
            }

        }

        private static void DrawWaterAndIslands(ref DCSMission mission)
        {
            // DEBUG water
            var i = 0;
            foreach (var item in mission.TheaterDB.WaterCoordinates)
            {
                AddFree(
                    ref mission,
                    "Water",
                    item.First(),
                    "Points".ToKeyValuePair(item.Select(coord => coord - item.First()).ToList()),
                    "Colour".ToKeyValuePair(DrawingColour.Clear),
                    "FillColour".ToKeyValuePair(DrawingColour.Clear));
                mission.MapData.Add($"WATER_{i}", item.Select(x => x.ToArray()).ToList());
                i++;
            }
            i = 0;
            foreach (var item in mission.TheaterDB.WaterExclusionCoordinates)
            {
                AddFree(
                    ref mission,
                    "Water Exclusion",
                    item.First(),
                    "Points".ToKeyValuePair(item.Select(coord => coord - item.First()).ToList()),
                    "Colour".ToKeyValuePair(DrawingColour.Clear),
                    "FillColour".ToKeyValuePair(DrawingColour.Clear));
                mission.MapData.Add($"ISLAND_{i}", item.Select(x => x.ToArray()).ToList());
                i++;
            }
        }

        internal static string GetLuaDrawings(ref DCSMission mission)
        {
            string luaDrawings = "";

            var index = 1;
            foreach (var drawing in mission.LuaDrawings)
            {
                var drawingLua = drawing;
                GeneratorTools.ReplaceKey(ref drawingLua, "Index", index);
                luaDrawings += $"{drawingLua}\n";
                index++;
            }
            return luaDrawings;
        }
    }
}