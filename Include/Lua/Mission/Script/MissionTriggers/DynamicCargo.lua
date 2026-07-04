function briefingRoom.mission.objectivesTriggersCommon.registerDynamicCargoTask(objectiveIndex, airbaseName, itemName, extraCount)
  local requiredCount = -1
  local objective = briefingRoom.mission.objectives[objectiveIndex]
  local nextRunAt = 0
  local activeIntervalSeconds = 3
  local hiddenIntervalSeconds = 5
  objective.hideTargetCount = true
  table.insert(briefingRoom.mission.objectiveTimers,  function ()
    local now = timer.getAbsTime()
    if now < nextRunAt then return true end
    if briefingRoom.mission.objectivesTriggersCommon.isMissionOrObjectiveComplete(objectiveIndex) then return false end
    if objective.progressionHidden then
      nextRunAt = now + hiddenIntervalSeconds
      return true -- skip check until active
    end
    nextRunAt = now + activeIntervalSeconds
    local w = Airbase.getByName(airbaseName):getWarehouse()
    local count = w:getItemCount(itemName)
    if(requiredCount == -1) then
      requiredCount = count + extraCount
      briefingRoom.debugPrint("Dynamic Cargo Task started for "..itemName.." at "..airbaseName..", current: "..count.." extra count: "..extraCount.." required count: "..requiredCount, 1)
    end
    if count > requiredCount then
      briefingRoom.radioManager.play("$LANG_PILOT$: $LANG_CARGODELIVERED$", "RadioPilotCargoDelivered")
      briefingRoom.mission.coreFunctions.completeObjective(objectiveIndex)
      return false
    end
  end)
end