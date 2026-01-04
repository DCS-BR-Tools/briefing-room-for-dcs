-- ===================================================================================
-- 2.3 - EVENT HANDLER: common event handler used during the mission
-- ===================================================================================
briefingRoom.eventHandler = {}
briefingRoom.eventHandler.BDASetting = "$BDASETTING$"

function briefingRoom.handleGeneralPlayerKill(event)
  local playerName = event.initiator:getPlayerName()
  if playerName == nil or event.target.getCoalition == nil then return end
  if event.target:getCoalition() ~= briefingRoom.playerCoalition then -- unit is an enemy, radio some variation of a "enemy destroyed" message
    local soundName = "UnitDestroyed"
    local messages = { "$LANG_DESTROY1$", "$LANG_DESTROY2$", "$LANG_SHOOTDOWN1$", "$LANG_SHOOTDOWN2$" }
    local messageIndex = math.random(1, 2)
    local messageIndexOffset = 0



    local targetType = "Ground"
    if event.id == world.event.S_EVENT_CRASH then
      messageIndexOffset = 2
      if event.target:inAir() then
        targetType = "Air"
        messageIndexOffset = 2
      elseif unitWasAMissionTarget then
        return -- No "target splashed" message when destroying a target aircraft on the ground (mostly for OCA missions)
      end
    end
    if briefingRoom.eventHandler.BDASetting == "ALL" then
      briefingRoom.radioManager.play("$LANG_COMMAND$: "..playerName.." "..messages[messageIndex + messageIndexOffset], "RadioHQ"..soundName..targetType..tostring(messageIndex), math.random(1, 3))
    end
    briefingRoom.aircraftActivator.possibleResponsiveSpawn()
  else
    briefingRoom.radioManager.play("$LANG_COMMAND$: "..playerName.." $LANG_TEAMKILL$", "RadioHQTeamkill", math.random(1, 3))
  end 
end

function briefingRoom.handleGeneralPlayerKilled(event)
  local playerName = event.target:getPlayerName()
  if playerName == nil then return end
    briefingRoom.handleTroopsInAircraft(event)
end

function briefingRoom.handleTroopsInAircraft(event)
  local unitName = event.initiator:getName()
  -- TODO see if we can detect units in aircraft that embarked using DCS radio
  if table.containsKey(briefingRoom.transportManager.transportRoster, unitName) then
    local troopNames={}
    local n=0
    for k,v in pairs(briefingRoom.transportManager.transportRoster[unitName].troops) do
      n=n+1
      troopNames[n]=k
    end
    briefingRoom.debugPrint("unpacking troops "..table.count(troopNames))
    -- Assume all troops are main characters and survive the crash no issues
    briefingRoom.transportManager.removeTroopCargo(unitName, troopNames, event.initiator:getPoint())
  end
end

function briefingRoom.handleGeneralKill(event)
  if event.id == world.event.S_EVENT_KILL then 
    if event.initiator == nil or event.target == nil or (event.initiator.getPlayerName == nil and event.target.getPlayerName == nil) then return end -- Incomplete event or player not involved
    if event.initiator.getPlayerName ~= nil then
      briefingRoom.handleGeneralPlayerKill(event)
    end
    if event.target.getPlayerName ~= nil then
      briefingRoom.handleGeneralPlayerKilled(event)
    end
  end

  if event.id == world.event.S_EVENT_DEAD or event.id == world.event.S_EVENT_CRASH or event.id == world.event.S_EVENT_PLAYER_LEAVE_UNIT then
    if event.initiator == nil or event.initiator.getCategory == nil then  return end -- no initiator
    if event.initiator:getCategory() ~= Object.Category.UNIT and event.initiator:getCategory() ~= Object.Category.STATIC then return end -- initiator was not an unit or static
    if event.initiator:getCoalition() == briefingRoom.playerCoalition then
      local unitName = event.initiator:getName()
      briefingRoom.debugPrint("Friendly Crash "..unitName)
      briefingRoom.handleTroopsInAircraft(event)
    end
  end
end

function briefingRoom.eventHandler:onEvent(event)
  if event.id == world.event.S_EVENT_TAKEOFF and -- unit took off
    event.initiator:getPlayerName() ~= nil then -- unit is a pleyr
      briefingRoom.mission.coreFunctions.beginMission() -- first player to take off triggers the mission start
  end

  local eventHandled = false
  -- Pass the event to the completion trigger of all objectives that have one
  for k, func in pairs(briefingRoom.mission.objectiveTriggers) do
    if func ~= nil then
      local didHandle = func(event)
      if didHandle then
        eventHandled = true
      end
    end
  end 
  if eventHandled == false then
    briefingRoom.handleGeneralKill(event)
  end
end