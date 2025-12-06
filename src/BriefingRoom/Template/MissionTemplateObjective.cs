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
along with Briefing Room for DCS World.
If not, see https://www.gnu.org/licenses/
==========================================================================
*/

using BriefingRoom4DCS.Data;
using System.Collections.Generic;

using System.Linq;

namespace BriefingRoom4DCS.Template
{
    public class MissionTemplateObjective : MissionTemplateSubTask
    {
        public List<string> Features { get { return Features_; } set { Features_ = Database.CheckIDs<DBEntryFeatureObjective>(value.ToArray()).ToList(); } }
        private List<string> Features_ = new();
        public double[] CoordinateHint { get { return CoordinateHint_.ToArray(); } set { CoordinateHint_ = new Coordinates(value[0], value[1]); } }
        internal Coordinates CoordinateHint_ { get; set; }

        public List<MissionTemplateSubTask> SubTasks { get; set; } = new List<MissionTemplateSubTask>();

        public MissionTemplateObjective(IDatabase database, bool setPreset = false)
            : base(database)
        {
            Features = new List<string>();
            Options = new List<ObjectiveOption>();
            Preset = setPreset ? "Interdiction" : "Custom";
            Target = "VehicleAny";
            TargetBehavior = "Idle";
            TargetCount = Amount.Average;
            Task = "DestroyAll";
        }


        public MissionTemplateObjective(IDatabase database, string presetID, Amount targetCount): base(database)
        {
            DBEntryObjectivePreset preset = Database.GetEntry<DBEntryObjectivePreset>(presetID);

            if (preset == null) // Preset doesn't exist.
            {
                Features = new List<string>();
                Options = new List<ObjectiveOption>();
                Preset = "";
                Target = "VehicleAny";
                TargetBehavior = "Idle";
                TargetCount = Amount.Average;
                Task = "DestroyAll";
            }
            else
            {
                Features = preset.Features.ToList();
                Options = preset.Options.ToList();
                Preset = presetID;
                Target = Toolbox.RandomFrom(preset.Targets);
                TargetBehavior = Toolbox.RandomFrom(preset.TargetsBehaviors);
                TargetCount = targetCount;
                Task = preset.Task;
            }

            ProgressionDependentTasks = new List<int>();
            ProgressionDependentIsAny = false;
            ProgressionOptions = new List<ObjectiveProgressionOption>();
            ProgressionOverrideCondition = "";
        }

        internal MissionTemplateObjective(IDatabase database, INIFile ini, string section, string key): base(database)
        {
            LoadFromFile(ini, section, key);
        }

        internal void LoadFromFile(IDatabase database, INIFile ini, string section, string key)
        {
            Features = database.CheckIDs<DBEntryFeatureObjective>(ini.GetValueArray<string>(section, $"{key}.Features")).ToList();
            Options = ini.GetValueArray<ObjectiveOption>(section, $"{key}.Options").ToList();
            Preset = ini.GetValue<string>(section, $"{key}.Preset", "Custom");
            Target = ini.GetValue<string>(section, $"{key}.Target");
            TargetBehavior = ini.GetValue<string>(section, $"{key}.TargetBehavior");
            TargetCount = ini.GetValue<Amount>(section, $"{key}.TargetCount");
            Task = ini.GetValue<string>(section, $"{key}.Task");
            ProgressionDependentTasks = ini.GetValueArray<int>(section, $"{key}.Progression.DependentTasks").ToList();
            ProgressionDependentIsAny = ini.GetValue<bool>(section, $"{key}.Progression.IsAny");
            ProgressionOptions = ini.GetValueArray<ObjectiveProgressionOption>(section, $"{key}.Progression.Options").ToList();
            ProgressionOverrideCondition = ini.GetValue<string>(section, $"{key}.Progression.OverrideCondition");
            CoordinateHint_ = ini.GetValue<Coordinates>(section, $"{key}.CoordinateHint");
            foreach (var subKey in ini.GetKeysInSection(section)
                .Where(x => x.Contains(key))
                .Select(x => x.Split(".")[1])
                .Where(x => x.Contains("subtask"))
                .Distinct().ToList())
            {
                SubTasks.Add(new MissionTemplateSubTask(Database, ini, section, $"{key}.{subKey}"));
            }
        }

        new internal void SaveToFile(INIFile ini, string section, string key)
        {
            ini.SetValueArray(section, $"{key}.Features", Features.ToArray());
            ini.SetValueArray(section, $"{key}.Options", Options.ToArray());
            ini.SetValue(section, $"{key}.Preset", Preset);
            ini.SetValue(section, $"{key}.Task", Task);
            ini.SetValue(section, $"{key}.Target", Target);
            ini.SetValue(section, $"{key}.TargetBehavior", TargetBehavior);
            ini.SetValue(section, $"{key}.TargetCount", TargetCount);
            ini.SetValue(section, $"{key}.CoordinateHint", CoordinateHint_);
            ini.SetValueArray(section, $"{key}.Progression.DependentTasks", ProgressionDependentTasks.Select(x => x.ToString()).ToArray());
            ini.SetValue(section, $"{key}.Progression.IsAny", ProgressionDependentIsAny);
            ini.SetValueArray(section, $"{key}.Progression.Options", ProgressionOptions.ToArray());
            ini.SetValue(section, $"{key}.Progression.OverrideCondition", ProgressionOverrideCondition);
            var i = 0;
            foreach (var subTask in SubTasks)
            {
                subTask.SaveToFile(ini, section, $"{key}.SubTask{i}");
                i++;
            }
        }

        new public void AssignAlias(int index)
        {
            Alias = Toolbox.GetAlias(index);
            foreach (var subTask in SubTasks)
                subTask.Alias = $"{Alias}{SubTasks.IndexOf(subTask) + 2}";
        }

    }
}
