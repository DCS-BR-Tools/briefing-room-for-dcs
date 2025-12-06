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

using BriefingRoom4DCS.Template;
using System.Collections.Generic;
using System.IO;

namespace BriefingRoom4DCS.Data
{
    public class DatabaseCommon
    {
        internal string[] CommonOGG { get; private set; }
        public int MaxCombinedArmsSlots { get; private set; }
        public int MaxPlayerFlightGroups { get; private set; }
        public int MaxObjectives { get; private set; }
        public int MaxObjectiveDistance { get; private set; }
        public int MaxObjectiveSeparation { get; private set; }
        public int MaxTransportDistance { get; private set; }
        public int MinBorderLimit { get; private set; }
        public int MaxBorderLimit { get; private set; }
        public int MinCampaignMissions { get; private set; }
        public int MaxCampaignMissions { get; private set; }
        public int DropOffDistanceMeters { get; private set; }
        public Dictionary<Amount, MinMaxI> HoldTimesInMinutes { get; private set; }
        public Dictionary<Amount, MinMaxI> HoldSizesInNauticalMiles { get; private set; }
        internal DBCommonAirDefense AirDefense { get; private set; }
        internal DBCommonCAP CAP { get; private set; }
        internal DBCommonCarrierGroup CarrierGroup { get; private set; }
        internal DBCommonNames Names { get; private set; }
        internal DBCommonWind[] Wind { get; private set; }
        internal DBCommonBriefing Briefing { get; private set; }
        internal DBCommonFrontLine FrontLine { get; private set; }
        

        internal DatabaseCommon() { }

        internal void Load(DatabaseLanguage LangDB)
        {
            int i;

            BriefingRoom.PrintToLog("Loading common global settings...");
            INIFile commonIni = new(Path.Combine(BRPaths.DATABASE, "Common.ini"));
            CommonOGG = commonIni.GetValueArray<string>("Include", "CommonOgg");
            MaxCombinedArmsSlots = commonIni.GetValue<int>("Limits", "MaxCombinedArmsSlots");
            MaxPlayerFlightGroups = commonIni.GetValue<int>("Limits", "MaxPlayerFlightGroups");
            MaxObjectives = commonIni.GetValue<int>("Limits", "MaxObjectives");
            MaxObjectiveDistance = commonIni.GetValue<int>("Limits", "MaxObjectiveDistance");
            MaxObjectiveSeparation = commonIni.GetValue<int>("Limits", "MaxObjectiveSeparation");
            MaxTransportDistance = commonIni.GetValue<int>("Limits", "MaxTransportDistance");
            MinBorderLimit = commonIni.GetValue<int>("Limits", "MinBorderLimit");
            MaxBorderLimit = commonIni.GetValue<int>("Limits", "MaxBorderLimit");
            MinCampaignMissions = commonIni.GetValue<int>("Limits", "MinCampaignMissions");
            MaxCampaignMissions = commonIni.GetValue<int>("Limits", "MaxCampaignMissions");
            DropOffDistanceMeters = commonIni.GetValue<int>("Limits", "DropOffDistanceMeters");

            HoldTimesInMinutes = new Dictionary<Amount, MinMaxI>();
            foreach (Amount amount in Toolbox.GetEnumValues<Amount>())
                HoldTimesInMinutes[amount] = commonIni.GetValue<MinMaxI>("HoldTimeMinutes", amount.ToString());

            HoldSizesInNauticalMiles = new Dictionary<Amount, MinMaxI>();
            foreach (Amount amount in Toolbox.GetEnumValues<Amount>())
                HoldSizesInNauticalMiles[amount] = commonIni.GetValue<MinMaxI>("HoldSizeNauticalMiles", amount.ToString());

            foreach (string f in CommonOGG)
                if (!File.Exists(Path.Combine(BRPaths.INCLUDE_OGG, $"{f}.ogg")))
                    BriefingRoom.PrintToLog($"File \"Include\\Ogg\\{f}.ogg\" doesn't exist.", LogMessageErrorLevel.Warning);

            BriefingRoom.PrintToLog("Loading common front line settings...");
            FrontLine = new DBCommonFrontLine();

            BriefingRoom.PrintToLog("Loading common air defense settings...");
            AirDefense = new DBCommonAirDefense();

            BriefingRoom.PrintToLog("Loading common CAP settings...");
            CAP = new DBCommonCAP();

            BriefingRoom.PrintToLog("Loading common carrier group settings...");
            CarrierGroup = new DBCommonCarrierGroup();

            BriefingRoom.PrintToLog("Loading common names settings...");
            Names = new DBCommonNames(LangDB);

            BriefingRoom.PrintToLog("Loading common briefing settings...");
            Briefing = new DBCommonBriefing(LangDB);

            BriefingRoom.PrintToLog("Loading common wind settings...");
            INIFile windIni = new(Path.Combine(BRPaths.DATABASE, "Wind.ini"));
            Wind = new DBCommonWind[Toolbox.EnumCount<Wind>() - 1]; // -1 because we don't want "Random"
            for (i = 0; i < Wind.Length; i++)
                Wind[i] = new DBCommonWind(windIni, ((Wind)i).ToString());
        }
    }
}