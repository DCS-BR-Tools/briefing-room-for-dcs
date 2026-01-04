function briefingRoom.mission.objectiveFeaturesCommon.registerEmitRadioTransmission(objectiveIndex)
    local unit = dcsExtensions.getUnitOrStatic(briefingRoom.mission.objectives[objectiveIndex].unitNames[1])
    trigger.action.radioTransmission('l10n/DEFAULT/resources/ogg/FXRadioSignal.ogg', unit:getPoint(), 0, true, 124000000,
        100, "-- Morse Code --")
end
