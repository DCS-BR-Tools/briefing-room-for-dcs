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

using System.Collections.Generic;

namespace BriefingRoom4DCS.Data
{
    internal class DBEntryDCSMod : DBEntry
    {
        internal static List<string> CORE_MODS = new List<string>{
            "World War II AI Units by Eagle Dynamics",
            "Characters",
            "Animals",
            "TAVKR 1143 High Detail",
            "RailwayObjectsPack",
            "South_Atlantic_Assets",
            "TechWeaponPack",
            "USS_Nimitz",
            "A-10 Warthog",
            "AH-64D BLK.II AI",
            "AJS37 AI by Heatblur Simulations",
            "AV-8B N/A AI by RAZBAM Sims",
            "AircraftWeaponPack",
            "C-101 Aviojet by AvioDev",
            "China Asset Pack by Deka Ironwork Simulations and Eagle Dynamics",
            "Christen Eagle II AI by Magnitude 3 LLC",
            "F-16C bl.50 AI",
            "F-5E/E-3 by Belsimtek",
            "F-86F Sabre AI by Eagle Dynamics",
            "F-14B AI by Heatblur Simulations",
            "F/A-18C AI",
            "Hawk T.1A AI by VEAO Simulations",
            "I-16 AI by OctopusG",
            "Ka-50 Black Shark",
            "L-39C/ZA by Eagle Dynamics",
            "M-2000C AI by RAZBAM Sims",
            "MB-339A/PAN AI by IndiaFoxtEcho",
            "MQ-9 Reaper AI",
            "Mi-24P AI by Eagle Dynamics",
            "MiG-15bis AI by Eagle Dynamics",
            "MiG-19P AI by RAZBAM",
            "MiG-21Bis AI by Magnitude 3 LLC",
            "./CoreMods/aircraft/Mirage-F1",
            "SA342 AI by Polychop-Simulations",
            "Su-34 AI",
            "./CoreMods/aircraft/Yak-52",
            "CaptoGloveSupport",
            "LeapMotionSupport",
            "SensoryxVRFreeSupport",
            "VoiceChat",
            "jsAvionics",
            "F-15E AI by RAZBAM",
            "HeavyMetalCore",
            "Massun92-Assetpack",
            "F-4E AI by Heatblur Simulations",
            "OH58D AI by Polychop-Simulations",
            "CH-47F bl.1 AI",
            "Yak-52 AI by Eagle Dynamics",
            "F4U-1D AI by Magnitude 3 LLC",
            "Mirage F1 Assets by Aerges",
            "Currenthill Assets Pack",
            "MiG-29 Fulcrum AI",
            "C-130J AI",
            "C-130-Assets",
            "ColdWarAssetsPack",
            "A-6E AI by Heatblur Simulations"
        };

        internal string Module { get; private set; }
        protected override bool OnLoad(string iniFilePath)
        {
            var ini = new INIFile(iniFilePath);
            Module = ini.GetValue<string>("Module", "Module");
            return true;
        }
    }
}
