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

using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS.Generator
{
    internal class MissionGeneratorOptions
    {

        internal static void GenerateForcedOptions(ref DCSMission mission, MissionTemplateRecord template)
        {
            string forcedOptionsLua = "";

            foreach (RealismOption realismOption in template.OptionsRealism)
            {
                switch (realismOption)
                {
                    case RealismOption.BirdStrikes: forcedOptionsLua += "[\"birds\"] = 300,"; break;
                    case RealismOption.HideLabels: forcedOptionsLua += "[\"labels\"] = 0,"; break;
                    case RealismOption.NoBDA: forcedOptionsLua += "[\"RBDAI\"] = false,\r\n"; break;
                    case RealismOption.NoCheats: forcedOptionsLua += "[\"immortal\"] = false, [\"fuel\"] = false, [\"weapons\"] = false,"; break;
                    case RealismOption.NoCrashRecovery: forcedOptionsLua += "[\"permitCrash\"] = false,"; break;
                    case RealismOption.NoEasyComms: forcedOptionsLua += "[\"easyCommunication\"] = false,"; break;
                    case RealismOption.NoExternalViews: forcedOptionsLua += "[\"externalViews\"] = false,"; break;
                    case RealismOption.NoGameMode: forcedOptionsLua += "[\"easyFlight\"] = false, [\"easyRadar\"] = false,"; break;
                    case RealismOption.NoOverlays: forcedOptionsLua += "[\"miniHUD\"] = false, [\"cockpitStatusBarAllowed\"] = false,"; break;
                    case RealismOption.NoPadlock: forcedOptionsLua += "[\"padlock\"] = false,"; break;
                    case RealismOption.RandomFailures: forcedOptionsLua += "[\"accidental_failures\"] = true,"; break;
                    case RealismOption.RealisticGEffects: forcedOptionsLua += "[\"geffect\"] = \"realistic\","; break;
                    case RealismOption.WakeTurbulence: forcedOptionsLua += "[\"wakeTurbulence\"] = true,"; break;
                }
            }

            // Some realism options are forced OFF when not explicitely enabled
            if (!template.OptionsRealism.Contains(RealismOption.BirdStrikes))
                forcedOptionsLua += "[\"birds\"] = 0,";
            else if (!template.OptionsRealism.Contains(RealismOption.RandomFailures))
                forcedOptionsLua += "[\"accidental_failures\"] = false,";
            else if (!template.OptionsRealism.Contains(RealismOption.NoBDA))
                forcedOptionsLua += "[\"RBDAI\"] = true,";

            forcedOptionsLua += $"[\"civTraffic\"] = \"{(template.OptionsMission.Contains("EnableCivilianTraffic") ? "medium" : "false")}\",";
            forcedOptionsLua += $"[\"radio\"] = {(template.OptionsRealism.Contains(RealismOption.DisableDCSRadioAssists) ? "false" : "true")},";

            forcedOptionsLua += template.OptionsFogOfWar switch
            {
                FogOfWar.AlliesOnly => "[\"optionsView\"] = \"optview_onlyallies\",",
                FogOfWar.KnownUnitsOnly => "[\"optionsView\"] = \"optview_allies\",",
                FogOfWar.SelfOnly => "[\"optionsView\"] = \"optview_myaircraft\",",
                FogOfWar.None => "[\"optionsView\"] = \"optview_onlymap\",",
                _ => "[\"optionsView\"] = \"optview_all\",",
            };
            mission.SetValue("ForcedOptions", forcedOptionsLua);
        }
    }
}
