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
    public sealed class MissionTemplate : BaseTemplate, IBaseTemplate
    {
        public string BriefingMissionName { get; set; }
        public string BriefingMissionDescription { get; set; }
        public Season EnvironmentSeason { get; set; }
        public TimeOfDay EnvironmentTimeOfDay { get; set; }
        public string EnvironmentWeatherPreset { get { return EnvironmentWeatherPreset_; } set { EnvironmentWeatherPreset_ = Database.CheckID<DBEntryWeatherPreset>(value, allowEmptyStr: true); } }
        private string EnvironmentWeatherPreset_;
        public Wind EnvironmentWind { get; set; }
        public int FlightPlanObjectiveSeparationMax { get { return FlightPlanObjectiveSeparationMax_; } set { FlightPlanObjectiveSeparationMax_ = Toolbox.Clamp(value, 0, Database.Common.MaxObjectiveSeparation); } }
        private int FlightPlanObjectiveSeparationMax_;
        public int FlightPlanObjectiveSeparationMin { get { return FlightPlanObjectiveSeparationMin_; } set { FlightPlanObjectiveSeparationMin_ = Toolbox.Clamp(value, 0, Database.Common.MaxObjectiveSeparation); } }
        private int FlightPlanObjectiveSeparationMin_;
        public int BorderLimit { get { return BorderLimit_; } set { BorderLimit_ = Toolbox.Clamp(value, Database.Common.MinBorderLimit, Database.Common.MaxBorderLimit); } }
        private int BorderLimit_;
        public List<MissionTemplateObjective> Objectives { get; set; } = new List<MissionTemplateObjective>();
        public List<MissionTemplatePackage> AircraftPackages { get { return AircraftPackages_; } set { AircraftPackages_ = value.Take(Database.Common.MaxPlayerFlightGroups).ToList(); } }
        private List<MissionTemplatePackage> AircraftPackages_ = new();
        public Dictionary<string, double[]> CarrierHints { get; set; } = new Dictionary<string, double[]>();

        public MissionTemplate(IDatabase database): base(database)
        {
            Clear();
        }

        public MissionTemplate(IDatabase database, string filePath): base(database)
        {
            Clear();
            LoadFromFile(filePath);
        }
        public new void Clear()
        {
            base.Clear();

            BriefingMissionName = "";
            BriefingMissionDescription = "";

            EnvironmentSeason = Season.Random;
            EnvironmentTimeOfDay = TimeOfDay.RandomDaytime;
            EnvironmentWeatherPreset = "";
            EnvironmentWind = Wind.Random;

            FlightPlanObjectiveSeparationMax = 100;
            FlightPlanObjectiveSeparationMin = 10;
            BorderLimit = 100;

            Objectives = new MissionTemplateObjective[] { new MissionTemplateObjective(Database) }.ToList();
            AircraftPackages = new();
            CarrierHints = new Dictionary<string, double[]>();

            AssignAliases();

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
            BriefingMissionName = ini.GetValue("Briefing", "MissionName", BriefingMissionName);
            BriefingMissionDescription = ini.GetValue("Briefing", "MissionDescription", BriefingMissionDescription).Replace("\\n", "\n");

            EnvironmentSeason = ini.GetValue("Environment", "Season", EnvironmentSeason);
            EnvironmentTimeOfDay = ini.GetValue("Environment", "TimeOfDay", EnvironmentTimeOfDay);
            EnvironmentWeatherPreset = ini.GetValue("Environment", "WeatherPreset", EnvironmentWeatherPreset);
            EnvironmentWind = ini.GetValue("Environment", "Wind", EnvironmentWind);

            FlightPlanObjectiveSeparationMax = ini.GetValue("FlightPlan", "ObjectiveSeparationMax", FlightPlanObjectiveSeparationMax);
            FlightPlanObjectiveSeparationMin = ini.GetValue("FlightPlan", "ObjectiveSeparationMin", FlightPlanObjectiveSeparationMin);
            BorderLimit = ini.GetValue("FlightPlan", "BorderLimit", BorderLimit);

            Objectives.Clear();
            foreach (string key in ini.GetTopLevelKeysInSection("Objectives"))
                Objectives.Add(new MissionTemplateObjective(Database, ini, "Objectives", key));


            AircraftPackages.Clear();
            foreach (string key in ini.GetTopLevelKeysInSection("AircraftPackages"))
                AircraftPackages.Add(new MissionTemplatePackage(Database, ini, "AircraftPackages", key));

            CarrierHints.Clear();
            foreach (string key in ini.GetTopLevelKeysInSection("CarrierHints"))
            {
                CarrierHints.Add(ini.GetValue("CarrierHintsNames", key, ""), ini.GetValue("CarrierHints", key, new double[] { 0, 0 }));
            }
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
            int i;
            var ini = base.GetAsIni();

            ini.SetValue("Briefing", "MissionName", BriefingMissionName);
            ini.SetValue("Briefing", "MissionDescription", BriefingMissionDescription.Replace("\n", "\\n"));

            ini.SetValue("Environment", "Season", EnvironmentSeason);
            ini.SetValue("Environment", "TimeOfDay", EnvironmentTimeOfDay);
            ini.SetValue("Environment", "WeatherPreset", EnvironmentWeatherPreset);
            ini.SetValue("Environment", "Wind", EnvironmentWind);

            ini.SetValue("FlightPlan", "ObjectiveSeparationMax", FlightPlanObjectiveSeparationMax);
            ini.SetValue("FlightPlan", "ObjectiveSeparationMin", FlightPlanObjectiveSeparationMin);
            ini.SetValue("FlightPlan", "BorderLimit", BorderLimit);

            for (i = 0; i < Objectives.Count; i++)
                Objectives[i].SaveToFile(ini, "Objectives", $"Objective{i:000}");

            for (i = 0; i < AircraftPackages.Count; i++)
                AircraftPackages[i].SaveToFile(ini, "AircraftPackages", $"AircraftPackage{i:000}");

            foreach (string key in CarrierHints.Keys)
            {
                ini.SetValue("CarrierHints", key, CarrierHints[key]);
                ini.SetValue("CarrierHintsNames", key, key);
            }

            return ini;
        }

        internal new void AssignAliases()
        {
            base.AssignAliases();
            foreach (var item in Objectives)
                item.AssignAlias(Objectives.IndexOf(item));
            foreach (var item in AircraftPackages)
                item.AssignAlias(AircraftPackages.IndexOf(item));
        }

        public List<MissionTemplateSubTask> GetTasksFlat() {
            List<MissionTemplateSubTask> lst = new();
            foreach (var obj in Objectives)
            {
                lst.Add(obj);
                lst.AddRange(obj.SubTasks);
            }
            return lst;
        }


    }
}
