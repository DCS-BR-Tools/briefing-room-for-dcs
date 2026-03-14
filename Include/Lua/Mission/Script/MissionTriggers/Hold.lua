function briefingRoom.mission.objectivesTriggersCommon.registerHoldTrigger(objectiveIndex, distanceInMeters, timeRequiredSeconds, superiorityRequired)
  local objective = briefingRoom.mission.objectives[objectiveIndex]
  superiorityRequired = superiorityRequired or false
  briefingRoom.mission.objectives[objectiveIndex].superiortyTimer = 0
  objective.hideTargetCount = true
  table.insert(briefingRoom.mission.objectiveTimers,  function ()
    if briefingRoom.mission.objectivesTriggersCommon.isMissionOrObjectiveComplete(objectiveIndex) then return false end
    if objective.progressionHidden then return true end -- skip check until active
    local players = dcsExtensions.getAllPlayers()
  
    for _,p in ipairs(players) do
      local vec2p = dcsExtensions.toVec2(p:getPoint())
      for _,id in ipairs(briefingRoom.mission.objectives[objectiveIndex].unitNames) do
        local targetUnit = dcsExtensions.getUnitOrStatic(id)
        if targetUnit ~= nil then
          local targetPosition = dcsExtensions.toVec2(targetUnit:getPoint())
          local distance = dcsExtensions.getDistance(vec2p, targetPosition);
          if distance < distanceInMeters then
            if objective.startMinutes == -1 then -- start the objective
              local minsPassed = math.floor((timer.getAbsTime() - timer.getTime0())/60)
              objective.startMinutes = minsPassed
             end
            if superiorityRequired then
              for __,eu in ipairs(dcsExtensions.getCoalitionUnits(briefingRoom.enemyCoalition)) do
                local evec2u = dcsExtensions.toVec2(eu:getPoint())
                local edistance = dcsExtensions.getDistance(vec2p, evec2u);
                if edistance < distanceInMeters then
                  return false
                end
              end
            end
            objective.superiortyTimer = objective.superiortyTimer + 1
            briefingRoom.debugPrint("Player in zone "..tostring(objectiveIndex).." for "..tostring(objective.superiortyTimer).." seconds")
            if objective.superiortyTimer > timeRequiredSeconds then
              objective.unitNames = { }
              briefingRoom.mission.coreFunctions.completeObjective(objectiveIndex)
              return false
            end
          end
        end
      end
    end
  end)
end