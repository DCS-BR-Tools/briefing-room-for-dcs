/*
==========================================================================
This file is part of Briefing Room for DCS World, a mission
generator for DCS World, by @akaAgar (https://github.com/akaAgar/briefing-room-for-dcs)

Briefing Room for DCS World is free software: you can redistribute it
and/or modify it under the terms of the GNU General Public License
as published by the Free Software Foundation, either version 3 of
the License, or (at your option) any later version.

Briefing Room for DCS World is distributed in the hope that it will
be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Briefing Room for DCS World. If not, see https://www.gnu.org/licenses/
==========================================================================
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BriefingRoom4DCS.Data.JSON;
using BriefingRoom4DCS.Template;
using LuaTableSerializer;
using Newtonsoft.Json;

namespace BriefingRoom4DCS.Data
{
    internal class DBEntryAircraft : DBEntryJSONUnit
    {
        internal List<DCSTask> Tasks { get; init; }
        internal double Fuel { get; init; }
        internal int Flares { get; init; }
        internal int Chaff { get; init; }
        internal int? AmmoType { get; init; }
        internal int MaxAlt { get; init; }
        internal int CruiseAlt { get { return (int)Math.Floor(MaxAlt * 0.6); } }
        internal double CruiseSpeed { get; init; }
        internal bool PlayerControllable { get; init; }
        internal RadioChannel Radio { get; init; }
        internal List<DBEntryUnitRadioPreset> PanelRadios { get; init; }
        internal Dictionary<string, object> ExtraProps { get; init; }
        internal bool EPLRS { get; init; }
        internal Dictionary<Country, List<string>> CallSigns { get; init; }
        internal List<Payload> Payloads { get; init; }
        internal int MinimumRunwayLengthFt { get; init; }

        internal Dictionary<string, object> PayloadCommon
        {
            get
            {
                var dict = new Dictionary<string, object>{
                {"flare", Flares},
                {"chaff", Chaff},
                {"gun", 100},
                {"fuel", this.Category == UnitCategory.Helicopter ?  Fuel * 0.6 : Fuel}
                };
                if (AmmoType.HasValue)
                    dict.Add("ammo_type", AmmoType.Value);
                return dict;
            }
        }

        internal double Height { get; init; }
        internal double Width { get; init; }
        internal double Length { get; init; }

        protected override bool OnLoad(string o)
        {
            throw new NotImplementedException();
        }

        internal static Dictionary<string, DBEntry> LoadJSON(string filepath, DatabaseLanguage LangDB)
        {
            var itemMap = new Dictionary<string, DBEntry>(StringComparer.InvariantCulture);
            var data = JsonConvert.DeserializeObject<List<Aircraft>>(File.ReadAllText(filepath));
            var supportData = JsonConvert.DeserializeObject<List<BRInfo>>(File.ReadAllText($"{filepath.Replace(".json", "")}BRInfo.json")).ToDictionary(x => x.type, x => x);

            foreach (var aircraft in data)
            {
                var id = aircraft.type;
                if (!supportData.ContainsKey(id))
                {
                    BriefingRoom.PrintToLog($"Aircraft missing {aircraft.module} {id} info data.", LogMessageErrorLevel.Warning);
                    continue;
                }
                var supportInfo = supportData[id];


                bool modAircraft = !string.IsNullOrEmpty(aircraft.module) && !DBEntryDCSMod.CORE_MODS.Contains(aircraft.module);
                var DBaircraft = new DBEntryAircraft
                {
                    ID = id,
                    UIDisplayName = new LanguageString(LangDB, GetLanguageClassName(typeof(DBEntryAircraft)), id, "displayName", modAircraft ? $"{aircraft.displayName} [{aircraft.module}] " : aircraft.displayName),
                    DCSID = aircraft.type,
                    Liveries = aircraft.paintSchemes.ToDictionary(pair => (Country)Enum.Parse(typeof(Country), pair.Key.Replace(" ", ""), true), pair => pair.Value),
                    Operators = GetOperationalCountries(aircraft),
                    Module = aircraft.module,
                    Tasks = aircraft.tasks.Where(x => x is not null).Select(x => (DCSTask)x.WorldID).ToList(),
                    Fuel = aircraft.fuel,
                    Flares = aircraft.flares,
                    Chaff = aircraft.chaff,
                    AmmoType = aircraft.ammoType,
                    MaxAlt = (int)aircraft.maxAlt,
                    CruiseSpeed = aircraft.cruiseSpeed,
                    Radio = new RadioChannel(aircraft.radio.frequency, (RadioModulation)aircraft.radio.modulation),
                    PanelRadios = (aircraft.panelRadio ?? new List<PanelRadio>()).Select(radio =>
                    {
                        return new DBEntryUnitRadioPreset(radio.channels.Select(x => x.@default).ToArray(), radio.channels.Select(x =>
                        {
                            var modulation = RadioModulation.AM;
                            if (!string.IsNullOrEmpty(x.modulation) && x.modulation != "AM/FM")
                                modulation = (RadioModulation)Enum.Parse(typeof(RadioModulation), x.modulation, true);
                            return (int)modulation;
                        }).ToArray(), RadioType.Unknown);
                    }).ToList(),
                    ExtraProps = (aircraft.extraProps ?? new List<ExtraProp>()).Where(x => x.defValue is not null).ToDictionary(x => x.id, x => x.defValue),
                    EPLRS = (bool)(aircraft.EPLRS ?? false),
                    CallSigns = aircraft.callsigns.ToDictionary(x => (Country)Enum.Parse(typeof(Country), x.Key, true), x => x.Value.Select(v => $"{v.WorldID}:{v.Name}").ToList()),
                    Payloads = aircraft.payloadPresets,
                    Shape = aircraft.shape,
                    Height = aircraft.height,
                    Width = aircraft.width,
                    Length = aircraft.length,
                    PlayerControllable = supportInfo.playerControllable,
                    Families = supportInfo.families.Select(x => (UnitFamily)Enum.Parse(typeof(UnitFamily), x, true)).ToArray(),
                    LowPolly = supportInfo.lowPolly
                };
                DBaircraft.GetDCSPayloads();
                DBaircraft.GetDCSLiveries();
                itemMap.Add(id, DBaircraft);

            }

            
            missingDCSDataWarnings(supportData, itemMap, "Aircraft");


            return itemMap;
        }

        public DBEntryAircraft() {}

        internal Dictionary<int, Dictionary<string, object>> GetPylonsObject(string aircraftPayload)
        {
            var payload = Payloads.Find(x => x.name == aircraftPayload);
            if (payload == null)
                return new Dictionary<int, Dictionary<string, object>>();
            return payload.pylons.ToDictionary(x => x.num, x => new Dictionary<string, object> { { "CLSID", x.CLSID  }, {"settings", x.settings} });
        }

        internal Dictionary<int, Dictionary<string, object>> GetPylonsObject(DCSTask task)
        {
            if (Payloads.Count == 0)
                return new Dictionary<int, Dictionary<string, object>>();
            var payload = Toolbox.RandomFrom(Payloads.Where(x => x.tasks.Contains((int)task)).ToList()) ?? Toolbox.RandomFrom(Payloads);
            return payload.pylons.Where(x => x != null).ToDictionary(x => x.num, x => new Dictionary<string, object> { { "CLSID", x.CLSID }, {"settings", x.settings} });
        }

        internal void GetDCSPayloads()
        {
            var userPath = Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
            string folderPath;
            if (Directory.Exists(Path.Join(userPath, "Saved Games", "DCS.openbeta")))
            {
                folderPath = Path.Join(userPath, "Saved Games", "DCS.openbeta", "MissionEditor", "UnitPayloads");
            }
            else
            {
                folderPath = Path.Join(userPath, "Saved Games", "DCS", "MissionEditor", "UnitPayloads");
            }
            if (!File.Exists(Path.Join(folderPath, $"{DCSID}.lua")))
                return;

            var fileText = File.ReadAllText(Path.Join(folderPath, $"{DCSID}.lua"))
                .Replace("local unitPayloads = ", "")
                .Replace("return unitPayloads", "");
            try
            {
                var obj = LuaSerializer.Deserialize(fileText);
                foreach (var item in (IDictionary)obj["payloads"])
                {
                    var itemEntry = (IDictionary)((DictionaryEntry)item).Value;

                    var tasks = new List<int?>();
                    foreach (var taskItem in ((IDictionary)itemEntry["tasks"]).Values)
                        tasks.Add((int)(long)taskItem);

                    var pylons = new List<Pylon>();
                    foreach (var pylonItem in ((IDictionary)itemEntry["pylons"]).Values)
                    {
                        var pylonItemEntry = (IDictionary)pylonItem;
                        var pylon = new Pylon
                        {
                            CLSID = (string)pylonItemEntry["CLSID"],
                            num = (int)(long)pylonItemEntry["num"],
                        };

                        if (pylonItemEntry.Contains("settings"))
                        {
                            pylon.settings = new Dictionary<string, object>();
                            foreach (var settingItem in ((IDictionary)pylonItemEntry["settings"]))
                            {
                                var settingKV = (DictionaryEntry)settingItem;
                                pylon.settings.Add((string)settingKV.Key, settingKV.Value);
                            }

                        }
                        pylons.Add(pylon);
                    }

                    var payload = new Payload
                    {
                        name = (string)itemEntry["name"],
                        displayName = (string)(itemEntry.Contains("displayName") ? itemEntry["displayName"] : itemEntry["name"]),
                        tasks = tasks,
                        pylons = pylons,
                    };

                    Payloads.Add(payload);

                    BriefingRoom.PrintToLog($"Imported payload {payload.displayName} for {DCSID}");
                }

            }
            catch (System.Exception e)
            {
                BriefingRoom.PrintToLog($"Cannot parse player payloads for {DCSID}. Likely as a payload name isn't happy with our parser, Reccomend you remove any of these characters {{}}/\\: from your custom payload names.", LogMessageErrorLevel.Warning);
                Console.WriteLine(e);
                // Debugging
                // Console.WriteLine(fileText);
            }

        }

        internal void GetDCSLiveries()
        {
            var userPath = Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
            string folderPath;
            if (Directory.Exists(Path.Join(userPath, "Saved Games", "DCS.openbeta")))
            {
                folderPath = Path.Join(userPath, "Saved Games", "DCS.openbeta", "Liveries");
            }
            else
            {
                folderPath = Path.Join(userPath, "Saved Games", "DCS", "Liveries");
            }
            if (!Directory.Exists(Path.Join(folderPath, $"{DCSID}")))
                return;

            foreach (var item in Directory.GetFiles(Path.Join(folderPath, $"{DCSID}"), "*.*", SearchOption.TopDirectoryOnly))
            {
                var rawFileName = item.Replace(".zip", "").Split("\\").Last();
                Liveries.AddIfKeyUnused(Country.ALL, new List<string>());
                if (!Liveries[Country.ALL].Contains(rawFileName))
                {
                    Liveries[Country.ALL].Add(rawFileName);
                    BriefingRoom.PrintToLog($"Imported Livery {rawFileName} for {DCSID}");
                }
            }

            foreach (var item in Directory.GetFiles(Path.Join(folderPath, $"{DCSID}"), "description.lua", SearchOption.AllDirectories))
            {
                var rawFileName = item.Replace("description.lua", "").Split("\\")[^2];
                Liveries.AddIfKeyUnused(Country.ALL, new List<string>());
                if (!Liveries[Country.ALL].Contains(rawFileName))
                {
                    Liveries[Country.ALL].Add(rawFileName);
                    BriefingRoom.PrintToLog($"Imported Livery {rawFileName} for {DCSID}");
                }
            }

        }
    }
}
