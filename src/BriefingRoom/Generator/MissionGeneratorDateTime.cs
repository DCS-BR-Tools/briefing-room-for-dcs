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
using System;

namespace BriefingRoom4DCS.Generator
{
    internal class MissionGeneratorDateTime
    {

        internal static Month GenerateMissionDate(ref DCSMission mission)
        {
            int day;
            Month month;

            // Select a random year from the most recent coalition's decade.
            var year = Toolbox.GetRandomYearFromDecade(mission.TemplateRecord.ContextDecade);

            BriefingRoom.PrintToLog($"No fixed date provided in the mission template, generating date in decade {mission.TemplateRecord.ContextDecade}");

            if (mission.TemplateRecord.EnvironmentSeason == Season.Random) // Random season, pick any day of the year.
            {
                month = (Month)Toolbox.RandomInt(12);
                day = Toolbox.RandomMinMax(1, GeneratorTools.GetDaysPerMonth(month, year));
            }
            else // Pick a date according to the desired season
            {
                Month[] seasonMonths = GetMonthsForSeason(mission.TemplateRecord.EnvironmentSeason,  mission.TheaterDB);

                int monthIndex = Toolbox.RandomInt(4);
                month = seasonMonths[monthIndex];
                day = monthIndex switch
                {
                    // First month of the season, season begins on the 21st
                    0 => Toolbox.RandomMinMax(21, GeneratorTools.GetDaysPerMonth(month, year)),
                    // Last month of the season, season ends on the 20th
                    3 => Toolbox.RandomMinMax(1, 20),
                    _ => Toolbox.RandomMinMax(1, GeneratorTools.GetDaysPerMonth(month, year)),
                };
            }

            mission.SetValue("DateDay", day);
            mission.SetValue("DateMonth", (int)month + 1);
            mission.SetValue("DateYear", year);
            mission.SetValue("BriefingDate", $"{day:00}/{(int)month + 1:00}/{year:0000}");

            BriefingRoom.PrintToLog($"Misson date set to {day} {month} {year}.");
            return month;
        }

        internal static void GenerateMissionTime(ref DCSMission mission, Month month)
        {
            int hour, minute;
            var totalMinutes = mission.TemplateRecord.EnvironmentTimeOfDay switch
            {
                TimeOfDay.RandomDaytime => Toolbox.RandomInt(mission.TheaterDB.DayTime[(int)month].Min, mission.TheaterDB.DayTime[(int)month].Max - 60),
                TimeOfDay.Dawn => Toolbox.RandomInt(mission.TheaterDB.DayTime[(int)month].Min, mission.TheaterDB.DayTime[(int)month].Min + 120),
                TimeOfDay.Noon => Toolbox.RandomInt(
                                        (mission.TheaterDB.DayTime[(int)month].Min + mission.TheaterDB.DayTime[(int)month].Max) / 2 - 90,
                                        (mission.TheaterDB.DayTime[(int)month].Min + mission.TheaterDB.DayTime[(int)month].Max) / 2 + 90),
                TimeOfDay.Twilight => Toolbox.RandomInt(mission.TheaterDB.DayTime[(int)month].Max - 120, mission.TheaterDB.DayTime[(int)month].Max + 30),
                TimeOfDay.Night => Toolbox.RandomInt(0, mission.TheaterDB.DayTime[(int)month].Min - 120),
                // case TimeOfDay.Random
                _ => (double)Toolbox.RandomInt(Toolbox.MINUTES_PER_DAY),
            };
            hour = Toolbox.Clamp((int)Math.Floor(totalMinutes / 60), 0, 23);
            minute = Toolbox.Clamp((int)Math.Floor((totalMinutes - hour * 60) / 15) * 15, 0, 45);

            mission.SetValue("BriefingTime", $"{hour:00}:{minute:00}");
            mission.SetValue("StartTime", hour * 3600 + minute * 60); // DCS World time is stored in seconds since midnight
        }

        private static Month[] GetMonthsForSeason(Season season, DBEntryTheater theaterDB)
        {
            if(theaterDB.SouthernHemisphere)
            {
                return season switch
                {
                    Season.Summer => new Month[] { Month.December, Month.January, Month.February, Month.March },
                    Season.Fall => new Month[] { Month.March, Month.April, Month.May, Month.June },
                    Season.Winter => new Month[] { Month.June, Month.July, Month.August, Month.September },
                    _ => new Month[] { Month.September, Month.October, Month.November, Month.December },// case Season.Spring or Season.Random
                };
            }

            return season switch
            {
                Season.Summer => new Month[] { Month.June, Month.July, Month.August, Month.September },
                Season.Fall => new Month[] { Month.September, Month.October, Month.November, Month.December },
                Season.Winter => new Month[] { Month.December, Month.January, Month.February, Month.March },
                _ => new Month[] { Month.March, Month.April, Month.May, Month.June },// case Season.Spring or Season.Random
            };
        }
    }
}
