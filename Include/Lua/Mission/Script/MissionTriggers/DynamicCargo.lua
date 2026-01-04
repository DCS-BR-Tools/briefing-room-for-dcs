function briefingRoom.mission.objectivesTriggersCommon.registerDynamicCargoTask(objectiveIndex, airbaseName, itemName, extraCount)
  local requiredCount = -1
  table.insert(briefingRoom.mission.objectiveTimers,  function ()
    if briefingRoom.mission.objectivesTriggersCommon.isMissionOrObjectiveComplete(objectiveIndex) then return false end
    local w = Airbase.getByName(airbaseName):getWarehouse()
    local count = w:getItemCount(itemName)
    if(requiredCount == -1) then
      requiredCount = count + extraCount
      briefingRoom.debugPrint("Dynamic Cargo Task started for "..itemName.." at "..airbaseName..", current: "..count.." extra count: "..extraCount.." required count: "..requiredCount, 1)
    end
    if count > requiredCount then
      briefingRoom.radioManager.play("$LANG_PILOT$: $LANG_CARGODELIVERED$", "RadioPilotCargoDelivered")
      briefingRoom.mission.coreFunctions.completeObjective(objectiveIndex)
      return nil
    end
  end)
  
  briefingRoom.mission.objectives[objectiveIndex].hideTargetCount = true
end