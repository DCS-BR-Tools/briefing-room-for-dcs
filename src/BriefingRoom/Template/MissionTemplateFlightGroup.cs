/*
==========================================================================
This file is part of Briefing Room for DCS World, a mission
generator for DCS World, by @akaAgar
(https://github.com/akaAgar/briefing-room-for-dcs)

Briefing Room for DCS World is free software: you can redistribute it
and/or modify it under the terms of the GNU General Public License
as published by the Free Software Foundation, either version 3 of
the License, or (at your option) any later version.

Briefing Room for DCS World is distributed in the hope that it will
be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Briefing Room for DCS World.
If not, see https://www.gnu.org/licenses/
==========================================================================
*/

using BriefingRoom4DCS.Data;

namespace BriefingRoom4DCS.Template
{
    public sealed class MissionTemplateFlightGroup : MissionTemplateGroup
    {
        public string Aircraft { get { return Aircraft_; } set { Aircraft_ = Database.Instance.CheckID<DBEntryJSONUnit>(value, "Su-25T"); } }
        private string Aircraft_;
        public bool AIWingmen { get; set; }
        public bool Hostile { get; set; }
        public string Carrier
        {
            get { return Carrier_; }
            set
            {
                Carrier_ = Database.Instance.CheckID<DBEntryJSONUnit>(value, allowEmptyStr: true);
                if (string.IsNullOrEmpty(Carrier_) && !string.IsNullOrEmpty(value))
                    Carrier_ = Database.Instance.CheckID<DBEntryDCSTemplate>(value, allowEmptyStr: true);
            }
        }
        private string Carrier_;
        public int Count { get { return _Count; } set { _Count = Toolbox.Clamp(value, 1, Toolbox.MAXIMUM_FLIGHT_GROUP_SIZE); } }
        private int _Count = 1;
        public Country Country { get; set; }
        public string Payload { get; set; } = "default";
        public PlayerStartLocation StartLocation { get; set; }
        public int PlayerSlots { get { return AIWingmen ? 1 : _Count; } }
        public string Livery { get; set; } = "default";

        public string OverrideRadioFrequency { get; set; } = "";
        public RadioModulation OverrideRadioBand { get; set; } = RadioModulation.AM;

        public string OverrideCallsignName { get; set; } = "";
        public int OverrideCallsignNumber { get; set; } = 1;

        public MissionTemplateFlightGroup()
        {
            Clear();
        }

        public MissionTemplateFlightGroup(string aircraft, int count, string payload, string carrier, Country country, PlayerStartLocation startLocation, bool aiWingmen, bool hostile, string livery,
            string overrideRadioFrequency,
            RadioModulation overrideRadioBand,
            string overrideCallsignName,
            int overrideCallsignNumber)
        {
            Aircraft = aircraft;
            AIWingmen = aiWingmen;
            Hostile = hostile;
            Count = count;
            Payload = payload;
            Carrier = carrier;
            Country = country;
            StartLocation = startLocation;
            Livery = livery;
            OverrideRadioFrequency = overrideRadioFrequency;
            OverrideRadioBand = overrideRadioBand;
            OverrideCallsignName = overrideCallsignName;
            OverrideCallsignNumber = overrideCallsignNumber;
        }

        internal MissionTemplateFlightGroup(INIFile ini, string section, string key)
        {
            Clear();

            Aircraft = ini.GetValue(section, $"{key}.AircraftType", Aircraft); // TODO: Database.CheckValue<DBPseudoEntryPlayerAircraft>(ini.GetValue(section, $"{key}.AircraftType", Aircraft));
            AIWingmen = ini.GetValue(section, $"{key}.AIWingmen", AIWingmen);
            Hostile = ini.GetValue(section, $"{key}.Hostile", Hostile);
            Carrier = ini.GetValue(section, $"{key}.Carrier", Carrier);
            Count = ini.GetValue(section, $"{key}.Count", Count);
            Payload = ini.GetValue(section, $"{key}.Payload", Payload);
            Country = ini.GetValue(section, $"{key}.Country", Country);
            StartLocation = ini.GetValue(section, $"{key}.StartLocation", StartLocation);
            Livery = ini.GetValue(section, $"{key}.Livery", Livery);
            OverrideRadioFrequency = ini.GetValue(section, $"{key}.OverrideRadioFrequency", OverrideRadioFrequency);
            OverrideRadioBand = ini.GetValue(section, $"{key}.OverrideRadioBand", OverrideRadioBand);
            OverrideCallsignName = ini.GetValue(section, $"{key}.OverrideCallsignName", OverrideCallsignName);
            OverrideCallsignNumber = ini.GetValue(section, $"{key}.OverrideCallsignNumber", OverrideCallsignNumber);
        }

        private void Clear()
        {
            Aircraft = "Su-25T"; // Database.CheckValue<DBPseudoEntryPlayerAircraft>("Su-25T", "Su-25T");
            AIWingmen = false;
            Hostile = false;
            Carrier = "";
            Count = 2;
            Payload = "default";
            Country = Country.CombinedJointTaskForcesBlue;
            StartLocation = PlayerStartLocation.Runway;
            Livery = "default";
            OverrideRadioFrequency = "";
            OverrideRadioBand = RadioModulation.AM;
            OverrideCallsignName = "";
            OverrideCallsignNumber = 1;
        }

        internal void SaveToFile(INIFile ini, string section, string key)
        {
            ini.SetValue(section, $"{key}.AircraftType", Aircraft);
            ini.SetValue(section, $"{key}.AIWingmen", AIWingmen);
            ini.SetValue(section, $"{key}.Hostile", Hostile);
            ini.SetValue(section, $"{key}.Carrier", Carrier);
            ini.SetValue(section, $"{key}.Count", Count);
            ini.SetValue(section, $"{key}.Payload", Payload);
            ini.SetValue(section, $"{key}.Country", Country);
            ini.SetValue(section, $"{key}.StartLocation", StartLocation);
            ini.SetValue(section, $"{key}.Livery", Livery);
            ini.SetValue(section, $"{key}.OverrideRadioFrequency", OverrideRadioFrequency);
            ini.SetValue(section, $"{key}.OverrideRadioBand", OverrideRadioBand);
            ini.SetValue(section, $"{key}.OverrideCallsignName", OverrideCallsignName);
            ini.SetValue(section, $"{key}.OverrideCallsignNumber", OverrideCallsignNumber);
        }

    }
}
