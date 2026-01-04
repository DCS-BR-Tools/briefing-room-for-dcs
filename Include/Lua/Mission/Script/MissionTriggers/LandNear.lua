function briefingRoom.mission.objectivesTriggersCommon.registerLandNearTrigger(objectiveIndex)
  table.insert(briefingRoom.mission.objectiveTriggers, function(event)
    if briefingRoom.mission.objectivesTriggersCommon.isMissionOrObjectiveComplete(objectiveIndex) then return false end
    if event.id ~= world.event.S_EVENT_LAND then return false end -- Not a "land" event, nothing to do
  
    if event.initiator == nil then return false end -- Initiator was nil
    if Object.getCategory(event.initiator) ~= Object.Category.UNIT then return false end -- Initiator was not an unit
    if event.initiator:getCoalition() ~= briefingRoom.playerCoalition then return false end -- Initiator was not a friendy unit

    local position = dcsExtensions.toVec2(event.initiator:getPoint()) -- get the landing unit position

    -- check if any target unit is close enough from the landing unit
    -- if so, clean the target unit ID table and mark the objective as completed
    for _,id in ipairs(briefingRoom.mission.objectives[objectiveIndex].unitNames) do
      local targetUnit = dcsExtensions.getUnitOrStatic(id)
      if targetUnit ~= nil then
        local targetPosition = dcsExtensions.toVec2(targetUnit:getPoint())
        if dcsExtensions.getDistance(position, targetPosition) < 650 then
          briefingRoom.mission.objectives[objectiveIndex].unitNames = { }
          briefingRoom.mission.coreFunctions.completeObjective(objectiveIndex)
          return true
        end
      end
    end
end)

briefingRoom.mission.objectives[objectiveIndex].hideTargetCount = true
end