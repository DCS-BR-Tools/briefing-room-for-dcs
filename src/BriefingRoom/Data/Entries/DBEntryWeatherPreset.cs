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

namespace BriefingRoom4DCS.Data
{
    internal class DBEntryWeatherPreset : DBEntry
    {
        internal bool BadWeather { get; private set; }

        internal LanguageString BriefingDescription { get; private set; }

        internal MinMaxI CloudsBase { get; private set; }

        internal string[] CloudsPresets { get; private set; }

        internal MinMaxI CloudsThickness { get; private set; }

        internal bool Dust { get; private set; }

        internal MinMaxI DustDensity { get; private set; }

        internal bool Fog { get; private set; }

        internal MinMaxI FogThickness { get; private set; }

        internal MinMaxI FogVisibility { get; private set; }

        internal MinMaxI QNH { get; private set; }

        internal MinMaxI Turbulence { get; private set; }

        internal MinMaxI Visibility { get; private set; }


        protected override bool OnLoad(string iniFilePath)
        {
            var ini = new INIFile(iniFilePath);
            var className = this.GetLanguageClassName();
            BriefingDescription = ini.GetLangStrings(Database.Language, className, ID, "Briefing", "Description");

            CloudsBase = ini.GetValue<MinMaxI>("Weather", "Clouds.Base");
            CloudsPresets = ini.GetValueArray<string>("Weather", "Clouds.Presets");
            CloudsThickness = ini.GetValue<MinMaxI>("Weather", "Clouds.Thickness");

            Dust = ini.GetValue<bool>("Weather", "Dust");
            DustDensity = ini.GetValue<MinMaxI>("Weather", "Dust.Density");

            Fog = ini.GetValue<bool>("Weather", "Fog");
            FogThickness = ini.GetValue<MinMaxI>("Weather", "Fog.Thickness");
            FogVisibility = ini.GetValue<MinMaxI>("Weather", "Fog.Visibility");

            BadWeather = ini.GetValue<bool>("Weather", "IsBadWeather");
            QNH = ini.GetValue<MinMaxI>("Weather", "QNH");
            Turbulence = ini.GetValue<MinMaxI>("Weather", "Turbulence");
            Visibility = ini.GetValue<MinMaxI>("Weather", "Visibility");

            return true;
        }
    }
}
