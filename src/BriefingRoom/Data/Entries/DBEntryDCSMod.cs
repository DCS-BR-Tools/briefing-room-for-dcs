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
            "./CoreMods/aircraft/Mirage-F1",
            "./CoreMods/aircraft/Yak-52",
            "A-10 Warthog",
            "A-6E AI by Heatblur Simulations",
            "AH-64D BLK.II AI",
            "AircraftWeaponPack",
            "AJS37 AI by Heatblur Simulations",
            "Animals",
            "AV-8B N/A AI by RAZBAM Sims",
            "C-101 Aviojet by AvioDev",
            "C-130-Assets",
            "C-130J AI",
            "CaptoGloveSupport",
            "CH-47F bl.1 AI",
            "Characters",
            "China Asset Pack by Deka Ironwork Simulations and Eagle Dynamics",
            "Christen Eagle II AI by Magnitude 3 LLC",
            "ColdWarAssetsPack",
            "Currenthill Assets Pack",
            "F-100D AI by GrinnelliDesigns",
            "F-14B AI by Heatblur Simulations",
            "F-14B AI by Heatblur Simulations",
            "F-15E AI by RAZBAM",
            "F-16C bl.50 AI",
            "F-4E AI by Heatblur Simulations",
            "F-5E/E-3 by Belsimtek",
            "F-86F Sabre AI by Eagle Dynamics",
            "F/A-18C AI",
            "F4U-1D AI by Magnitude 3 LLC",
            "Hawk T.1A AI by VEAO Simulations",
            "HeavyMetalCore",
            "I-16 AI by OctopusG",
            "jsAvionics",
            "Ka-50 Black Shark",
            "L-39C/ZA by Eagle Dynamics",
            "La-7 AI by OctopusG",
            "LeapMotionSupport",
            "M-2000C AI by RAZBAM Sims",
            "Massun92-Assetpack",
            "MB-339A/PAN AI by IndiaFoxtEcho",
            "Mi-24P AI by Eagle Dynamics",
            "MiG-15bis AI by Eagle Dynamics",
            "MiG-19P AI by RAZBAM",
            "MiG-21Bis AI by Magnitude 3 LLC",
            "MiG-29 Fulcrum AI",
            "Mirage F1 Assets by Aerges",
            "MQ-9 Reaper AI",
            "OH58D AI by Polychop-Simulations",
            "RailwayObjectsPack",
            "SA342 AI by Polychop-Simulations",
            "SensoryxVRFreeSupport",
            "South_Atlantic_Assets",
            "Su-34 AI",
            "TAVKR 1143 High Detail",
            "TechWeaponPack",
            "USS_Nimitz",
            "VoiceChat",
            "World War II AI Units by Eagle Dynamics",
            "Yak-52 AI by Eagle Dynamics",
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
