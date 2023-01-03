-- Triggers the failure of objective $OBJECTIVEINDEX$ when all targets are destroyed
table.insert(briefingRoom.mission.objectiveTriggers, function(event)
  -- Mission complete, nothing to do
  if briefingRoom.mission.complete then return false end

  -- Check if event is a "destruction" event
  local destructionEvent = false
  if event.id == world.event.S_EVENT_DEAD or event.id == world.event.S_EVENT_CRASH then destructionEvent = true end
  -- "Landing" events are considered kills for helicopter targets
  if briefingRoom.mission.objectives[$OBJECTIVEINDEX$].targetCategory == Unit.Category.HELICOPTER and event.id == world.event.S_EVENT_LAND then destructionEvent = true end
  if not destructionEvent then return false end

  -- Initiator was nil
  if event.initiator == nil then return false end
  if Object.getCategory(event.initiator) ~= Object.Category.UNIT and Object.getCategory(event.initiator) ~= Object.Category.STATIC then return false end

  local unitName = event.initiator:getName()
  -- Destroyed unit wasn't a target
  if not table.contains(briefingRoom.mission.objectives[$OBJECTIVEINDEX$].unitNames, unitName) then return false end

  -- Remove the unit from the list of targets
  table.removeValue(briefingRoom.mission.objectives[$OBJECTIVEINDEX$].unitNames, unitName)

  -- Play "target destroyed" radio message
  local soundName = "RadioHQTargetLost"
  local messages = { "$LANG_COMMAND$: $LANG_TARGETLOST1$", "$LANG_COMMAND$: $LANG_TARGETLOST2$" }
  local targetType = "Ground"
  local messageIndex = math.random(1, 2)
  local messageIndexOffset = 0

  if briefingRoom.eventHandler.BDASetting == "ALL" or briefingRoom.eventHandler.BDASetting == "TARGETONLY" then
    briefingRoom.radioManager.play(messages[messageIndex + messageIndexOffset], "RadioHQTargetLost", math.random(1, 3))
  end

  -- Mark the objective as complete if all targets have been destroyed
  if #briefingRoom.mission.objectives[$OBJECTIVEINDEX$].unitNames < 1 then -- all target units destroyed, objective failed
    briefingRoom.mission.coreFunctions.completeObjective($OBJECTIVEINDEX$, true)
  end

  return true
end)
