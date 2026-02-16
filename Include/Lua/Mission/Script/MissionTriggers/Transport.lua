function briefingRoom.mission.objectivesTriggersCommon.transportTroopsForcePickup(args)
  local objectiveIndex = args[1]
  if briefingRoom.mission.objectives[objectiveIndex].complete then return end
  local players = dcsExtensions.getAllPlayers()
    for _,p in ipairs(players) do
      if not p:inAir() then
      briefingRoom.debugPrint("Player on ground")
      local position = dcsExtensions.toVec2(p:getPoint())
      local collect = {}
      -- Pickup
      for _,id in ipairs(briefingRoom.mission.objectives[objectiveIndex].unitNames) do
        local targetUnit = Unit.getByName(id)
        if targetUnit ~= nil then
          local targetPosition = dcsExtensions.toVec2(targetUnit:getPoint())
          briefingRoom.debugPrint("Player distance"..dcsExtensions.getDistance(position, targetPosition))
          if dcsExtensions.getDistance(position, targetPosition) < briefingRoom.mission.objectiveDropDistanceMeters then
            table.insert(collect, id)
          end
        end
      end


      if table.count(collect) > 0 then
        briefingRoom.debugPrint("Loading "..table.count(collect).." troops")
        briefingRoom.transportManager.troopsMoveToGetIn(p:getName(), collect)
      end
    end
  end
end

function briefingRoom.mission.objectivesTriggersCommon.transportTroopsForceDrop(args)
  local objectiveIndex = args[1]
  local objectiveIndex = objectiveIndex
  if briefingRoom.mission.objectives[objectiveIndex].complete then return end
  local players = dcsExtensions.getAllPlayers()
    for _,p in ipairs(players) do
      if not p:inAir() then
        briefingRoom.debugPrint("Player on ground")
        briefingRoom.transportManager.removeTroopCargo(p:getName(), briefingRoom.mission.objectives[objectiveIndex].unitNames)
    end
  end
end

function briefingRoom.mission.objectivesTriggersCommon.fireTroopsNearTrigger(objectiveIndex, unitName)
   if briefingRoom.mission.objectivesTriggersCommon.isMissionOrObjectiveComplete(objectiveIndex) then return false end
   table.removeValue(briefingRoom.mission.objectives[objectiveIndex].unitNames, unitName)
   briefingRoom.radioManager.play("$LANG_PILOT$: $LANG_TROOPSDELIVERED$", "RadioPilotTroopsDelivered")
    if table.count(briefingRoom.mission.objectives[objectiveIndex].unitNames) < 1 then
      briefingRoom.mission.coreFunctions.completeObjective(objectiveIndex)
    end
end

function briefingRoom.mission.objectivesTriggersCommon.registerTransportTroopsTrigger(objectiveIndex)
  table.insert(briefingRoom.mission.objectives[objectiveIndex].f10Commands, {text = "$LANG_FORCEPICKUP$", func = briefingRoom.mission.objectivesTriggersCommon.transportTroopsForcePickup, args = {objectiveIndex}})
  table.insert(briefingRoom.mission.objectives[objectiveIndex].f10Commands, {text = "$LANG_FORCEDROP$", func =  briefingRoom.mission.objectivesTriggersCommon.transportTroopsForceDrop, args =  {objectiveIndex}})

  table.insert(briefingRoom.mission.objectiveTriggers, function(event)
    if briefingRoom.mission.objectivesTriggersCommon.isMissionOrObjectiveComplete(objectiveIndex) then return false end

    if event.id ~= world.event.S_EVENT_LAND then return false end -- Not a "land" event, nothing to do
  
    if event.initiator == nil then return false end -- Initiator was nil
    if Object.getCategory(event.initiator) ~= Object.Category.UNIT then return false end -- Initiator was not an unit
    if event.initiator:getCoalition() ~= briefingRoom.playerCoalition then return false end -- Initiator was not a friendy unit
  
    local position = dcsExtensions.toVec2(event.initiator:getPoint()) -- get the landing unit position
   
    -- Drop off
    local distanceToObjective = dcsExtensions.getDistance(briefingRoom.mission.objectives[objectiveIndex].waypoint, position);  -- Zone Pos?
    if distanceToObjective < briefingRoom.mission.objectiveDropDistanceMeters then
      local removed = briefingRoom.transportManager.removeTroopCargo(event.initiator:getName(), briefingRoom.mission.objectives[objectiveIndex].unitNames)
      for index, value in ipairs(removed) do
        table.removeValue(briefingRoom.mission.objectives[objectiveIndex].unitNames, value)
      end
      if table.count(briefingRoom.mission.objectives[objectiveIndex].unitNames) < 1 then -- all target units moved or dead, objective complete
        local playername = event.initiator ~= nil  and event.initiator.getPlayerName and event.initiator:getPlayerName() or nil
        briefingRoom.radioManager.play((playername or"$LANG_PILOT$")..": $LANG_TROOPSDELIVERED$", "RadioPilotTroopsDelivered")
        briefingRoom.mission.coreFunctions.completeObjective(objectiveIndex)
      end
      return true
    end
  
    local collect = {}
    -- Pickup
    for _,id in ipairs(briefingRoom.mission.objectives[objectiveIndex].unitNames) do
      local targetUnit = Unit.getByName(id)
      if targetUnit ~= nil then
        local targetPosition = dcsExtensions.toVec2(targetUnit:getPoint())
        if dcsExtensions.getDistance(position, targetPosition) < briefingRoom.mission.objectiveDropDistanceMeters then
          table.insert(collect, id)
        end
      end
    end
  
    local nonNativeTransportingAircraft = { "UH-60L" }
    if table.count(collect) > 0 and table.contains(nonNativeTransportingAircraft, event.initiator:getTypeName()) then
      briefingRoom.transportManager.troopsMoveToGetIn(event.initiator:getName(), collect)
    end
  end)
end

function briefingRoom.mission.objectivesTriggersCommon.registerCaptureLocation(objectiveIndex, objectiveLocationId)
  table.insert(briefingRoom.mission.objectiveTriggers, function(event)

    if briefingRoom.mission.objectivesTriggersCommon.isMissionOrObjectiveComplete(objectiveIndex) then return false end
    if event.id == world.event.S_EVENT_BASE_CAPTURED then

      if event.place:getID() == objectiveLocationId and event.place:getCoalition() == briefingRoom.playerCoalition then
        briefingRoom.radioManager.play("$LANG_PILOT$: $LANG_PILOTREPORTCOMPLETE$", "RadioPilotReportComplete", math.random(1, 3))
        briefingRoom.mission.coreFunctions.completeObjective(objectiveIndex)
        return true
      end
    else return false end
  end)
end