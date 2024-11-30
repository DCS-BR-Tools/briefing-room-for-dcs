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
using System;
using System.Collections.Generic;

using System.Linq;

namespace BriefingRoom4DCS.Template
{
    public class MissionTemplateSubTask : MissionTemplateGroup
    {
        public List<ObjectiveOption> Options { get { return Options_; } set { Options_ = value.Distinct().ToList(); } }
        private List<ObjectiveOption> Options_;
        public string Target { get { return Target_; } set { Target_ = Database.Instance.CheckID<DBEntryObjectiveTarget>(value); } }
        private string Target_;
        public string TargetBehavior { get { return TargetBehavior_; } set { TargetBehavior_ = Database.Instance.CheckID<DBEntryObjectiveTargetBehavior>(value); } }
        private string TargetBehavior_;
        public Amount TargetCount { get; set; }
        public string Task { get { return Task_; } set { Task_ = Database.Instance.CheckID<DBEntryObjectiveTask>(value); } }
        private string Task_;

        public string Preset { get { return Preset_; } set { Preset_ = Database.Instance.CheckID<DBEntryObjectivePreset>(value); } }
        private string Preset_ = "Custom";
        public bool HasPreset { get { return Preset_ != "Custom"; } }
        public bool ProgressionActivation { get { return ProgressionDependentTasks.Count > 0 || !string.IsNullOrEmpty(ProgressionOverrideCondition); } }
        public List<int> ProgressionDependentTasks { get { return ProgressionDependentTasks_; } set { ProgressionDependentTasks_ = value.Distinct().ToList(); } }
        private List<int> ProgressionDependentTasks_;
        public bool ProgressionDependentIsAny { get; set; } 
        public List<ObjectiveProgressionOption> ProgressionOptions { get { return ProgressionOptions_; } set { ProgressionOptions_ = value.Distinct().ToList(); } }
        private List<ObjectiveProgressionOption> ProgressionOptions_;
        public string ProgressionOverrideCondition {get; set;}

        public MissionTemplateSubTask()
        {
            Options = new List<ObjectiveOption>();
            Target = "VehicleAny";
            TargetBehavior = "Idle";
            TargetCount = Amount.Average;
            Task = "DestroyAll";
            Preset = "Custom";
            ProgressionDependentTasks = new List<int>();
            ProgressionDependentIsAny = false;
            ProgressionOptions = new List<ObjectiveProgressionOption>();
            ProgressionOverrideCondition = "";
        }

        public MissionTemplateSubTask(string presetID, Amount targetCount)
        {
            DBEntryObjectivePreset preset = Database.Instance.GetEntry<DBEntryObjectivePreset>(presetID);

            if (preset == null) // Preset doesn't exist.
            {
                Options = new List<ObjectiveOption>();
                Preset = "";
                Target = "VehicleAny";
                TargetBehavior = "Idle";
                TargetCount = Amount.Average;
                Task = "DestroyAll";

            }
            else
            {
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

        public MissionTemplateSubTask(string target, string targetBehavior, string task, Amount targetCount = Amount.Average, params ObjectiveOption[] options)
        {
            Options = new List<ObjectiveOption>(options);
            Target = target;
            TargetBehavior = targetBehavior;
            TargetCount = targetCount;
            Task = task;
            Preset = "Custom";
        }

        internal MissionTemplateSubTask(INIFile ini, string section, string key)
        {
            LoadFromFile(ini, section, key);
        }

        internal void LoadFromFile(INIFile ini, string section, string key)
        {
            Options = ini.GetValueArray<ObjectiveOption>(section, $"{key}.Options").ToList();
            Target = ini.GetValue<string>(section, $"{key}.Target");
            TargetBehavior = ini.GetValue<string>(section, $"{key}.TargetBehavior");
            TargetCount = ini.GetValue<Amount>(section, $"{key}.TargetCount");
            Task = ini.GetValue<string>(section, $"{key}.Task");
            Preset = ini.GetValue<string>(section, $"{key}.Preset", "Custom");
            ProgressionDependentTasks = ini.GetValueArray<int>(section, $"{key}.Progression.DependentTasks").ToList();
            ProgressionDependentIsAny = ini.GetValue<bool>(section, $"{key}.Progression.IsAny");
            ProgressionOptions = ini.GetValueArray<ObjectiveProgressionOption>(section, $"{key}.Progression.Options").ToList();
            ProgressionOverrideCondition = ini.GetValue<string>(section, $"{key}.Progression.OverrideCondition");
        }

        internal void SaveToFile(INIFile ini, string section, string key)
        {
            ini.SetValueArray(section, $"{key}.Options", Options.ToArray());
            ini.SetValue(section, $"{key}.Task", Task);
            ini.SetValue(section, $"{key}.Target", Target);
            ini.SetValue(section, $"{key}.TargetBehavior", TargetBehavior);
            ini.SetValue(section, $"{key}.TargetCount", TargetCount);
            ini.SetValue(section, $"{key}.Preset", Preset);
            ini.SetValueArray(section, $"{key}.Progression.DependentTasks", ProgressionDependentTasks.Select(x => x.ToString()).ToArray());
            ini.SetValue(section, $"{key}.Progression.IsAny", ProgressionDependentIsAny);
            ini.SetValueArray(section, $"{key}.Progression.Options", ProgressionOptions.ToArray());
            ini.SetValue(section, $"{key}.Progression.OverrideCondition", ProgressionOverrideCondition);
        }
    }
}
