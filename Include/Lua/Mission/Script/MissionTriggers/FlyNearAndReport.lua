function briefingRoom.mission.objectivesTriggersCommon.flyNearAndReportComplete(args)
  local objectiveIndex = args[1]
  briefingRoom.radioManager.play("$LANG_PILOT$: $LANG_PILOTREPORTCOMPLETE$", "RadioPilotReportComplete", math.random(1, 3))
  briefingRoom.mission.coreFunctions.completeObjective(objectiveIndex)
  missionCommands.removeItemForCoalition(briefingRoom.playerCoalition, briefingRoom.mission.objectives[objectiveIndex].completeCommand)
end

function briefingRoom.mission.objectivesTriggersCommon.registerFlyNearAndReportTrigger(objectiveIndex)
  local objective = briefingRoom.mission.objectives[objectiveIndex]
  objective.completeCommand = nil


  table.insert(briefingRoom.mission.objectiveTimers,  function ()
    if objective.flownOver then return false end -- Objective complete, nothing to do
    if objective.progressionHidden then return true end -- skip check until active
  
    local players = dcsExtensions.getAllPlayers()
  
    for _,p in ipairs(players) do
      for __,u in ipairs(objective.unitNames) do
        local unit = dcsExtensions.getUnitOrStatic(u)
        if unit ~= nil then
          local vec2p = dcsExtensions.toVec2(p:getPoint())
          local vec2u = dcsExtensions.toVec2(unit:getPoint())
          local distance = dcsExtensions.getDistance(vec2p, vec2u);
  
          if distance < 9260 and math.abs(vec2p.y - vec2u.y) < 2438 and briefingRoom.mission.objectives[objectiveIndex].completeCommand == nil then
            local playername = p.getPlayerName and p:getPlayerName() or nil
            if math.random(1, 2) == 1 then
                briefingRoom.radioManager.play((playername or"$LANG_PILOT$").." $LANG_FLYNEAR1$", "RadioPilotTargetReconned1")
            else
                briefingRoom.radioManager.play((playername or"$LANG_PILOT$").." $LANG_FLYNEAR2$", "RadioPilotTargetReconned2")
            end
            objective.unitNames = { }
            if objective.startMinutes == -1 then -- start the objective
              local minsPassed = math.floor((timer.getAbsTime() - timer.getTime0())/60)
              objective.startMinutes = minsPassed
             end
            objective.completeCommand = missionCommands.addCommandForCoalition(briefingRoom.playerCoalition, "$LANG_REPORTCOMPLETE$", briefingRoom.f10Menu.objectives[objectiveIndex],  briefingRoom.mission.objectivesTriggersCommon.flyNearAndReportComplete, {objectiveIndex})
          end
        end
      end
    end
  end)
end