function briefingRoom.mission.objectivesTriggersCommon.registerFlyNearTrigger(objectiveIndex)
  local objective = briefingRoom.mission.objectives[objectiveIndex]
  objective.hideTargetCount = true
  table.insert(briefingRoom.mission.objectiveTimers,  function ()
    if briefingRoom.mission.objectivesTriggersCommon.isMissionOrObjectiveComplete(objectiveIndex) then return false end
    if objective.progressionHidden then return true end -- skip check until active

    local players = dcsExtensions.getAllPlayers()
  
    for _,p in ipairs(players) do
      for __,u in ipairs(briefingRoom.mission.objectives[objectiveIndex].unitNames) do
        local unit = dcsExtensions.getUnitOrStatic(u)
        if unit ~= nil then
          local vec2p = dcsExtensions.toVec2(p:getPoint())
          local vec2u = dcsExtensions.toVec2(unit:getPoint())
          local distance = dcsExtensions.getDistance(vec2p, vec2u);
          
          if distance < 3704 and math.abs(vec2p.y - vec2u.y) < 609.6 and p:inAir() then -- less than 2nm away on the X/Z axis, less than 2000 feet of altitude difference
            local playername = p.getPlayerName and p:getPlayerName() or nil
            if math.random(1, 2) == 1 then
              briefingRoom.radioManager.play((playername or"$LANG_PILOT$")..": $LANG_FLYNEAR1$", "RadioPilotTargetReconned1")
            else
              briefingRoom.radioManager.play((playername or"$LANG_PILOT$")..": $LANG_FLYNEAR2$", "RadioPilotTargetReconned2")
            end
            objective.unitNames = { }
            briefingRoom.mission.coreFunctions.completeObjective(objectiveIndex)
            return false
          end
        end
      end
    end
  end)
end