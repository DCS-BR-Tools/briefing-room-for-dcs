function briefingRoom.mission.objectivesTriggersCommon.registerDisableTrigger(objectiveIndex)
  table.insert(briefingRoom.mission.objectiveTriggers, function(event)
    -- Mission complete, nothing to do
    if briefingRoom.mission.objectivesTriggersCommon.isMissionOrObjectiveComplete(objectiveIndex) then return false end

    local killedUnit = event.initiator
    local playerName = nil
    if event.id == world.event.S_EVENT_KILL then
        if event.target == nil then return false end
        killedUnit = event.target
        playerName = event.initiator ~= nil and event.initiator.getPlayerName and event.initiator:getPlayerName() or nil
    elseif  event.id == world.event.S_EVENT_HIT then -- unit was hit but not destroyed, check anyway because destroying a parked aircraft in DCS is HARD, and any aircraft with less than 90% hp left is not airworthy
        if event.target == nil then return false end
        if event.target:getCategory() ~= Object.Category.UNIT then return false end -- target was not a unit
        local life = event.target:getLife() / event.target:getLife0()
        if life > .9 then return false end -- not damaged enough
        killedUnit = event.target
    elseif briefingRoom.mission.isSoftKillEvent(event.id) then -- unit destroyed
        if event.initiator == nil then return false end
    else return false end
    return  briefingRoom.mission.destroyCallout(objectiveIndex, killedUnit, event.id, playerName)
  end)
end