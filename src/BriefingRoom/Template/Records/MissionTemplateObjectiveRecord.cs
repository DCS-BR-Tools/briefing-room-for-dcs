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

using System.Collections.Generic;
using System.Linq;
using BriefingRoom4DCS.Data;

namespace BriefingRoom4DCS.Template
{
    internal record MissionTemplateObjectiveRecord : MissionTemplateSubTaskRecord
    {
        internal List<string> Features { get; init; }
        internal List<MissionTemplateSubTaskRecord> SubTasks { get; init; }
        internal Coordinates CoordinatesHint { get; init; }

        public MissionTemplateObjectiveRecord(MissionTemplateObjective objective)
        {
            Features = objective.Features;
            Options = objective.Options;
            Preset = SelectPreset(objective.Database, objective.Preset);
            Target = objective.Target;
            TargetBehavior = objective.TargetBehavior;
            TargetCount = objective.TargetCount;
            Task = objective.Task;
            TransportDistance = new MinMaxD(objective.TransportDistanceMin, objective.TransportDistanceMax);
            CoordinatesHint = objective.CoordinateHint_;
            ProgressionActivation = objective.ProgressionActivation;
            ProgressionDependentTasks = objective.ProgressionDependentTasks;
            ProgressionDependentIsAny = objective.ProgressionDependentIsAny;
            ProgressionOptions = objective.ProgressionOptions;
            ProgressionOverrideCondition = objective.ProgressionOverrideCondition.Trim();
            SubTasks = objective.SubTasks.Select(x => new MissionTemplateSubTaskRecord(x)).ToList();
        }

        private static string SelectPreset(IDatabase database, string preset)
        {
            if (string.IsNullOrEmpty(preset))
                return "";
            var presets = database.GetAllEntries<DBEntryObjectivePreset>().Where(x => !x.ID.Contains("Random")).ToList();
            var unsuitableFixedWingTasks = new List<string> { "TransportTroops", "TransportCargo", "LandNearAlly", "LandNearEnemy" };
            var unsuitableRotorTargets = new List<string> { "PlaneAny", "PlaneAttack", "PlaneBomber", "PlaneFighter", "PlaneTransport" };
            return preset switch
            {
                "RandomFixedWing" => Toolbox.RandomFrom(presets.Where(x => !unsuitableFixedWingTasks.Contains(x.Task)).Select(x => x.ID).ToList()),
                "RandomRotor" => Toolbox.RandomFrom(presets.Where(x => !x.Targets.Intersect(unsuitableRotorTargets).Any()).Select(x => x.ID).ToList()),
                "Random" => Toolbox.RandomFrom(presets.Select(x => x.ID).ToList()),
                _ => preset,
            };
        }
    }
}
