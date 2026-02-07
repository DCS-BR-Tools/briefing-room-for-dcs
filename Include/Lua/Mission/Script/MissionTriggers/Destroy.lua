function briefingRoom.mission.objectivesTriggersCommon.registerDestroyTrigger(objectiveIndex)
  table.insert(briefingRoom.mission.objectiveTriggers, function(event)

    if briefingRoom.mission.objectivesTriggersCommon.isMissionOrObjectiveComplete(objectiveIndex) then return false end

    -- Check if event is a "destruction" event
    local destructionEvent = false
    local killedUnit = event.initiator
    local playerName = nil
    if event.id == world.event.S_EVENT_KILL then
      destructionEvent = true
      killedUnit = event.target
      playerName = event.initiator ~= nil and event.initiator.getPlayerName and event.initiator:getPlayerName() or nil
    elseif 
      briefingRoom.mission.isSoftKillEvent(event.id) or
      (event.id == world.event.S_EVENT_LAND and briefingRoom.mission.objectives[objectiveIndex].targetCategory == Unit.Category.HELICOPTER) then -- Check if parked AI Aircraft are damaged enough to be considered dead
      destructionEvent = true
    end
    -- "Landing" events are considered kills for helicopter targets

    if 
      not destructionEvent or
      killedUnit == nil or
      Object.getCategory(killedUnit) ~= Object.Category.UNIT and Object.getCategory(killedUnit) ~= Object.Category.STATIC
    then return false end

    return briefingRoom.mission.destroyCallout(objectiveIndex, killedUnit, event.id, playerName)
  end)
end