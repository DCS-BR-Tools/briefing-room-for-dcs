-- ===================================================================================
-- 3.1 - MAIN BRIEFINGROOM TABLE AND CORE FUNCTIONS
-- ===================================================================================

briefingRoom.mission = {} -- Main BriefingRoom mission table
briefingRoom.mission.complete = false -- Is the mission complete?
briefingRoom.mission.coreFunctions = { }
briefingRoom.mission.hasStarted = false -- has at least one player taken off?
briefingRoom.mission.autoEnd = $ENDMISSIONAUTOMATICALLY$
briefingRoom.mission.commandEnd = $ENDMISSIONONCOMMAND$
briefingRoom.mission.MapMarkers = $SHOWMAPMARKERS$
briefingRoom.mission.objectiveDropDistanceMeters = $DROPOFFDISTANCEMETERS$

-- Marks objective with index index as complete, and completes the mission itself if all objectives are complete
function briefingRoom.mission.coreFunctions.completeObjective(index, failed)
  failed = failed or false
  local revealObjective = 0
  if briefingRoom.mission.complete then return end -- mission already complete
  if briefingRoom.mission.objectives[index].complete and briefingRoom.mission.objectives[index].failed == failed then return end -- objective already complete with same fail state

  local objName = briefingRoom.mission.objectives[index].name
  briefingRoom.debugPrint("Objective "..objName.." marked as "..(failed and "failed" or"complete"))
  briefingRoom.mission.objectives[index].complete = true
  briefingRoom.mission.objectives[index].failed = failed
  briefingRoom.aircraftActivator.pushFromReserveQueue() -- activate next batch of aircraft (so more CAP will pop up)
  for k,objective in pairs(briefingRoom.mission.objectives) do
    if objective ~= nil and objective.progressionHidden and briefingRoom.mission.coreFunctions.assesCondition(objective.progressionCondition) then
      local minsPassed = math.floor((timer.getAbsTime() - timer.getTime0())/60)
      objective.startMinutes = minsPassed
      local acGroup = Group.getByName(objective.groupName) -- get the group
      if acGroup ~= nil then -- activate the group, if it exists
        acGroup:activate()
        local Start = {
          id = 'Start',
          params = {
          }
        }
        acGroup:getController():setCommand(Start)
        briefingRoom.debugPrint("Activating objective group "..acGroup:getName())
        objective.progressionHidden = false
      else
        briefingRoom.debugPrint("Failed to activate objective group "..objective.name)
      end
      if objective ~= nil and objective.progressionHiddenBrief then
        objective.progressionHiddenBrief = false
        briefingRoom.f10MenuCommands.activateObjective(k)
        if objective.waypoint ~= nil and briefingRoom.mission.MapMarkers then
          local vec3pos = dcsExtensions.toVec3(objective.waypoint)
          trigger.action.textToAll(briefingRoom.playerCoalition, k * 100 , vec3pos,{0, 0, 0, .53} , {1, 1, 1, .53} , 15, true , objective.name)
        end
        revealObjective = revealObjective + 1
      end
    end
  end
  

  -- Remove objective menu from the F10 menu
  if briefingRoom.f10Menu.objectives[index] ~= nil then
    missionCommands.removeItemForCoalition(briefingRoom.playerCoalition, briefingRoom.f10Menu.objectives[index])
    briefingRoom.f10Menu.objectives[index] = nil
  end

  -- Add a little delay before playing the "mission/objective complete" sounds to make sure all "target destroyed", "target photographed", etc. sounds are done playing
  local completeCount = 0
  for k,v in pairs(briefingRoom.mission.objectives) do
    if v.complete then
      completeCount = completeCount + 1
    end
  end
  local missionOver = completeCount >= table.count(briefingRoom.mission.objectives)
  -- Debug missions called complete early
  if briefingRoom.printDebugMessages then
    briefingRoom.debugPrint("Objective Completion state: "..tostring(completeCount).."/"..tostring(table.count(briefingRoom.mission.objectives)).."=".. tostring(missionOver))
  end
  -- End Debug
  if missionOver then
    briefingRoom.debugPrint("Mission marked as complete")
    briefingRoom.mission.complete = true
    local hasFailed = false

    for k,v in pairs(briefingRoom.mission.objectives) do
      if v.failed then
        hasFailed = true
      end
    end

    briefingRoom.radioManager.play("$LANG_COMMAND$: "..(hasFailed and "$LANG_MISSIONCOMPLETEWITHFAILURES$" or "$LANG_MISSIONCOMPLETE$"), (hasFailed and  "RadioHQMissionFailed" or "RadioHQMissionComplete"), math.random(6, 8))
    trigger.action.setUserFlag("BR_MISSION_COMPLETE", true) -- Mark the mission complete internally, so campaigns can move to the next mission
    if briefingRoom.mission.autoEnd then
      trigger.action.setUserFlag("BR_END_MISSION", true) -- Set off end mission trigger
    end
    if briefingRoom.mission.commandEnd then
      missionCommands.addCommandForCoalition(briefingRoom.playerCoalition, "$LANG_ENDMISSION$", nil, briefingRoom.f10MenuCommands.endMission, nil)
    end
  elseif not briefingRoom.mission.hasStarted then
    briefingRoom.radioManager.play("$LANG_AUTOCOMPLETEOBJECTIVE$", "Radio0", math.random(6, 8))
  else
    briefingRoom.radioManager.play("$LANG_COMMAND$: "..(failed and "$LANG_FAILEDOBJECTIVE$" or "$LANG_COMPLETEOBJECTIVE$"), (failed and "RadioHQObjectiveFailed" or "RadioHQObjectiveComplete"), math.random(6, 8))
    if revealObjective > 0 then 
      local time =  math.random(12, 15)
      for j = 1, revealObjective, 1 do
        if j > 1 then
          time = time + 4
        end
        briefingRoom.radioManager.play("$LANG_COMMAND$: $LANG_NEWOBJECTIVE$: "..briefingRoom.mission.objectives[index + j].task, "RadioHQNewObjective", time)
      end
    end
  end
end

function briefingRoom.mission.coreFunctions.assesCondition(condition)
    if condition == nil then return true end -- better to have a objective activate than not
    briefingRoom.debugPrint("Assessing raw condition: "..condition)
    local parsedCondition = string.gsub(condition, "(%d+)", "briefingRoom.mission.objectives[%1].complete == true")
    briefingRoom.debugPrint("Assessing parsed condition: "..parsedCondition)
    local f,err=loadstring("return "..parsedCondition)
    if f then
        return f()
    else
        briefingRoom.debugPrint("Condition Parsing Error: "..err)
        return true -- better to have a objective activate than not
    end
end

-- Begins the mission (called when the first player takes off)
function briefingRoom.mission.coreFunctions.beginMission()
  if briefingRoom.mission.hasStarted then return end -- mission has already started, do nothing

  briefingRoom.debugPrint("Mission has started")

  -- enable the aircraft activator and start spawning aircraft
  briefingRoom.mission.hasStarted = true
  timer.scheduleFunction(briefingRoom.aircraftActivator.update, nil, timer.getTime() + briefingRoom.aircraftActivator.getRandomInterval())
end