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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BriefingRoom4DCS.Template
{
    public sealed class CampaignTemplate : BaseTemplate, IBaseTemplate
    {
        private static readonly int MIN_CAMPAIGN_MISSIONS = Database.Instance.Common.MinCampaignMissions;
        private static readonly int MAX_CAMPAIGN_MISSIONS = Database.Instance.Common.MaxCampaignMissions;
        public string BriefingCampaignName { get; set; }
        public Amount EnvironmentBadWeatherChance { get; set; }
        public Amount EnvironmentNightMissionChance { get; set; }
        public int MissionsCount { get { return MissionsCount_; } set { MissionsCount_ = Toolbox.Clamp(value, MIN_CAMPAIGN_MISSIONS, MAX_CAMPAIGN_MISSIONS); } }
        private int MissionsCount_ = MIN_CAMPAIGN_MISSIONS;
        public CampaignDifficultyVariation MissionsDifficultyVariation { get; set; }
        public List<string> MissionsObjectives { get { return MissionObjectives_; } set { MissionObjectives_ = Database.Instance.CheckIDs<DBEntryObjectivePreset>(value.ToArray()).Distinct().ToList(); } }
        private List<string> MissionObjectives_ = new();
        public Amount MissionsObjectiveCount { get; set; }
        public Amount MissionsObjectiveVariationDistance { get; set; }
        public AmountN MissionsAirbaseVariationDistance { get; set; }
        public Amount MissionTargetCount { get; set; }
        public AmountNR MissionsProgression { get; set; }
        public bool StaticSituation { get; set; }

        public CampaignTemplate()
        {
            Clear();
        }

        public CampaignTemplate(string filePath)
        {
            LoadFromFile(filePath);
        }

        public new void Clear()
        {

            base.Clear();

            BriefingCampaignName = "";

            EnvironmentBadWeatherChance = Amount.VeryLow;
            EnvironmentNightMissionChance = Amount.VeryLow;

            MissionsCount = 5;
            MissionsDifficultyVariation = CampaignDifficultyVariation.Random;
            MissionsObjectives = BriefingRoom.GetDatabaseEntriesIDs(DatabaseEntryType.ObjectivePreset).ToList();
            MissionsObjectiveCount = Amount.Average;
            MissionsObjectiveVariationDistance = Amount.Average;
            MissionsAirbaseVariationDistance = AmountN.Average;
            MissionTargetCount = Amount.Average;
            StaticSituation = false;
            MissionsProgression = AmountNR.None;

            AssignAliases();

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

        public bool LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException(filePath);

            return Load(new INIFile(filePath));
        }

        public bool LoadFromString(string data)
        {
            return Load(INIFile.CreateFromRawINIString(data));
        }

        internal new bool Load(INIFile ini)
        {
            base.Load(ini);
            BriefingCampaignName = ini.GetValue("Briefing", "CampaignName", BriefingCampaignName);

            EnvironmentBadWeatherChance = ini.GetValue("Environment", "BadWeatherChance", EnvironmentBadWeatherChance);
            EnvironmentNightMissionChance = ini.GetValue("Environment", "NightMissionChance", EnvironmentBadWeatherChance);

            MissionsCount = ini.GetValue("Missions", "Count", MissionsCount);
            MissionsDifficultyVariation = ini.GetValue("Missions", "DifficultyVariation", MissionsDifficultyVariation);
            MissionsObjectives = ini.GetValueList<string>("Missions", "Objectives");
            MissionsObjectiveCount = ini.GetValue("Missions", "ObjectiveCount", MissionsObjectiveCount);
            MissionsObjectiveVariationDistance = ini.GetValue("Missions", "ObjectiveVariationDistance", MissionsObjectiveVariationDistance);
            MissionsAirbaseVariationDistance = ini.GetValue("Missions", "AirbaseVariationDistance", MissionsAirbaseVariationDistance);
            MissionTargetCount = ini.GetValue("Missions", "TargetCount", MissionTargetCount);
            StaticSituation = ini.GetValue("CampaignOptions", "StaticSituation", false);
            MissionsProgression = ini.GetValue("Missions", "Progression", MissionsProgression);

            PlayerFlightGroups.Clear();
            foreach (string key in ini.GetTopLevelKeysInSection("PlayerFlightGroups"))
                PlayerFlightGroups.Add(new MissionTemplateFlightGroup(ini, "PlayerFlightGroups", key));

            AssignAliases();
            return true;
        }

        public void SaveToFile(string filePath)
        {
            var ini = GetAsIni();
            ini.SaveToFile(filePath);
        }

        public byte[] GetIniBytes()
        {
            var ini = GetAsIni();
            return Encoding.ASCII.GetBytes(ini.GetFileData());
        }

        internal new INIFile GetAsIni()
        {
            var ini = base.GetAsIni();
            ini.SetValue("Briefing", "CampaignName", BriefingCampaignName);

            ini.SetValue("Environment", "BadWeatherChance", EnvironmentBadWeatherChance);
            ini.SetValue("Environment", "NightMissionChance", EnvironmentBadWeatherChance);

            ini.SetValue("Missions", "Count", MissionsCount);
            ini.SetValue("Missions", "DifficultyVariation", MissionsDifficultyVariation);
            ini.SetValueArray("Missions", "Objectives", MissionsObjectives.ToArray());
            ini.SetValue("Missions", "ObjectiveCount", MissionsObjectiveCount);
            ini.SetValue("Missions", "ObjectiveVariationDistance", MissionsObjectiveVariationDistance);
            ini.SetValue("Missions", "AirbaseVariationDistance", MissionsAirbaseVariationDistance);
            ini.SetValue("Missions", "TargetCount", MissionTargetCount);
            ini.SetValue("CampaignOptions", "StaticSituation", StaticSituation);
            ini.SetValue("Missions", "Progression", MissionsProgression);

            AssignAliases();
            return ini;
        }

        public string GetCoalition(Coalition coalition)
        {
            if (coalition == Coalition.Red) return ContextCoalitionRed;
            return ContextCoalitionBlue;
        }
    }
}
