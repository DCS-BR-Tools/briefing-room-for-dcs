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
using System.Linq;
using LuaTableSerializer;

namespace BriefingRoom4DCS.Data
{
    internal class DBEntryUnitRadioPreset
    {
        internal int[] Modulations { get; }

        internal double[] Channels { get; }

        internal RadioType Type { get; }

        internal DBEntryUnitRadioPreset(double[] channels, int[] modulations, RadioType type)
        {
            Channels = channels;
            Modulations = modulations;
            Type = type;
        }

        internal DBEntryUnitRadioPresetModified SetOverrides(double radioFrequency, int radioModulation, double? atcRadioFrequency, int? atcRadioModulation)
        {
            if (Type == RadioType.Air)
                return new DBEntryUnitRadioPresetModified(this, radioFrequency, radioModulation);
            else if (Type == RadioType.ATC)
                return new DBEntryUnitRadioPresetModified(this, atcRadioFrequency, atcRadioModulation);
            else
                return new DBEntryUnitRadioPresetModified(this, null, null);
        }
    }

    internal class DBEntryUnitRadioPresetModified
    {
        internal int[] Modulations { get; }

        internal double[] Channels { get; }

        internal RadioType Type { get; }

        internal DBEntryUnitRadioPresetModified(DBEntryUnitRadioPreset preset, double? overrideFrequency, int? overrideModulation)
        {
            Channels = preset.Channels.Select(x => x).ToArray();
            if (overrideFrequency.HasValue)
                Channels[0] = overrideFrequency.Value;
            Modulations = preset.Modulations.Select(x => x).ToArray();
            if (overrideModulation.HasValue)
                Modulations[0] = overrideModulation.Value;
            Type = preset.Type;
        }

        public string ToLuaString()
        {
            return LuaSerializer.Serialize(new Dictionary<string, object>{
                {"modulations", Modulations},
                {"channels", Channels}
            });
        }
    }
}
