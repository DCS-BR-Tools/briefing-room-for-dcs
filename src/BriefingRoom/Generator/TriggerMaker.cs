using System.IO;
using BriefingRoom4DCS.Mission;

namespace BriefingRoom4DCS.Generator
{
    internal static class TriggerMaker
    {
        internal static void AddEscortThreatTrigger(ref DCSMission mission, int zoneId, int triggerGroupID, int activationGroupId)
        {
            var trigIndex = int.Parse(mission.GetValue("NextTrigIndex"));
            var trigAction = $"[{trigIndex}] = \"a_activate_group({activationGroupId}); mission.trig.func[{trigIndex}]=nil;\",\n"; 
            mission.SetValue("TrigActions",mission.GetValue("TrigActions") + trigAction);

            var trigFunc = $"[{trigIndex}] = \"if mission.trig.conditions[{trigIndex}]() then mission.trig.actions[{trigIndex}]() end\",\n"; 
            mission.SetValue("TrigFuncs",mission.GetValue("TrigFuncs") + trigFunc);
            mission.SetValue("TrigFlags",mission.GetValue("TrigFlags") + $"[{trigIndex}] = true,\n");

            var trigCondition = $"[{trigIndex}] = \"return(c_zone_contains_unit({triggerGroupID}, {zoneId}) )\",\n";
            mission.SetValue("TrigConditions",mission.GetValue("TrigConditions") + trigCondition);


            string template = File.ReadAllText(Path.Combine(BRPaths.INCLUDE_LUA_MISSION,"TrigRules","PartGroupInZoneEscortThreat.lua"));
            GeneratorTools.ReplaceKey(ref template, "INDEX", trigIndex);
            GeneratorTools.ReplaceKey(ref template, "TRIGGROUP", triggerGroupID);
            GeneratorTools.ReplaceKey(ref template, "ACTIVATIONGROUPID", activationGroupId);
            GeneratorTools.ReplaceKey(ref template, "ZONEID", zoneId);
            mission.SetValue("TrigRules", mission.GetValue("TrigRules") + template);
            mission.SetValue("NextTrigIndex", trigIndex + 1);
        }

        internal static void AddEscortEndTrigger(ref DCSMission mission, int zoneId, int triggerGroupID, int objectiveIndex)
        {
            var trigIndex = int.Parse(mission.GetValue("NextTrigIndex"));
            var trigAction = $"[{trigIndex}] = \"a_do_script(\\\"briefingRoom.mission.objectivesTriggersCommon.fireEscortNearTrigger({objectiveIndex + 1})\\\");\",\n";
            mission.SetValue("TrigActions",mission.GetValue("TrigActions") + trigAction);

            var trigFunc = $"[{trigIndex}] = \"if mission.trig.conditions[{trigIndex}]() then mission.trig.actions[{trigIndex}]() end\",\n"; 
            mission.SetValue("TrigFuncs",mission.GetValue("TrigFuncs") + trigFunc);
            mission.SetValue("TrigFlags",mission.GetValue("TrigFlags") + $"[{trigIndex}] = true,\n");

            var trigCondition = $"[{trigIndex}] = \"return(c_zone_contains_unit({triggerGroupID}, {zoneId}) )\",\n";
            mission.SetValue("TrigConditions",mission.GetValue("TrigConditions") + trigCondition);


            string template = File.ReadAllText(Path.Combine(BRPaths.INCLUDE_LUA_MISSION,"TrigRules","PartGroupInZoneEscortEnd.lua"));
            GeneratorTools.ReplaceKey(ref template, "INDEX", trigIndex);
            GeneratorTools.ReplaceKey(ref template, "TRIGGROUP", triggerGroupID);
            GeneratorTools.ReplaceKey(ref template, "OBJECTIVEINDEX", objectiveIndex + 1);
            GeneratorTools.ReplaceKey(ref template, "ZONEID", zoneId);
            mission.SetValue("TrigRules", mission.GetValue("TrigRules") + template);
            mission.SetValue("NextTrigIndex", trigIndex + 1);
        }

         internal static void AddCargoTrigger(ref DCSMission mission, int zoneId, int cargoUnitId, string cargoUnitName, int objectiveIndex)
        {
            var trigIndex = int.Parse(mission.GetValue("NextTrigIndex"));
            var trigAction = $"[{trigIndex}] = \"a_do_script(\\\"briefingRoom.mission.objectivesTriggersCommon.fireCargoNearTrigger({objectiveIndex + 1}, \\\"{cargoUnitName}\\\")\\\");\",\n";
            mission.SetValue("TrigActions",mission.GetValue("TrigActions") + trigAction);

            var trigFunc = $"[{trigIndex}] = \"if mission.trig.conditions[{trigIndex}]() then mission.trig.actions[{trigIndex}]() end\",\n"; 
            mission.SetValue("TrigFuncs",mission.GetValue("TrigFuncs") + trigFunc);
            mission.SetValue("TrigFlags",mission.GetValue("TrigFlags") + $"[{trigIndex}] = true,\n");

            var trigCondition = $"[{trigIndex}] = \"return(c_cargo_unhooked_in_zone({cargoUnitId}, {zoneId}) )\",\n";
            mission.SetValue("TrigConditions",mission.GetValue("TrigConditions") + trigCondition);


            string template = File.ReadAllText(Path.Combine(BRPaths.INCLUDE_LUA_MISSION,"TrigRules","CargoInZone.lua"));
            GeneratorTools.ReplaceKey(ref template, "INDEX", trigIndex);
            GeneratorTools.ReplaceKey(ref template, "OBJECTIVEINDEX", objectiveIndex + 1);
            GeneratorTools.ReplaceKey(ref template, "CARGOUNITID", cargoUnitId);
            GeneratorTools.ReplaceKey(ref template, "CARGOUNITNAME", cargoUnitName);
            GeneratorTools.ReplaceKey(ref template, "ZONEID", zoneId);
            mission.SetValue("TrigRules", mission.GetValue("TrigRules") + template);
            mission.SetValue("NextTrigIndex", trigIndex + 1);
        }

         internal static void AddTransportTrigger(ref DCSMission mission, int zoneId, int cargoUnitId, string cargoUnitName, int objectiveIndex)
        {
            var trigIndex = int.Parse(mission.GetValue("NextTrigIndex"));
            var trigAction = $"[{trigIndex}] = \"a_do_script(\\\"briefingRoom.mission.objectivesTriggersCommon.fireTroopsNearTrigger({objectiveIndex + 1}, \\\"{cargoUnitName}\\\")\\\");\",\n";
            mission.SetValue("TrigActions",mission.GetValue("TrigActions") + trigAction);

            var trigFunc = $"[{trigIndex}] = \"if mission.trig.conditions[{trigIndex}]() then mission.trig.actions[{trigIndex}]() end\",\n"; 
            mission.SetValue("TrigFuncs",mission.GetValue("TrigFuncs") + trigFunc);
            mission.SetValue("TrigFlags",mission.GetValue("TrigFlags") + $"[{trigIndex}] = true,\n");

            var trigCondition = $"[{trigIndex}] = \"return(c_unit_in_zone({cargoUnitId}, {zoneId}) )\",\n";
            mission.SetValue("TrigConditions",mission.GetValue("TrigConditions") + trigCondition);


            string template = File.ReadAllText(Path.Combine(BRPaths.INCLUDE_LUA_MISSION,"TrigRules","TroopsInZone.lua"));
            GeneratorTools.ReplaceKey(ref template, "INDEX", trigIndex);
            GeneratorTools.ReplaceKey(ref template, "OBJECTIVEINDEX", objectiveIndex + 1);
            GeneratorTools.ReplaceKey(ref template, "CARGOUNITID", cargoUnitId);
            GeneratorTools.ReplaceKey(ref template, "CARGOUNITNAME", cargoUnitName);
            GeneratorTools.ReplaceKey(ref template, "ZONEID", zoneId);
            mission.SetValue("TrigRules", mission.GetValue("TrigRules") + template);
            mission.SetValue("NextTrigIndex", trigIndex + 1);
        }
    }
}