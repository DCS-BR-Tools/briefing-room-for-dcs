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

using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;
using System.Collections.Generic;
using System.Linq;
using FluentRandomPicker;

namespace BriefingRoom4DCS.Generator.Mission
{
    internal class Briefing
    {

        internal static void GenerateMissionBriefingDescription(IDatabase database, ref DCSMission mission, MissionTemplateRecord template, List<UnitFamily> objectiveTargetUnitFamilies, DBEntrySituation situationDB)
        {
            // Try to get the provided custom mission description.
            string briefingDescription = (template.BriefingMissionDescription ?? "").Replace("\r\n", "\n").Trim();

            // No custom description found, generate one from the most frequent objective task/target combination.
            if (string.IsNullOrEmpty(briefingDescription))
            {
                if (template.Objectives.Count == 0)
                    briefingDescription = "";
                else
                {
                    var familyCount = 0;
                    Dictionary<string, List<string>> descriptionsMap = new();
                    foreach (var obj in template.Objectives)
                    {   
                        var task = obj.Task;
                        if(obj.HasPreset)
                            task = database.GetEntry<DBEntryObjectivePreset>(obj.Preset).Task;
 
                        DBEntryBriefingDescription descriptionDB =
                            database.GetEntry<DBEntryBriefingDescription>(
                                database.GetEntry<DBEntryObjectiveTask>(task).BriefingDescription);
                        AppendDescription(task, descriptionDB.DescriptionText[(int)objectiveTargetUnitFamilies[familyCount]].Get(mission.LangKey), ref descriptionsMap);
                        familyCount++;
                        AddSubTasks(database, mission.LangKey, obj, objectiveTargetUnitFamilies, ref descriptionsMap, ref familyCount);
                    }

                    briefingDescription = ConstructTaskDescriptions(database, descriptionsMap, ref mission);
                }
                if (situationDB.BriefingDescriptions != null && situationDB.BriefingDescriptions.Count > 0)
                    briefingDescription = GeneratorTools.ParseRandomString(string.Join(" ", Toolbox.RandomFrom(situationDB.BriefingDescriptions).Get(mission.LangKey), briefingDescription), mission);
            }


            mission.Briefing.Description = briefingDescription;
            mission.SetValue("BRIEFINGDESCRIPTION", briefingDescription);
        }

        private static string ConstructTaskDescriptions(IDatabase database, Dictionary<string, List<string>> descriptionsMap, ref DCSMission mission)
        {
            var briefingDescriptionList = new List<string>();
            var maxDescriptionCount = database.Common.Briefing.MaxObjectiveDescriptionCount;

            // Remove duplicates
            foreach (var key in descriptionsMap.Keys)
                descriptionsMap[key] = descriptionsMap[key].Distinct().ToList();

            while (descriptionsMap.Keys.Count > 0 && briefingDescriptionList.Count < maxDescriptionCount)
            {
                var task = descriptionsMap.Keys.First();
                if (descriptionsMap.Keys.Count > 1)
                    task = Out.Of()
                    .Values(descriptionsMap.Keys.ToList())
                    .WithWeights(descriptionsMap.Values.Select(x => x.Count).ToList())
                    .PickOne();

                var item = descriptionsMap[task].GroupBy(i => i).OrderByDescending(grp => grp.Count()).Select(grp => grp.Key).First();
                descriptionsMap[task].Remove(item);
                if (descriptionsMap[task].Count == 0)
                    descriptionsMap.Remove(task);
                briefingDescriptionList.Add(item);
            }

            var description = GeneratorTools.ParseRandomString(JoinObjectiveDescriptions(database, mission.LangKey, briefingDescriptionList), mission);
            if (descriptionsMap.Keys.Count > 0 && briefingDescriptionList.Count == maxDescriptionCount)
                description = $"{description} {database.Common.Briefing.OverflowObjectiveDescriptionText.Get(mission.LangKey)}";
            return description;
        }

        private static string JoinObjectiveDescriptions(IDatabase database, string langKey,  IEnumerable<string> descriptions) => descriptions.Aggregate((acc, x) =>
                {
                    if (string.IsNullOrEmpty(acc))
                        return x;
                    return $"{acc} {Toolbox.RandomFrom(database.Common.Briefing.ObjectiveDescriptionConnectors.Get(langKey).Split(","))} {LowerFirstChar(GeneratorTools.ParseRandomString(x))}";
                });

        private static void AddSubTasks(IDatabase database, string langKey, MissionTemplateObjectiveRecord obj, List<UnitFamily> objectiveTargetUnitFamilies, ref Dictionary<string, List<string>> descriptionsMap, ref int familyCount)
        {
            foreach (var subTask in obj.SubTasks)
            {
                var descriptionDB =
                    database.GetEntry<DBEntryBriefingDescription>(
                        database.GetEntry<DBEntryObjectiveTask>(subTask.Task).BriefingDescription);
                AppendDescription(obj.Task, descriptionDB.DescriptionText[(int)objectiveTargetUnitFamilies[familyCount]].Get(langKey), ref descriptionsMap);
                familyCount++;
            }
        }

        private static void AppendDescription(string task, string description, ref Dictionary<string, List<string>> descriptionsMap)
        {
            if (descriptionsMap.ContainsKey(task))
                descriptionsMap[task].Add(description);
            else
                descriptionsMap.Add(task, new List<string> { description });
        }

        private static string LowerFirstChar(string str)
        {
            return char.ToLower(str[0]) + str[1..];
        }
    }
}
