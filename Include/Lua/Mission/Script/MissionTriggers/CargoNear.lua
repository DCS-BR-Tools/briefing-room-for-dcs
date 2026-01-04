function briefingRoom.mission.objectivesTriggersCommon.fireCargoNearTrigger(objectiveIndex, unitName)
   if briefingRoom.mission.objectivesTriggersCommon.isMissionOrObjectiveComplete(objectiveIndex) then return false end
   table.removeValue(briefingRoom.mission.objectives[objectiveIndex].unitNames, unitName)
   briefingRoom.radioManager.play("$LANG_PILOT$: $LANG_CARGODELIVERED$", "RadioPilotCargoDelivered")
    if table.count(briefingRoom.mission.objectives[objectiveIndex].unitNames) < 1 then
      briefingRoom.mission.coreFunctions.completeObjective(objectiveIndex)
    end
end
