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
    public class BaseTemplate : IBaseTemplate
    {
        public string ContextCoalitionBlue { get { return ContextCoalitionBlue_; } set { ContextCoalitionBlue_ = Database.CheckID<DBEntryCoalition>(value); } }
        private string ContextCoalitionBlue_;
        public string ContextCoalitionRed { get { return ContextCoalitionRed_; } set { ContextCoalitionRed_ = Database.CheckID<DBEntryCoalition>(value); } }
        private string ContextCoalitionRed_;
        public Decade ContextDecade { get; set; }
        public Coalition ContextPlayerCoalition { get; set; }
        public string ContextTheater { get { return ContextTheater_; } set { ContextTheater_ = Database.CheckID<DBEntryTheater>(value); } }
        private string ContextTheater_;
        public string ContextSituation { get { return ContextSituation_; } set { ContextSituation_ = Database.CheckID<DBEntrySituation>(value, allowEmptyStr: true, allowedValues: new List<string>{"None"}); } }
        private string ContextSituation_;
        public int FlightPlanObjectiveDistanceMax { get { return FlightPlanObjectiveDistanceMax_; } set { FlightPlanObjectiveDistanceMax_ = Toolbox.Clamp(value, 0, Database.Common.MaxObjectiveDistance); } }
        private int FlightPlanObjectiveDistanceMax_;
        public int FlightPlanObjectiveDistanceMin { get { return FlightPlanObjectiveDistanceMin_; } set { FlightPlanObjectiveDistanceMin_ = Toolbox.Clamp(value, 0, Database.Common.MaxObjectiveDistance); } }
        private int FlightPlanObjectiveDistanceMin_;
        public string FlightPlanTheaterStartingAirbase { get { return FlightPlanTheaterStartingAirbase_; } set { FlightPlanTheaterStartingAirbase_ = Database.CheckID<DBEntryAirbase>(value, allowEmptyStr: true); } }
        private string FlightPlanTheaterStartingAirbase_;
        public List<string> MissionFeatures { get { return MissionFeatures_; } set { MissionFeatures_ = Database.CheckIDs<DBEntryFeatureMission>(value.ToArray()).ToList(); } }
        private List<string> MissionFeatures_ = new();
        public List<string> Mods { get { return Mods_; } set { Mods_ = Database.CheckIDs<DBEntryDCSMod>(value.ToArray()).ToList(); } }
        private List<string> Mods_ = new();
        public FogOfWar OptionsFogOfWar { get; set; }
        public List<string> OptionsMission { get { return OptionsMission_; } set { OptionsMission_ = Database.CheckIDs<DBEntryOptionsMission>(value.ToArray()).ToList(); } }
        private List<string> OptionsMission_ = new();
        public List<RealismOption> OptionsRealism { get { return OptionsRealism_; } set { OptionsRealism_ = value.Distinct().ToList(); } }
        private List<RealismOption> OptionsRealism_ = new();
        public List<string> OptionsUnitBanList { get { return OptionsUnitBanList_; } set { OptionsUnitBanList_ = value.Distinct().ToList(); } }
        private List<string> OptionsUnitBanList_ = new();
        public List<MissionTemplateFlightGroup> PlayerFlightGroups { get { return PlayerFlightGroups_; } set { PlayerFlightGroups_ = value.Take(Database.Common.MaxPlayerFlightGroups).ToList(); } }
        private List<MissionTemplateFlightGroup> PlayerFlightGroups_ = new();
        public AmountR SituationEnemySkill { get; set; }
        public AmountNR SituationEnemyAirDefense { get; set; }
        public AmountNR SituationEnemyAirForce { get; set; }
        public AmountR SituationFriendlySkill { get; set; }
        public AmountNR SituationFriendlyAirDefense { get; set; }
        public AmountNR SituationFriendlyAirForce { get; set; }
        public int CombinedArmsCommanderBlue { get { return CombinedArmsCommanderBlue_; } set { CombinedArmsCommanderBlue_ = Toolbox.Clamp(value, 0, Database.Common.MaxCombinedArmsSlots); } }
        private int CombinedArmsCommanderBlue_;
        public int CombinedArmsCommanderRed { get { return CombinedArmsCommanderRed_; } set { CombinedArmsCommanderRed_ = Toolbox.Clamp(value, 0, Database.Common.MaxCombinedArmsSlots); } }
        private int CombinedArmsCommanderRed_;
        public int CombinedArmsJTACBlue { get { return CombinedArmsJTACBlue_; } set { CombinedArmsJTACBlue_ = Toolbox.Clamp(value, 0, Database.Common.MaxCombinedArmsSlots); } }
        private int CombinedArmsJTACBlue_;
        public int CombinedArmsJTACRed { get { return CombinedArmsJTACRed_; } set { CombinedArmsJTACRed_ = Toolbox.Clamp(value, 0, Database.Common.MaxCombinedArmsSlots); } }
        private int CombinedArmsJTACRed_;
        public DsAirbase AirbaseDynamicSpawn { get; set; }
        public bool CarrierDynamicSpawn { get; set; }
        public DsAirbase AirbaseDynamicCargo { get; set; }
        public bool CarrierDynamicCargo { get; set; }
        public bool DSAllowHotStart { get; set; }
        public IDatabase Database { get; private set; }

        public BaseTemplate(IDatabase database)
        {
            Database = database;
        }
        public void Clear()
        {
            ContextCoalitionBlue = "USA";
            ContextCoalitionRed = "Russia";
            ContextDecade = Decade.Decade2000;
            ContextPlayerCoalition = Coalition.Blue;
            ContextTheater = "Caucasus";
            ContextSituation = "";

            FlightPlanObjectiveDistanceMax = 160;
            FlightPlanObjectiveDistanceMin = 40;
            FlightPlanTheaterStartingAirbase = "";

            MissionFeatures = new List<string>{
                "FriendlyAWACS",
                "FriendlyTankerBasket",
                "FriendlyTankerBoom"
            };

            Mods = new List<string>();

            OptionsFogOfWar = FogOfWar.All;
            OptionsMission = new List<string> { "ImperialUnitsForBriefing", "MarkWaypoints", "CombinedArmsPilotControl", "AllowLowPoly" };
            OptionsRealism = new RealismOption[] { RealismOption.DisableDCSRadioAssists, RealismOption.NoBDA }.ToList();
            OptionsUnitBanList = new List<string>();

            PlayerFlightGroups = new MissionTemplateFlightGroup[] { new MissionTemplateFlightGroup(Database) }.ToList();

            SituationEnemySkill = AmountR.Random;
            SituationEnemyAirDefense = AmountNR.Random;
            SituationEnemyAirForce = AmountNR.Random;

            SituationFriendlySkill = AmountR.Random;
            SituationFriendlyAirDefense = AmountNR.Random;
            SituationFriendlyAirForce = AmountNR.Random;

            CombinedArmsCommanderBlue = 0;
            CombinedArmsCommanderRed = 0;
            CombinedArmsJTACBlue = 0;
            CombinedArmsJTACRed = 0;

            AirbaseDynamicSpawn = DsAirbase.None;
            CarrierDynamicSpawn = false;
            DSAllowHotStart = false;

            AirbaseDynamicCargo = DsAirbase.Friendly;
            CarrierDynamicCargo = true;


            AssignAliases();
        }

        internal bool Load(INIFile ini)
        {
            ContextCoalitionBlue = ini.GetValue("Context", "CoalitionBlue", ContextCoalitionBlue);
            ContextCoalitionRed = ini.GetValue("Context", "CoalitionRed", ContextCoalitionRed);
            ContextDecade = ini.GetValue("Context", "Decade", ContextDecade);
            ContextPlayerCoalition = ini.GetValue("Context", "PlayerCoalition", ContextPlayerCoalition);
            ContextTheater = ini.GetValue("Context", "Theater", ContextTheater);
            ContextSituation = ini.GetValue("Context", "Situation", ContextSituation);

            FlightPlanObjectiveDistanceMax = ini.GetValue("FlightPlan", "ObjectiveDistanceMax", FlightPlanObjectiveDistanceMax);
            FlightPlanObjectiveDistanceMin = ini.GetValue("FlightPlan", "ObjectiveDistanceMin", FlightPlanObjectiveDistanceMin);
            FlightPlanTheaterStartingAirbase = ini.GetValue("FlightPlan", "TheaterStartingAirbase", FlightPlanTheaterStartingAirbase);

            MissionFeatures = ini.GetValueDistinctList<string>("MissionFeatures", "MissionFeatures");

            Mods = ini.GetValueArray<string>("Mods", "Mods").ToList();


            OptionsFogOfWar = ini.GetValue("Options", "FogOfWar", OptionsFogOfWar);
            OptionsMission = ini.GetValueDistinctList<string>("Options", "Mission");
            OptionsRealism = ini.GetValueDistinctList<RealismOption>("Options", "Realism");
            OptionsUnitBanList = ini.GetValueDistinctList<string>("Options", "UnitBanList");

            PlayerFlightGroups.Clear();
            foreach (string key in ini.GetTopLevelKeysInSection("PlayerFlightGroups"))
                PlayerFlightGroups.Add(new MissionTemplateFlightGroup(Database, ini, "PlayerFlightGroups", key));

            SituationEnemySkill = ini.GetValue("Situation", "EnemySkill", SituationEnemySkill);
            SituationEnemyAirDefense = ini.GetValue("Situation", "EnemyAirDefense", SituationEnemyAirDefense);
            SituationEnemyAirForce = ini.GetValue("Situation", "EnemyAirForce", SituationEnemyAirForce);

            SituationFriendlySkill = ini.GetValue("Situation", "FriendlySkill", SituationFriendlySkill);
            SituationFriendlyAirDefense = ini.GetValue("Situation", "FriendlyAirDefense", SituationFriendlyAirDefense);
            SituationFriendlyAirForce = ini.GetValue("Situation", "FriendlyAirForce", SituationFriendlyAirForce);

            CombinedArmsCommanderBlue = ini.GetValue("CombinedArms", "CommanderBlue", CombinedArmsCommanderBlue);
            CombinedArmsCommanderRed = ini.GetValue("CombinedArms", "CommanderRed", CombinedArmsCommanderRed);
            CombinedArmsJTACBlue = ini.GetValue("CombinedArms", "JTACBlue", CombinedArmsJTACBlue);
            CombinedArmsJTACRed = ini.GetValue("CombinedArms", "JTACRed", CombinedArmsJTACRed);

            AirbaseDynamicSpawn = ini.GetValue("Options", "AirbaseDynamicSpawn", AirbaseDynamicSpawn);
            CarrierDynamicSpawn = ini.GetValue("Options", "CarrierDynamicSpawn", CarrierDynamicSpawn);
            DSAllowHotStart = ini.GetValue("Options", "DSAllowHotStart", DSAllowHotStart);

            AirbaseDynamicCargo = ini.GetValue("Options", "AirbaseDynamicCargo", AirbaseDynamicCargo);
            CarrierDynamicCargo = ini.GetValue("Options", "CarrierDynamicCargo", CarrierDynamicCargo);


            AssignAliases();
            return true;
        }

        internal INIFile GetAsIni()
        {
            int i;
            var ini = new INIFile();

            ini.SetValue("Context", "CoalitionBlue", ContextCoalitionBlue);
            ini.SetValue("Context", "CoalitionRed", ContextCoalitionRed);
            ini.SetValue("Context", "Decade", ContextDecade);
            ini.SetValue("Context", "PlayerCoalition", ContextPlayerCoalition);
            ini.SetValue("Context", "Theater", ContextTheater);
            ini.SetValue("Context", "Situation", ContextSituation);

            ini.SetValue("FlightPlan", "ObjectiveDistanceMax", FlightPlanObjectiveDistanceMax);
            ini.SetValue("FlightPlan", "ObjectiveDistanceMin", FlightPlanObjectiveDistanceMin);
            ini.SetValue("FlightPlan", "TheaterStartingAirbase", FlightPlanTheaterStartingAirbase);

            ini.SetValueArray("MissionFeatures", "MissionFeatures", MissionFeatures.ToArray());

            ini.SetValueArray("Mods", "Mods", Mods.ToArray());


            ini.SetValue("Options", "FogOfWar", OptionsFogOfWar);
            ini.SetValueArray("Options", "Mission", OptionsMission.ToArray());
            ini.SetValueArray("Options", "Realism", OptionsRealism.ToArray());
            ini.SetValueArray("Options", "UnitBanList", OptionsUnitBanList.ToArray());

            ini.SetValue("Options", "AirbaseDynamicSpawn", AirbaseDynamicSpawn);
            ini.SetValue("Options", "CarrierDynamicSpawn", CarrierDynamicSpawn);
            ini.SetValue("Options", "DSAllowHotStart", DSAllowHotStart);

            ini.SetValue("Options", "AirbaseDynamicCargo", AirbaseDynamicCargo);
            ini.SetValue("Options", "CarrierDynamicCargo", CarrierDynamicCargo);

            for (i = 0; i < PlayerFlightGroups.Count; i++)
                PlayerFlightGroups[i].SaveToFile(ini, "PlayerFlightGroups", $"PlayerFlightGroup{i:000}");

            ini.SetValue("Situation", "EnemySkill", SituationEnemySkill);
            ini.SetValue("Situation", "EnemyAirDefense", SituationEnemyAirDefense);
            ini.SetValue("Situation", "EnemyAirForce", SituationEnemyAirForce);

            ini.SetValue("Situation", "FriendlySkill", SituationFriendlySkill);
            ini.SetValue("Situation", "FriendlyAirDefense", SituationFriendlyAirDefense);
            ini.SetValue("Situation", "FriendlyAirForce", SituationFriendlyAirForce);

            ini.SetValue("CombinedArms", "CommanderBlue", CombinedArmsCommanderBlue);
            ini.SetValue("CombinedArms", "CommanderRed", CombinedArmsCommanderRed);
            ini.SetValue("CombinedArms", "JTACBlue", CombinedArmsJTACBlue);
            ini.SetValue("CombinedArms", "JTACRed", CombinedArmsJTACRed);

            return ini;
        }

        internal void AssignAliases()
        {
            foreach (var item in PlayerFlightGroups)
                item.AssignAlias(PlayerFlightGroups.IndexOf(item));
        }


    }
}
