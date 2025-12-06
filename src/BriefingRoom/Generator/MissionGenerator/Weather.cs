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

namespace BriefingRoom4DCS.Generator.Mission
{
    internal class Weather
    {

        internal static int GenerateWeather(IDatabase database, ref DCSMission mission)
        {
            var baseAlt = mission.TemplateRecord.OptionsMission.Contains("SeaLevelRefCloud") ? 0.0 : mission.PlayerAirbase.Elevation;
            if (mission.TemplateRecord.OptionsMission.Contains("HighCloud"))
                baseAlt += 2000;
            DBEntryWeatherPreset weatherDB;
            if (string.IsNullOrEmpty(mission.TemplateRecord.EnvironmentWeatherPreset)) // Random weather
                weatherDB = Toolbox.RandomFrom(database.GetAllEntries<DBEntryWeatherPreset>());
            else
                weatherDB = database.GetEntry<DBEntryWeatherPreset>(mission.TemplateRecord.EnvironmentWeatherPreset);

            mission.SetValue("WeatherName", weatherDB.BriefingDescription.Get(mission.LangKey));
            mission.SetValue("WeatherCloudsBase", weatherDB.CloudsBase.GetValue() + baseAlt);
            mission.SetValue("WeatherCloudsPreset", Toolbox.RandomFrom(weatherDB.CloudsPresets));
            mission.SetValue("WeatherCloudsThickness", weatherDB.CloudsThickness.GetValue());
            mission.SetValue("WeatherDust", weatherDB.Dust);
            mission.SetValue("WeatherDustDensity", weatherDB.DustDensity.GetValue());
            mission.SetValue("WeatherFog", weatherDB.Fog);
            mission.SetValue("WeatherFogThickness", weatherDB.FogThickness.GetValue());
            mission.SetValue("WeatherFogVisibility", weatherDB.FogVisibility.GetValue());
            mission.SetValue("WeatherQNH", weatherDB.QNH.GetValue());
            mission.SetValue("WeatherTemperature", mission.TheaterDB.Temperature[int.Parse(mission.GetValue("DateMonth")) - 1].GetValue());
            mission.SetValue("WeatherVisibility", weatherDB.Visibility.GetValue());

            return weatherDB.Turbulence.GetValue();
        }

        internal static Tuple<double, double> GenerateWind(IBriefingRoom briefingRoom, ref DCSMission mission, int turbulenceFromWeather)
        {
            var windSpeedAtSeaLevel = 0.0;
            var windDirectionAtSeaLevel = 0.0;

            Wind windLevel = mission.TemplateRecord.EnvironmentWind == Wind.Random ? PickRandomWindLevel() : mission.TemplateRecord.EnvironmentWind;
            BriefingRoom.PrintToLog($"Wind speed level set to \"{windLevel}\".");

            int windAverage = 0;
            for (int i = 0; i < 3; i++)
            {
                int windSpeed = briefingRoom.Database.Common.Wind[(int)windLevel].Wind.GetValue();
                int windDirection = windSpeed > 0 ? Toolbox.RandomInt(0, 360) : 0;
                if (i == 0)
                {
                    windSpeedAtSeaLevel = windSpeed;
                    windDirectionAtSeaLevel = windDirection * Toolbox.DEGREES_TO_RADIANS;
                }
                windAverage += windSpeed;

                mission.SetValue($"WeatherWindSpeed{i + 1}", windSpeed);
                mission.SetValue($"WeatherWindDirection{i + 1}", windDirection);
            }
            windAverage /= 3;

            mission.SetValue($"WeatherWindName", windLevel.ToString()); // TODO: get name from attribute
            mission.SetValue($"WeatherWindSpeedAverage", windAverage);
            mission.SetValue($"WeatherWindDirectionCardinal", GetCardinalWindDirection(int.Parse(mission.GetValue("WeatherWindDirection1"))));

            mission.SetValue("WeatherGroundTurbulence", briefingRoom.Database.Common.Wind[(int)windLevel].Turbulence.GetValue() + turbulenceFromWeather);
            return new(windSpeedAtSeaLevel, windDirectionAtSeaLevel);
        }

        private static Wind PickRandomWindLevel()
        {
            return Toolbox.RandomFrom(
                Wind.Calm, Wind.Calm, Wind.Calm, Wind.Calm, Wind.Calm,
                Wind.LightBreeze, Wind.LightBreeze, Wind.LightBreeze, Wind.LightBreeze,
                Wind.ModerateBreeze, Wind.ModerateBreeze,
                Wind.StrongBreeze);
        }

        private static string GetCardinalWindDirection(int angle)
        {
            return angle switch
            {
                > 348 or < 11 => "N",
                < 35 => "NNE",
                < 57 => "NE",
                < 79 => "ENE",
                < 102 => "E",
                < 124 => "ESE",
                < 147 => "SE",
                < 169 => "SSE",
                < 192 => "S",
                < 214 => "SSW",
                < 237 => "SW",
                < 259 => "WSW",
                < 282 => "W",
                < 303 => "WNW",
                < 326 => "NW",
                < 349 => "NNW",
            };
        }
    }
}
