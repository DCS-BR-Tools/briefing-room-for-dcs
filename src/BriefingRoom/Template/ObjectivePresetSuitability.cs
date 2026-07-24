using System.Collections.Generic;
using System.Linq;
using BriefingRoom4DCS.Data;

namespace BriefingRoom4DCS.Template
{
    internal enum ObjectivePresetUnsuitabilityReason
    {
        None,
        AircraftTypeMismatch,
        TransportRequired,
        PlaneTargetsRotorOnly
    }

    internal readonly record struct ObjectivePresetSuitabilityProfile(
        bool HasFixedWingAircraft,
        bool HasRotorAircraft,
        bool HasTransportAircraft)
    {
        internal bool IsRotorOnly => HasRotorAircraft && !HasFixedWingAircraft;

        internal static ObjectivePresetSuitabilityProfile FromPlayerFlightGroups(
            IDatabase database,
            IEnumerable<MissionTemplateFlightGroup> playerFlightGroups)
        {
            var playerAircraft = playerFlightGroups
                .Where(x => !x.Hostile)
                .Select(x => database.GetEntry<DBEntryJSONUnit>(x.Aircraft))
                .Where(x => x != null)
                .ToList();

            return new ObjectivePresetSuitabilityProfile(
                playerAircraft.Any(x => x.Category == UnitCategory.Plane),
                playerAircraft.Any(x => x.Category == UnitCategory.Helicopter),
                playerAircraft.Any(x =>
                    x.Families.Contains(UnitFamily.PlaneTransport) ||
                    x.Families.Contains(UnitFamily.HelicopterTransport)));
        }
    }

    internal static class ObjectivePresetSuitability
    {
        private static readonly HashSet<string> RANDOM_PRESET_IDS = new() { "Random", "RandomFixedWing", "RandomRotor" };
        private static readonly HashSet<string> TRANSPORT_TASK_IDS = new() { "ExtractTroops", "LandNearAlly", "LandNearEnemy", "TransportCargo", "TransportTroops" };

        internal static bool IsDynamicSpawnEnabled(CampaignTemplate template) =>
            template.AirbaseDynamicSpawn != DsAirbase.None || template.CarrierDynamicSpawn;

        internal static string SelectPreset(IDatabase database, string presetId, ObjectivePresetSuitabilityProfile profile)
        {
            if (!RANDOM_PRESET_IDS.Contains(presetId))
                return presetId;

            var unsuitabilityReason = GetUnsuitabilityReason(database, presetId, profile);
            if (unsuitabilityReason != ObjectivePresetUnsuitabilityReason.None)
                return presetId;

            var compatiblePresets = database.GetAllEntries<DBEntryObjectivePreset>()
                .Where(x => !RANDOM_PRESET_IDS.Contains(x.ID))
                .Where(x => GetUnsuitabilityReason(database, x.ID, profile) == ObjectivePresetUnsuitabilityReason.None)
                .Select(x => x.ID)
                .ToList();

            return compatiblePresets.Count > 0 ? Toolbox.RandomFrom(compatiblePresets) : presetId;
        }

        internal static ObjectivePresetUnsuitabilityReason GetUnsuitabilityReason(
            IDatabase database,
            string presetId,
            ObjectivePresetSuitabilityProfile profile)
        {
            if (presetId == "Random")
                return ObjectivePresetUnsuitabilityReason.None;

            if (presetId == "RandomFixedWing")
                return profile.HasFixedWingAircraft
                    ? ObjectivePresetUnsuitabilityReason.None
                    : ObjectivePresetUnsuitabilityReason.AircraftTypeMismatch;

            if (presetId == "RandomRotor")
                return profile.HasRotorAircraft
                    ? ObjectivePresetUnsuitabilityReason.None
                    : ObjectivePresetUnsuitabilityReason.AircraftTypeMismatch;

            var preset = database.GetEntry<DBEntryObjectivePreset>(presetId);
            if (preset == null)
                return ObjectivePresetUnsuitabilityReason.None;

            if (!profile.HasTransportAircraft && TRANSPORT_TASK_IDS.Contains(preset.Task))
                return ObjectivePresetUnsuitabilityReason.TransportRequired;

            if (profile.IsRotorOnly && PresetTargetsPlanes(database, preset))
                return ObjectivePresetUnsuitabilityReason.PlaneTargetsRotorOnly;

            return ObjectivePresetUnsuitabilityReason.None;
        }

        internal static string GetPresetDisplayName(IDatabase database, string langKey, string presetId)
        {
            var preset = database.GetEntry<DBEntryObjectivePreset>(presetId);
            return preset?.UIDisplayName.Get(langKey) ?? presetId;
        }

        private static bool PresetTargetsPlanes(IDatabase database, DBEntryObjectivePreset preset) =>
            preset.Targets
                .Select(database.GetEntry<DBEntryObjectiveTarget>)
                .Where(x => x != null)
                .Any(x => x.UnitCategory == UnitCategory.Plane);
    }
}
