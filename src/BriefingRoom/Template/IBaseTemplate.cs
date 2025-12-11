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

using System.Collections.Generic;
using BriefingRoom4DCS.Data;

namespace BriefingRoom4DCS.Template
{
    public interface IBaseTemplate
    {
        public static readonly int MAX_PLAYER_FLIGHT_GROUPS;
        public static readonly int MAX_OBJECTIVE_DISTANCE;
        public static readonly int MAX_OBJECTIVE_SEPARATION;
        public static readonly int MAX_BORDER_LIMIT;
        public static readonly int MIN_BORDER_LIMIT;
        public static readonly int MAX_COMBINED_ARMS_SLOTS;
        public string ContextCoalitionBlue { get; set; }
        public string ContextCoalitionRed { get; set; }
        public Decade ContextDecade { get; set; }
        public Coalition ContextPlayerCoalition { get; set; }
        public string ContextTheater { get; set; }
        public string ContextSituation { get; set; }
        public int FlightPlanObjectiveDistanceMax { get; set; }
        public int FlightPlanObjectiveDistanceMin { get; set; }
        public string FlightPlanTheaterStartingAirbase { get; set; }
        public string FlightPlanTheaterDestinationAirbase { get; set; }
        public List<string> MissionFeatures { get; set; }
        public List<string> Mods { get; set; }
        public FogOfWar OptionsFogOfWar { get; set; }
        public List<string> OptionsMission { get; set; }
        public List<RealismOption> OptionsRealism { get; set; }
        public List<string> OptionsUnitBanList { get; set; }
        public List<MissionTemplateFlightGroup> PlayerFlightGroups { get; set; }
        public AmountR SituationEnemySkill { get; set; }
        public AmountNR SituationEnemyAirDefense { get; set; }
        public AmountNR SituationEnemyAirForce { get; set; }
        public AmountR SituationFriendlySkill { get; set; }
        public AmountNR SituationFriendlyAirDefense { get; set; }
        public AmountNR SituationFriendlyAirForce { get; set; }
        public int CombinedArmsCommanderBlue { get; set; }
        public int CombinedArmsCommanderRed { get; set; }
        public int CombinedArmsJTACBlue { get; set; }
        public int CombinedArmsJTACRed { get; set; }
        public DsAirbase AirbaseDynamicSpawn { get; set; }
        public bool DSAllowHotStart { get; set; }

        public void Clear(IDatabase database) {}

    }
}
