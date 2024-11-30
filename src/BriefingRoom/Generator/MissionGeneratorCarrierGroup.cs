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
using System.Collections.Generic;
using System.Linq;

namespace BriefingRoom4DCS.Generator
{
    internal class MissionGeneratorCarrierGroup
    {


        internal static void GenerateCarrierGroup(ref DCSMission mission)
        {
            DBEntryTheater theaterDB = Database.Instance.GetEntry<DBEntryTheater>(mission.TemplateRecord.ContextTheater);
            double carrierSpeed = Math.Max(
                    Database.Instance.Common.CarrierGroup.MinimumCarrierSpeed,
                    Database.Instance.Common.CarrierGroup.IdealWindOfDeck - mission.WindSpeedAtSeaLevel);
            if (mission.WindSpeedAtSeaLevel == 0) // No wind? Pick a random direction so carriers don't always go to a 0 course when wind is calm.
                mission.WindDirectionAtSeaLevel = Toolbox.RandomDouble(Toolbox.TWO_PI);
            var carrierPathDeg = ((mission.WindDirectionAtSeaLevel + Math.PI) % Toolbox.TWO_PI) * Toolbox.RADIANS_TO_DEGREES;
            var usedCoordinates = new List<Coordinates>();
            var templatesDB = Database.Instance.GetAllEntries<DBEntryTemplate>();
            foreach (MissionTemplateFlightGroupRecord flightGroup in mission.TemplateRecord.PlayerFlightGroups)
            {
                if (string.IsNullOrEmpty(flightGroup.Carrier) || mission.CarrierDictionary.ContainsKey(flightGroup.Carrier)) continue;
                if (templatesDB.Where(x => x.Type == "FOB").Any(x => x.ID == flightGroup.Carrier))
                {
                    //It Carries therefore carrier not because I can't think of a name to rename this lot
                    GenerateFOB(flightGroup, mission);
                    continue;
                }
                var initalUnitDB = Database.Instance.GetEntry<DBEntryJSONUnit>(flightGroup.Carrier);
                var unitDB = (DBEntryShip)initalUnitDB;
                if ((unitDB == null) || !unitDB.Families.Any(x => x.IsCarrier())) continue; // Unit doesn't exist or is not a carrier

                var (shipCoordinates, shipDestination) = GetSpawnAndDestination(mission, usedCoordinates, carrierPathDeg, flightGroup);
                usedCoordinates.Add(shipCoordinates);
                string cvnID = mission.CarrierDictionary.Count > 0 ? (mission.CarrierDictionary.Count + 1).ToString() : "";
                int ilsChannel = 11 + mission.CarrierDictionary.Count;
                int link4Frequency = 336 + mission.CarrierDictionary.Count;
                double radioFrequency = 127.5 + mission.CarrierDictionary.Count;
                string tacanCallsign = $"CVN{cvnID}";
                int tacanChannel = 74 + mission.CarrierDictionary.Count;
                var extraSettings = new Dictionary<string, object>{
                        {"GroupX2", shipDestination.X},
                        {"GroupY2", shipDestination.Y},
                        {"ILS", ilsChannel},
                        {"RadioBand", (int)RadioModulation.AM},
                        {"RadioFrequency", GeneratorTools.GetRadioFrequency(radioFrequency)},
                        {"Speed", carrierSpeed},
                        {"TACANCallsign", tacanCallsign},
                        {"TACANChannel", tacanChannel},
                        {"TACANFrequency", GeneratorTools.GetTACANFrequency(tacanChannel, 'X', false)},
                        {"Link4Frequency", GeneratorTools.GetRadioFrequency(link4Frequency)},
                        {"TACANMode"," X"},
                        {"playerCanDrive", false},
                        {"NoCM", true}};
                var templateOps = templatesDB.Where(x => x.Units.First().DCSID == unitDB.DCSID).ToList();
                UnitMakerGroupInfo? groupInfo;
                var groupLua = "ShipCarrier";
                var unitLua = "Ship";
                if (templateOps.Count > 0)
                    groupInfo = UnitMaker.AddUnitGroupTemplate(ref mission,Toolbox.RandomFrom(templateOps), Side.Ally, groupLua, unitLua, shipCoordinates, 0, extraSettings);
                else
                    groupInfo = UnitMaker.AddUnitGroup(ref mission,unitDB.DCSID, Side.Ally, unitDB.Families[0], groupLua, unitLua, shipCoordinates, 0, extraSettings);

                if (!groupInfo.HasValue || (groupInfo.Value.UnitNames.Length == 0)) continue; // Couldn't generate group

                mission.Briefing.AddItem(
                    DCSMissionBriefingItemType.Airbase,
                    $"{unitDB.UIDisplayName.Get(mission.LangKey)}\t-\t{GeneratorTools.FormatRadioFrequency(radioFrequency)}\t{ilsChannel}\t{tacanCallsign}, {tacanChannel}X\t{link4Frequency}");

                mission.CarrierDictionary.Add(flightGroup.Carrier, new CarrierUnitMakerGroupInfo(groupInfo.Value, unitDB.PlaneStorage, unitDB.HelicopterStorage, mission.TemplateRecord.ContextPlayerCoalition));
                mission.MapData.Add($"CARRIER_{flightGroup.Carrier}", new List<double[]> { groupInfo.Value.Coordinates.ToArray() });
            }
        }

        private static Tuple<Coordinates, Coordinates> GetSpawnAndDestination(
            DCSMission mission,
            List<Coordinates> usedCoordinates,
            double carrierPathDeg, MissionTemplateFlightGroupRecord flightGroup)
        {
            var travelMinMax = new MinMaxD(Database.Instance.Common.CarrierGroup.CourseLength, Database.Instance.Common.CarrierGroup.CourseLength * 2);
            Coordinates? carrierGroupCoordinates = null;
            Coordinates? destinationPoint = null;
            var iteration = 0;
            var maxDistance = 15;
            var usingHint = mission.TemplateRecord.CarrierHints.ContainsKey(flightGroup.Carrier);
            var location = mission.ObjectivesCenter;
            if (usingHint)
            {
                location = new Coordinates(mission.TemplateRecord.CarrierHints[flightGroup.Carrier]);
                if (!UnitMakerSpawnPointSelector.CheckInSea(mission.TheaterDB, location))
                    throw new BriefingRoomException(mission.LangKey, "CarrierHintonShore");
            }
            while (iteration < 5)
            {
                iteration++;
                carrierGroupCoordinates = usingHint ? location : UnitMakerSpawnPointSelector.GetRandomSpawnPoint(
                    ref mission,
                    new SpawnPointType[] { SpawnPointType.Sea },
                    mission.PlayerAirbase.Coordinates,
                    new MinMaxD(10, maxDistance),
                    mission.ObjectivesCenter,
                    new MinMaxD(10, mission.TemplateRecord.FlightPlanObjectiveDistance.Max * .75),
                    mission.TemplateRecord.OptionsMission.Contains("CarrierAllWaters") ? null : GeneratorTools.GetSpawnPointCoalition(mission.TemplateRecord, Side.Ally));
                if (!carrierGroupCoordinates.HasValue)
                    continue;
                var minDist = usedCoordinates.Aggregate(99999999.0, (acc, x) => x.GetDistanceFrom(carrierGroupCoordinates.Value) < acc ? x.GetDistanceFrom(carrierGroupCoordinates.Value) : acc);
                if (minDist < Database.Instance.Common.CarrierGroup.ShipSpacing)
                    continue;

                var destIteration = 0;
                while (destIteration < 5)
                {
                    destIteration++;
                    var distance = travelMinMax.Min + ((travelMinMax.Max - travelMinMax.Min) / destIteration);
                    destinationPoint = Coordinates.FromAngleAndDistance(carrierGroupCoordinates.Value, distance, carrierPathDeg);
                    if (UnitMakerSpawnPointSelector.CheckInSea(mission.TheaterDB ,destinationPoint.Value))
                        break;
                }
                if (UnitMakerSpawnPointSelector.CheckInSea(mission.TheaterDB ,destinationPoint.Value))
                    break;
            }

            if (!carrierGroupCoordinates.HasValue)
                throw new BriefingRoomException(mission.LangKey, "CarrierSpawnPointNotFound");
            if (!destinationPoint.HasValue)
                throw new BriefingRoomException(mission.LangKey, "CarrierDestinationNotFound");
            if (!UnitMakerSpawnPointSelector.CheckInSea(mission.TheaterDB ,destinationPoint.Value))
                throw new BriefingRoomException(mission.LangKey, "CarrierWaypointOnShore");
            if (!ShapeManager.IsLineClear(carrierGroupCoordinates.Value, destinationPoint.Value, mission.TheaterDB.WaterExclusionCoordinates))
                throw new BriefingRoomException(mission.LangKey, "CarrierPassesThrougLand");

            return new(carrierGroupCoordinates.Value, destinationPoint.Value);
        }

        private static void GenerateFOB(
            MissionTemplateFlightGroupRecord flightGroup,
            DCSMission mission)
        {
            DBEntryTheater theaterDB = Database.Instance.GetEntry<DBEntryTheater>(mission.TemplateRecord.ContextTheater);
            if (theaterDB == null) return; // Theater doesn't exist. Should never happen.

            var usingHint = mission.TemplateRecord.CarrierHints.ContainsKey(flightGroup.Carrier);
            var defaultFlightDistance = mission.PlayerAirbase.Coordinates.GetDistanceFrom(mission.ObjectivesCenter);
            var location = Coordinates.Lerp(mission.ObjectivesCenter, mission.PlayerAirbase.Coordinates, (defaultFlightDistance > 90 ? 0.3 : 0.5));
            if (usingHint)
                location = new Coordinates(mission.TemplateRecord.CarrierHints[flightGroup.Carrier]);

            Coordinates? spawnPoint = UnitMakerSpawnPointSelector.GetNearestSpawnPoint(ref mission,new SpawnPointType[] { SpawnPointType.LandLarge }, location);

            if (!spawnPoint.HasValue)
            {
                throw new BriefingRoomException(mission.LangKey, "NoFOBAirDefenseSpawnPoint");
            }

            var fobTemplate = Database.Instance.GetEntry<DBEntryTemplate>(flightGroup.Carrier);
            if (fobTemplate == null) return; // Unit doesn't exist or is not a carrier

            double radioFrequency = 127.5 + mission.CarrierDictionary.Count;
            var FOBNames = new List<string>{
                "FOB_London",
                "FOB_Dallas",
                "FOB_Paris",
                "FOB_Moscow",
                "FOB_Berlin"
            };
            var radioFrequencyValue = GeneratorTools.GetRadioFrequency(radioFrequency);
            var groupInfo =
                UnitMaker.AddUnitGroupTemplate(
                    ref mission,
                    fobTemplate, Side.Ally,
                    "Static", "StaticFOB",
                    spawnPoint.Value, 0,
                    new Dictionary<string, object>{
                    {"HeliportCallsignId", FOBNames.IndexOf(flightGroup.Carrier) + 1},
                    {"HeliportModulation", (int)RadioModulation.AM},
                    {"HeliportFrequency", GeneratorTools.FormatRadioFrequency(radioFrequency)},
                    {"RadioBand", (int)RadioModulation.AM},
                    {"RadioFrequency", radioFrequencyValue},
                    {"playerCanDrive", false},
                    {"NoCM", true}});
            if (!groupInfo.HasValue || (groupInfo.Value.UnitNames.Length == 0))
            {
                UnitMakerSpawnPointSelector.RecoverSpawnPoint(ref mission,spawnPoint.Value);
                return;
            }
            var unitDB = (DBEntryStatic)Database.Instance.GetEntry<DBEntryJSONUnit>(fobTemplate.Units.First().DCSID);
            groupInfo.Value.DCSGroup.Name = unitDB.UIDisplayName.Get(mission.LangKey);
            groupInfo.Value.DCSGroup.Units.First().Name = unitDB.UIDisplayName.Get(mission.LangKey);
            ZoneMaker.AddCTLDPickupZone(ref mission,spawnPoint.Value, true);
            mission.Briefing.AddItem(
                     DCSMissionBriefingItemType.Airbase,
                     $"{groupInfo.Value.Name}\t\t{GeneratorTools.FormatRadioFrequency(radioFrequency)}\t\t");
            mission.CarrierDictionary.Add(flightGroup.Carrier, new CarrierUnitMakerGroupInfo(groupInfo.Value, 4, 4, mission.TemplateRecord.ContextPlayerCoalition));
            mission.MapData.Add($"FOB_{flightGroup.Carrier}", new List<double[]> { groupInfo.Value.Coordinates.ToArray()
});

            foreach (var group in groupInfo.Value.DCSGroups)
            {
                var unit = group.Units.First();
                if (unit.DCSID != "FARP")
                    unit.UnitType = "StaticSupply";
            }
        }
    }
}
