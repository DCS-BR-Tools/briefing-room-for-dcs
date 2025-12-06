using System.Collections.Generic;
using System.Linq;
using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS.GUI.Utils
{
    public class ObjectiveUtils
    {
        internal ObjectiveUtils(bool usePreset)
        {
            UsePreset = usePreset;
        }
        private bool UsePreset;
        internal MissionTemplateObjective Tab;
        internal void SetObjectiveTab(MissionTemplateObjective var)
        {
            Tab = var;
        }

        internal void AddObjective(IDatabase database, MissionTemplate Template)
        {
            MissionTemplateObjective obj = new(database, UsePreset);
            Tab = obj;
            obj.Alias = BriefingRoom.GetAlias(Template.Objectives.Count);
            Template.Objectives.Add(obj);
        }

        internal void CloneObjective(IDatabase database, MissionTemplateObjective obj, MissionTemplate Template)
        {
            MissionTemplateObjective newObj = new(database, UsePreset)
            {
                Target = obj.Target,
                TargetBehavior = obj.TargetBehavior,
                TargetCount = obj.TargetCount,
                Task = obj.Task,
                Options = obj.Options,
                Features = obj.Features,
                SubTasks = obj.SubTasks,
                Preset = obj.Preset,
                ProgressionDependentTasks = obj.ProgressionDependentTasks,
                ProgressionDependentIsAny = obj.ProgressionDependentIsAny,
                ProgressionOptions = obj.ProgressionOptions,
                ProgressionOverrideCondition = obj.ProgressionOverrideCondition,
            };
            newObj.Alias = BriefingRoom.GetAlias(Template.Objectives.Count);
            Template.Objectives.Add(newObj);
            Tab = newObj;
        }

        internal void CloneObjectiveTask(IDatabase database, MissionTemplateObjective obj)
        {
            MissionTemplateSubTask newSubT = new(database)
            {
                Target = obj.Target,
                TargetBehavior = obj.TargetBehavior,
                TargetCount = obj.TargetCount,
                Task = obj.Task,
                Options = obj.Options,
                Preset = obj.Preset,
                ProgressionDependentTasks = obj.ProgressionDependentTasks,
                ProgressionDependentIsAny = obj.ProgressionDependentIsAny,
                ProgressionOptions = obj.ProgressionOptions,
                ProgressionOverrideCondition = obj.ProgressionOverrideCondition
            };
            obj.SubTasks.Add(newSubT);
        }
        internal void CloneObjectiveSubTask(IDatabase database, MissionTemplateSubTask subTask, MissionTemplateObjective obj)
        {
            MissionTemplateSubTask newSubT = new(database)
            {
                Target = subTask.Target,
                TargetBehavior = subTask.TargetBehavior,
                TargetCount = subTask.TargetCount,
                Task = subTask.Task,
                Options = subTask.Options,
                Preset = subTask.Preset,
                Alias = $"{obj.Alias}{obj.SubTasks.Count + 2}",
                ProgressionDependentTasks = subTask.ProgressionDependentTasks,
                ProgressionDependentIsAny = subTask.ProgressionDependentIsAny,
                ProgressionOptions = subTask.ProgressionOptions,
                ProgressionOverrideCondition = subTask.ProgressionOverrideCondition
            };
            obj.SubTasks.Add(newSubT);
        }

        internal void RemoveObjective(MissionTemplateObjective obj, MissionTemplate Template)
        {
            Template.Objectives.Remove(obj);
            if (Tab == obj)
                Tab = Template.Objectives.First();
        }

        internal void CloneProgression(MissionTemplateObjective obj, MissionTemplateSubTask subTask)
        {
            subTask.ProgressionDependentTasks = obj.ProgressionDependentTasks;
            subTask.ProgressionDependentIsAny = obj.ProgressionDependentIsAny;
            subTask.ProgressionOptions = obj.ProgressionOptions;
            subTask.ProgressionOverrideCondition = obj.ProgressionOverrideCondition;
        }

        internal void AddSubTask(IDatabase database, MissionTemplateObjective obj, bool preset = false)
        {
            obj.SubTasks.Add(new MissionTemplateSubTask(database){
                Preset = preset ? "Interdiction" : "Custom",
                Alias = $"{obj.Alias}{obj.SubTasks.Count + 2}",
            });
        }

        internal void RemoveSubTask(MissionTemplateObjective obj, MissionTemplateSubTask subTsk)
        {
            obj.SubTasks.Remove(subTsk);
        }

        internal void ClearObjectiveHint(MissionTemplateObjective obj)
        {
            obj.CoordinateHint = new double[] { 0, 0 };
        }

    }
}