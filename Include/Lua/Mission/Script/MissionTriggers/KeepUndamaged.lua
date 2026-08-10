function briefingRoom.mission.objectivesTriggersCommon.registerKeepUndamagedTrigger(objectiveIndex)
  local handler = function(event)
    -- Mission complete, nothing to do
    if briefingRoom.mission.objectivesTriggersCommon.isMissionOrObjectiveComplete(objectiveIndex) then return false end

    local hitOrDestroyedUnit = nil
    
    if event.id == world.event.S_EVENT_DEAD or event.id == world.event.S_EVENT_CRASH then
        hitOrDestroyedUnit = event.initiator
    elseif event.id == world.event.S_EVENT_HIT or event.id == world.event.S_EVENT_KILL then
        hitOrDestroyedUnit = event.target
    elseif briefingRoom.mission.objectives[objectiveIndex].targetCategory == Unit.Category.HELICOPTER and event.id == world.event.S_EVENT_LAND then
        hitOrDestroyedUnit = event.initiator
    end
    
    if hitOrDestroyedUnit == nil then return false end
    
    if Object.getCategory(hitOrDestroyedUnit) ~= Object.Category.UNIT and Object.getCategory(hitOrDestroyedUnit) ~= Object.Category.STATIC then return false end
    if hitOrDestroyedUnit.getName == nil then return false end
  
    local unitName = hitOrDestroyedUnit:getName()
    -- Destroyed/Hit unit wasn't a target
    if not briefingRoom.mission.objectivesTriggersCommon.objectiveHasUnitName(objectiveIndex, unitName) then return false end
  
    -- Remove the unit from the list of targets
    briefingRoom.mission.objectivesTriggersCommon.removeObjectiveUnitName(objectiveIndex, unitName)
  
    -- Play "target destroyed" radio message
    local messages = { "$LANG_COMMAND$: $LANG_TARGETLOST1$", "$LANG_COMMAND$: $LANG_TARGETLOST2$" }
    local messageIndex = math.random(1, 2)
  
    if briefingRoom.eventHandler.BDASetting == "ALL" or briefingRoom.eventHandler.BDASetting == "TARGETONLY" then
      briefingRoom.radioManager.play(messages[messageIndex], "RadioHQTargetLost", math.random(1, 3))
    end
  
    -- Mark the objective as failed (or complete in this context means failed since it's an escort?)
    -- In KeepAlive, removing all target units completes the objective but marks it failed?
    -- Wait, if any unit takes damage, we fail the entire objective.
    briefingRoom.mission.coreFunctions.completeObjective(objectiveIndex, true)
  
    return true
  end

  briefingRoom.mission.registerObjectiveEventTrigger(world.event.S_EVENT_HIT, handler)
  briefingRoom.mission.registerObjectiveEventTrigger(world.event.S_EVENT_KILL, handler)
  briefingRoom.mission.registerObjectiveEventTrigger(world.event.S_EVENT_DEAD, handler)
  briefingRoom.mission.registerObjectiveEventTrigger(world.event.S_EVENT_CRASH, handler)
  briefingRoom.mission.registerObjectiveEventTrigger(world.event.S_EVENT_LAND, handler)
end
