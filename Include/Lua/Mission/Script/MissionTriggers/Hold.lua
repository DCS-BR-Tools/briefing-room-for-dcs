function briefingRoom.mission.objectivesTriggersCommon.registerHoldTrigger(objectiveIndex, distanceInMeters, timeRequiredSeconds, superiorityRequired)
  superiorityRequired = superiorityRequired or false
  briefingRoom.mission.objectives[objectiveIndex].superiortyTimer = 0
  table.insert(briefingRoom.mission.objectiveTimers,  function ()
    if briefingRoom.mission.objectivesTriggersCommon.isMissionOrObjectiveComplete(objectiveIndex) then return false end
    local players = dcsExtensions.getAllPlayers()
  
    for _,p in ipairs(players) do
      local vec2p = dcsExtensions.toVec2(p:getPoint())
      for _,id in ipairs(briefingRoom.mission.objectives[objectiveIndex].unitNames) do
        local targetUnit = dcsExtensions.getUnitOrStatic(id)
        if targetUnit ~= nil then
          local targetPosition = dcsExtensions.toVec2(targetUnit:getPoint())
          local distance = dcsExtensions.getDistance(vec2p, targetPosition);
          if distance < distanceInMeters then -- less than 2nm
            if superiorityRequired then
              for __,eu in ipairs(dcsExtensions.getCoalitionUnits(briefingRoom.enemyCoalition)) do
                local evec2u = dcsExtensions.toVec2(eu:getPoint())
                local edistance = dcsExtensions.getDistance(vec2p, evec2u);
                if edistance < distanceInMeters then
                  return false
                end
              end
            end
            briefingRoom.mission.objectives[objectiveIndex].superiortyTimer = briefingRoom.mission.objectives[objectiveIndex].superiortyTimer + 1
            briefingRoom.debugPrint("Player in zone "..tostring(objectiveIndex).." for "..tostring(briefingRoom.mission.objectives[objectiveIndex].superiortyTimer).." seconds")
            if briefingRoom.mission.objectives[objectiveIndex].superiortyTimer > timeRequiredSeconds then
              briefingRoom.mission.objectives[objectiveIndex].unitNames = { }
              briefingRoom.mission.coreFunctions.completeObjective(objectiveIndex)
              return nil
            end
          end
        end
      end
    end
  end)
  briefingRoom.mission.objectives[objectiveIndex].hideTargetCount = true
end