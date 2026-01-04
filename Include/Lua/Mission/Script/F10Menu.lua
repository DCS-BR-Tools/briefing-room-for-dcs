-- ===================================================================================
-- 3.2 - COMMON F10 MENU
-- ===================================================================================

-- Mission F10 menu hierarchy
briefingRoom.f10Menu = { }
briefingRoom.f10Menu.objectives = { }

 -- Mission F10 menu functions
briefingRoom.f10MenuCommands = { }
briefingRoom.f10MenuCommands.missionFeatures = { }

 -- Mission status menu
function briefingRoom.f10MenuCommands.missionStatus()
  local msnStatus = ""
  local msnSound = ""

  if briefingRoom.mission.complete then
    msnStatus = "$LANG_COMMAND$: Mission complete, you may return to base.\n\n"
    msnSound = "RadioHQMissionStatusComplete"
  else
    msnStatus = "$LANG_COMMAND$: Mission is still in progress.\n\n"
    msnSound = "RadioHQMissionStatusInProgress"
  end

  for i,o in ipairs(briefingRoom.mission.objectives) do
    if o.progressionHiddenBrief == false then
      if o.complete then
        msnStatus = msnStatus..(o.failed and "[/]" or "[X]")
      else
        msnStatus = msnStatus.."[ ]"
      end

      local objectiveProgress = ""
      if o.unitsCount > 0 and o.hideTargetCount ~= true then
        local targetsDone = math.max(0, o.unitsCount - table.count(o.unitNames))
        objectiveProgress = " ("..tostring(targetsDone).."/"..tostring(o.unitsCount)..")"
      end

      msnStatus = msnStatus.." "..o.task..objectiveProgress.."\n"
    end
  end

  briefingRoom.radioManager.play("$LANG_PILOT$: $LANG_MISSIONSTATUSREQUEST$", "RadioPilotMissionStatus")
  briefingRoom.radioManager.play(msnStatus, msnSound, briefingRoom.radioManager.getAnswerDelay())
end

function briefingRoom.f10MenuCommands.getWaypointCoordinates(index)
  local cooMessage = dcsExtensions.vec2ToStringCoordinates(briefingRoom.mission.objectives[index].waypoint)
  briefingRoom.radioManager.play("$LANG_PILOT$: $LANG_WAYPOINTREQUEST$", "RadioPilotWaypointCoordinates")
  briefingRoom.radioManager.play("$LANG_COMMAND$: $LANG_WAYPOINTRESPONSE$\n\n"..cooMessage, "RadioHQWaypointCoordinates", briefingRoom.radioManager.getAnswerDelay())
  
  local idx = briefingRoom.mission.objectives[index].waypointRadioCommandIndex
  missionCommands.removeItemForCoalition(briefingRoom.playerCoalition, briefingRoom.mission.objectives[index].f10Commands[idx].commandPath)
  briefingRoom.mission.objectives[index].f10Commands[idx].commandPath = missionCommands.addCommandForCoalition(briefingRoom.playerCoalition, "$LANG_WAYPOINTCOORDINATES$:\n"..cooMessage, briefingRoom.f10Menu.objectives[index], briefingRoom.f10MenuCommands.getWaypointCoordinates, index)
end

function briefingRoom.f10MenuCommands.endMission ()
  trigger.action.setUserFlag("BR_END_MISSION_NOW", true)
end

function briefingRoom.f10MenuCommands.activateObjective(i)
  briefingRoom.f10Menu.objectives[i] = missionCommands.addSubMenuForCoalition(briefingRoom.playerCoalition,  briefingRoom.mission.objectives[i].f10MenuText, briefingRoom.f10Menu.objectiveMenu)
  for j=1,table.count(briefingRoom.mission.objectives[i].f10Commands) do
    briefingRoom.mission.objectives[i].f10Commands[j].commandPath = missionCommands.addCommandForCoalition(briefingRoom.playerCoalition, briefingRoom.mission.objectives[i].f10Commands[j].text, briefingRoom.f10Menu.objectives[i] ,briefingRoom.mission.objectives[i].f10Commands[j].func, briefingRoom.mission.objectives[i].f10Commands[j].args)
  end
  if briefingRoom.printDebugMessages then
    missionCommands.addCommandForCoalition(briefingRoom.playerCoalition, "(DEBUG) Destroy target unit", briefingRoom.f10Menu.objectives[i] ,briefingRoom.f10MenuCommands.debug.destroySpecificTargetUnit, i)
  end
end

-- Common mission menu (mission status and mission features)
briefingRoom.f10Menu.missionMenu = missionCommands.addSubMenuForCoalition(briefingRoom.playerCoalition, "$LANG_MISSION$", nil)
missionCommands.addCommandForCoalition(briefingRoom.playerCoalition, "$LANG_MISSIONSTATUS$", briefingRoom.f10Menu.missionMenu, briefingRoom.f10MenuCommands.missionStatus, nil)

briefingRoom.f10Menu.objectiveMenu = missionCommands.addSubMenuForCoalition(briefingRoom.playerCoalition, "$LANG_OBJECTIVES$", nil)