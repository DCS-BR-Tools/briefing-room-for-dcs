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

using System.Collections.Generic;
using BriefingRoom4DCS.Mission.DCSLuaObjects;
using BriefingRoom4DCS.Data;
using System.Linq;

namespace BriefingRoom4DCS.Generator
{
    public readonly struct Waypoint
    {
        private readonly List<DCSTask> ATTACK_GROUP_TASKS = new() { DCSTask.AntishipStrike, DCSTask.SEAD, DCSTask.CAS };
        internal string Name { get; }

        internal Coordinates Coordinates { get; }

        internal bool OnGround { get; }

        internal bool ScriptIgnore { get; }
        internal bool AircraftIgnore { get; }

        internal List<int> TargetGroupIDs { get; }
        internal bool HiddenMapMarker { get; }

        internal Waypoint(string name, Coordinates coordinates, bool onGround = false, List<int> targetGroupIDs = null, bool scriptIgnore = false, bool aircraftIgnore = false, bool hiddenMapMarker = false)
        {
            Name = name;
            Coordinates = coordinates;
            OnGround = onGround;
            ScriptIgnore = scriptIgnore;
            TargetGroupIDs = targetGroupIDs;
            AircraftIgnore = aircraftIgnore;
            HiddenMapMarker = hiddenMapMarker;
        }

        internal DCSWaypoint ToDCSWaypoint(Data.DBEntryAircraft aircraftData, DCSTask task)
        {
            var tasks = new List<DCSWaypointTask>();
            if (TargetGroupIDs != null && TargetGroupIDs.Count > 0)
            {
                if (ATTACK_GROUP_TASKS.Contains(task))
                    foreach (var TargetGroupID in TargetGroupIDs)
                        tasks.Add(new DCSWaypointTask
                        {
                            Enabled = true,
                            Auto = false,
                            Id = "AttackGroup",
                            Parameters = new Dictionary<string, object>{
                                {"altitudeEnabled", false},
                                {"groupId", TargetGroupID},
                                {"attackQtyLimit", false},
                                {"attackQty", 1},
                                {"expend", "Auto"},
                                {"altitude", 2000},
                                {"directionEnabled", false},
                                {"groupAttack", false},
                                {"weaponType", 9663676414},
                                {"direction", 0},
                            }
                        });
                else if (task == DCSTask.CAP)
                    foreach (var TargetGroupID in TargetGroupIDs)
                        tasks.Add(new DCSWaypointTask
                        {
                            Enabled = true,
                            Auto = false,
                            Id = "EngageGroup",
                            Parameters = new Dictionary<string, object>{
                                {"visible", false},
                                {"groupId", TargetGroupID},
                                {"priority", 1},
                                {"weaponType", 9659482112},
                            }
                        }
            );
            }
            return new DCSWaypoint
            {
                Alt = OnGround ? 0 : aircraftData.CruiseAlt,
                AltType = OnGround ? "RADIO" : "BARO",
                Action = "Turning Point",
                Speed = aircraftData.CruiseSpeed,
                Type = "Turning Point",
                EtaLocked = false,
                SpeedLocked = true,
                X = Coordinates.X,
                Y = Coordinates.Y,
                Name = Name,
                Tasks = tasks
            };
        }
    }

    internal class WaypointNameGenerator
    {
        private readonly List<string> ObjectiveNames = new();
        internal WaypointNameGenerator(IDatabase database, string langKey)
        {
            ObjectiveNames = [.. database.Common.Names.WPObjectivesNames.Get(langKey).Split(",")];
        }

        internal string GetWaypointName()
        {
            var objectiveName = Toolbox.RandomFrom(ObjectiveNames);
            ObjectiveNames.Remove(objectiveName);
            return objectiveName.ToUpper();
        }
    }
}
