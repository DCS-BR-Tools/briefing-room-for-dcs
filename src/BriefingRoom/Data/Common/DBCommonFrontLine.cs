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

using BriefingRoom4DCS.Template;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BriefingRoom4DCS.Data
{
    internal class DBCommonFrontLine
    {
        internal MinMaxD AngleVarianceRange { get; private set; }
        internal int SegmentsPerSide { get; private set; }
        internal MinMaxD LinePointSeparationRange { get; private set; }
        internal MinMaxD BorderBiasRange { get; private set; }
        internal MinMaxD BaseObjectiveBiasRange {get; private set;}
        internal double FriendlyObjectiveBias { get; private set; }
        internal double EnemyObjectiveBias { get; private set; }
        internal MinMaxD ObjectiveBiasLimits { get; private set; }
        internal MinMaxD[] DefaultUnitLimits { get; private set; }
        internal Dictionary<UnitFamily, MinMaxD[]> UnitLimits { get; private set; }

        internal DBCommonFrontLine()
        {
            INIFile ini = new(Path.Combine(BRPaths.DATABASE, "FrontLine.ini"));
            AngleVarianceRange = new MinMaxD(ini.GetValueArray<double>("Limits", "AngleVarianceRange"));
            SegmentsPerSide = ini.GetValue<int>("Limits", "SegmentsPerSide");
            LinePointSeparationRange = new MinMaxD(ini.GetValueArray<double>("Limits", "LinePointSeparationRange"));
            BorderBiasRange = new MinMaxD(ini.GetValueArray<double>("Limits", "BorderBiasRange"));
            FriendlyObjectiveBias = ini.GetValue<double>("Limits", "FriendlyObjectiveBias");
            EnemyObjectiveBias = ini.GetValue<double>("Limits", "EnemyObjectiveBias");
            BaseObjectiveBiasRange = new MinMaxD(ini.GetValueArray<double>("Limits", "BaseObjectiveBiasRange"));
            ObjectiveBiasLimits = new MinMaxD(ini.GetValueArray<double>("Limits", "ObjectiveBiasLimits"));

            DefaultUnitLimits = ini.GetValueArray<string>("Limits", "DefaultUnitLimit", ';').Select(x => new MinMaxD(x.Split(',').Select(x => Double.Parse(x)).ToArray())).ToArray();
            UnitLimits = new Dictionary<UnitFamily, MinMaxD[]>();
            foreach (string key in ini.GetKeysInSection("UnitLimits"))
            {
                UnitLimits.Add((UnitFamily)Enum.Parse(typeof(UnitFamily), key, true), ini.GetValueArray<string>("UnitLimits", key, ';').Select(x => new MinMaxD(x.Split(',').Select(x => Double.Parse(x)).ToArray())).ToArray());
            }
        }


    }
}