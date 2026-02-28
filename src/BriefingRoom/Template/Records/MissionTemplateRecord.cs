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
using System.Linq;
using BriefingRoom4DCS.Data;

namespace BriefingRoom4DCS.Template
{
    internal sealed record MissionTemplateRecord
    {
        internal string BriefingMissionName { get; init; }
        internal string BriefingMissionDescription { get; init; }
        internal string ContextCoalitionBlue { get; init; }
        internal string ContextCoalitionRed { get; init; }
        internal Decade ContextDecade { get; init; }
        internal Coalition ContextPlayerCoalition { get; init; }
        internal string ContextTheater { get; init; }
        internal string ContextSituation { get; init; }
        internal bool ContextSituationIgnoresFrontLine { get; init; }
        internal bool ContextSituationIgnoresCombatZones { get; init; }
        internal Season EnvironmentSeason { get; init; }
        internal TimeOfDay EnvironmentTimeOfDay { get; init; }
        internal string EnvironmentWeatherPreset { get; init; }
        internal Wind EnvironmentWind { get; init; }
        internal MinMaxD FlightPlanObjectiveDistance { get; init; }
        internal MinMaxD FlightPlanObjectiveSeparation { get; init; }
        internal int BorderLimit { get; init; }
        internal string FlightPlanTheaterStartingAirbase { get; init; }
        internal string FlightPlanTheaterDestinationAirbase { get; init; }
        internal List<string> MissionFeatures { get; init; }
        internal List<string> Mods { get; init; }
        internal List<MissionTemplateObjectiveRecord> Objectives { get; init; }
        internal FogOfWar OptionsFogOfWar { get; init; }
        internal List<string> OptionsMission { get; init; }
        internal List<RealismOption> OptionsRealism { get; init; }
        internal List<string> OptionsUnitBanList { get; init; }
        internal List<MissionTemplateFlightGroupRecord> PlayerFlightGroups { get; init; }
        internal List<MissionTemplatePackageRecord> AircraftPackages { get; init; }
        internal AmountR SituationEnemySkill { get; init; }
        internal AmountNR SituationEnemyAirDefense { get; init; }
        internal AmountNR SituationEnemyAirForce { get; init; }
        internal AmountR SituationFriendlySkill { get; init; }
        internal AmountNR SituationFriendlyAirDefense { get; init; }
        internal AmountNR SituationFriendlyAirForce { get; init; }
        internal int CombinedArmsCommanderBlue { get; init; }
        internal int CombinedArmsCommanderRed { get; init; }
        internal int CombinedArmsJTACBlue { get; init; }
        internal int CombinedArmsJTACRed { get; init; }
        internal bool SpawnAnywhere { get; init; }
        internal Dictionary<string, double[]> CarrierHints { get; init; }
        internal MissionTemplate Template { get; init; }
        internal DsAirbase AirbaseDynamicSpawn { get; init; }
        internal bool CarrierDynamicSpawn { get; init; }
        internal bool DsAllowHotStart { get; init; }

        internal DsAirbase AirbaseDynamicCargo { get; init; }
        internal bool CarrierDynamicCargo { get; init; }

        internal MissionTemplateRecord(IDatabase database, MissionTemplate template)
        {
            BriefingMissionName = template.BriefingMissionName;
            BriefingMissionDescription = template.BriefingMissionDescription;
            ContextCoalitionBlue = template.ContextCoalitionBlue;
            ContextCoalitionRed = template.ContextCoalitionRed;
            ContextDecade = template.ContextDecade;
            ContextPlayerCoalition = template.ContextPlayerCoalition;
            ContextTheater = template.ContextTheater;
            ContextSituation = template.ContextSituation;
            ContextSituationIgnoresFrontLine = template.ContextSituationIgnoresFrontLine;
            ContextSituationIgnoresCombatZones = template.ContextSituationIgnoresCombatZones;
            EnvironmentSeason = template.EnvironmentSeason;
            EnvironmentTimeOfDay = template.EnvironmentTimeOfDay;
            EnvironmentWeatherPreset = template.EnvironmentWeatherPreset;
            EnvironmentWind = template.EnvironmentWind;
            FlightPlanObjectiveDistance = new MinMaxD(template.FlightPlanObjectiveDistanceMin, template.FlightPlanObjectiveDistanceMax);
            FlightPlanObjectiveSeparation = new MinMaxD(template.FlightPlanObjectiveSeparationMin, template.FlightPlanObjectiveSeparationMax);
            BorderLimit = template.BorderLimit;
            FlightPlanTheaterStartingAirbase = template.FlightPlanTheaterStartingAirbase;
            FlightPlanTheaterDestinationAirbase = template.FlightPlanTheaterDestinationAirbase;
            Mods = GetMods(database, template);
            MissionFeatures = template.MissionFeatures;
            Objectives = template.Objectives.Select(x => new MissionTemplateObjectiveRecord(x)).ToList();
            OptionsFogOfWar = template.OptionsFogOfWar;
            OptionsMission = template.OptionsMission;
            OptionsRealism = template.OptionsRealism;
            OptionsUnitBanList = template.OptionsUnitBanList;
            PlayerFlightGroups = template.PlayerFlightGroups.Select((x, index) =>
            {
                if (((ContextPlayerCoalition == Coalition.Red && !x.Hostile) || (ContextPlayerCoalition == Coalition.Blue && x.Hostile)) && x.Country == Country.CombinedJointTaskForcesBlue)
                    x.Country = Country.CombinedJointTaskForcesRed;
                if (((ContextPlayerCoalition == Coalition.Red && x.Hostile) || (ContextPlayerCoalition == Coalition.Blue && !x.Hostile)) && x.Country == Country.CombinedJointTaskForcesRed)
                    x.Country = Country.CombinedJointTaskForcesBlue;

                return new MissionTemplateFlightGroupRecord(x, index);
            }).ToList();
            AircraftPackages = template.AircraftPackages.Select(x => new MissionTemplatePackageRecord(x)).ToList();
            SituationEnemySkill = template.SituationEnemySkill;
            SituationEnemyAirDefense = template.SituationEnemyAirDefense;
            SituationEnemyAirForce = template.SituationEnemyAirForce;
            SituationFriendlySkill = template.SituationFriendlySkill;
            SituationFriendlyAirDefense = template.SituationFriendlyAirDefense;
            SituationFriendlyAirForce = template.SituationFriendlyAirForce;

            CombinedArmsCommanderBlue = template.CombinedArmsCommanderBlue;
            CombinedArmsCommanderRed = template.CombinedArmsCommanderRed;
            CombinedArmsJTACBlue = template.CombinedArmsJTACBlue;
            CombinedArmsJTACRed = template.CombinedArmsJTACRed;
            SpawnAnywhere = template.OptionsMission.Contains("SpawnAnywhere") || template.ContextSituation == "None";
            CarrierHints = template.CarrierHints;
            Template = template;
            AirbaseDynamicSpawn = template.AirbaseDynamicSpawn;
            CarrierDynamicSpawn = template.CarrierDynamicSpawn;
            DsAllowHotStart = template.DSAllowHotStart;
            AirbaseDynamicCargo = template.AirbaseDynamicCargo;
            CarrierDynamicCargo = template.CarrierDynamicCargo;
        }

        internal string GetCoalitionID(Coalition coalition)
        {
            if (coalition == Coalition.Red) return ContextCoalitionRed;
            return ContextCoalitionBlue;
        }

        internal string GetCoalitionID(Side side)
        {
            return GetCoalitionID((side == Side.Ally) ? ContextPlayerCoalition : ContextPlayerCoalition.GetEnemy());
        }

        internal int GetPlayerSlotsCount()
        {
            return PlayerFlightGroups.Aggregate(0, (acc, x) => acc + (x.AIWingmen ? 1 : x.Count));
        }

        private static List<string> GetMods(IDatabase database, MissionTemplate template)
        {   
            var selectedMods = template.Mods.Select(x => database.GetEntry<DBEntryDCSMod>(x).Module);
            return template.PlayerFlightGroups
             .Select(x => database.GetEntry<DBEntryJSONUnit>(x.Aircraft).Module)
             .Where(x => !string.IsNullOrEmpty(x) && !DBEntryDCSMod.CORE_MODS.Contains(x)).Concat(selectedMods).Distinct().ToList();
        }
    }
}
