function briefingRoom.mission.objectivesTriggersCommon.escortNearTriggerlaunchMission (args)
  local objectiveIndex = args[1]
  local objective = briefingRoom.mission.objectives[objectiveIndex]
  briefingRoom.radioManager.play("$LANG_PILOT$: $LANG_ESCORTSTARTREQUEST$", "RadioPilotBeginEscort")
  local unit = dcsExtensions.getUnitOrStatic(briefingRoom.mission.objectives[objectiveIndex].unitNames[1])
  if unit ~= nil then
    local group = unit:getGroup()
    if group ~= nil then
      group:activate()
      briefingRoom.radioManager.play("$LANG_ESCORT$ "..objective.name..": $LANG_ESCORTAFFIRM$", "RadioEscortMoving", briefingRoom.radioManager.getAnswerDelay(), nil, nil)
    end
  end
end

function briefingRoom.mission.objectivesTriggersCommon.fireEscortNearTrigger(objectiveIndex)
   if briefingRoom.mission.objectivesTriggersCommon.isMissionOrObjectiveComplete(objectiveIndex) then return false end
    briefingRoom.radioManager.play("$LANG_PILOT$: $LANG_ESCORTCOMPLETE$", "RadioPilotEscortComplete")
    briefingRoom.mission.coreFunctions.completeObjective(objectiveIndex)
end

function briefingRoom.mission.objectivesTriggersCommon.registerEscortNearTrigger(objectiveIndex)
  local unit = dcsExtensions.getUnitOrStatic(briefingRoom.mission.objectives[objectiveIndex].unitNames[1])
  if unit ~= nil and not unit:isActive() then
    table.insert(briefingRoom.mission.objectives[objectiveIndex].f10Commands, {text = "$LANG_ESCORTMENU$", func = briefingRoom.mission.objectivesTriggersCommon.escortNearTriggerlaunchMission, args =  {objectiveIndex}})
  end
end