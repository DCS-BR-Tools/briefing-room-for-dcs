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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BriefingRoom4DCS.Data.JSON;
using BriefingRoom4DCS.Mission;
using Newtonsoft.Json;

namespace BriefingRoom4DCS.Data
{
    internal class DBEntrySituation : DBEntry
    {

        internal List<List<Coordinates>> RedZones { get; set; }

        internal List<List<Coordinates>> BlueZones { get; set; }

        internal List<List<Coordinates>> NoSpawnZones { get; set; }

        internal string Theater { get; private set; }

        internal List<LanguageString> BriefingDescriptions { get; private set; }

        internal List<string> RelatedSituations { get; private set; }

        protected override bool OnLoad(string o)
        {
            throw new NotImplementedException();
        }
        internal static Dictionary<string, DBEntry> LoadJSON(string filepath, DatabaseLanguage LangDB)
        {
            var situation = JsonConvert.DeserializeObject<Situation>(File.ReadAllText(filepath));
            var id = Path.GetFileNameWithoutExtension(filepath);
            var className = GetLanguageClassName(typeof(DBEntrySituation));
            return new Dictionary<string, DBEntry>{
                {id, new DBEntrySituation{
                    ID = id,
                    UIDisplayName = new LanguageString(LangDB, className, id, "DisplayName",   situation.DisplayName),
                    BriefingDescriptions = situation.BriefingDescriptions.Select(x => new LanguageString(LangDB, className, id, "BriefingDescription",x)).ToList(),
                    Theater = situation.Theater.ToLower(),
                    RelatedSituations = situation.RelatedSituations,
                    RedZones = situation.redZones.Select(x => x.Select(y => new Coordinates(y)).ToList()).ToList(),
                    BlueZones = situation.blueZones.Select(x => x.Select(y => new Coordinates(y)).ToList()).ToList(),
                    NoSpawnZones = situation.noSpawnZones.Select(x => x.Select(y => new Coordinates(y)).ToList()).ToList(),
                }}
            };
        }

        internal List<List<Coordinates>> GetRedZones(bool invertCoalition) => !invertCoalition ? RedZones : BlueZones;
        internal List<List<Coordinates>> GetBlueZones(bool invertCoalition) => !invertCoalition ? BlueZones : RedZones;

        internal List<DBEntryAirbase> GetAirbases(bool invertCoalition)
        {
            var airbases = (from DBEntryAirbase airbase in Database.Instance.GetAllEntries<DBEntryAirbase>()
                            where Toolbox.StringICompare(airbase.Theater, Theater)
                            select airbase).ToList();
            foreach (var airbase in airbases)
            {
                airbase.Coalition = Coalition.Neutral;
                if (ShapeManager.IsPosValid(airbase.Coordinates, GetBlueZones(invertCoalition)))
                    airbase.Coalition = Coalition.Blue;
                if (ShapeManager.IsPosValid(airbase.Coordinates, GetRedZones(invertCoalition)))
                    airbase.Coalition = Coalition.Red;
            }

            return airbases;
        }
    }
}
