-- ===================================================================================
-- 2.2 - AIRCRAFT ACTIVATOR: activates aircraft flight groups gradually during the mission
-- ===================================================================================
briefingRoom.aircraftActivator = { }
briefingRoom.aircraftActivator.INTERVAL = { 10, 20 } -- min/max interval (in seconds) between two updates
briefingRoom.aircraftActivator.currentQueue = dcsExtensions.getGroupNamesContaining("%-IQ%-") -- current queue of aircraft group IDs to spawn every INTERVAL seconds
briefingRoom.aircraftActivator.reserveQueue = dcsExtensions.getGroupNamesContaining("%-RQ%-")
briefingRoom.aircraftActivator.timeQueue = dcsExtensions.getGroupNamesContaining("%-TQ%-") -- additional aircraft group IDs to be added to the queue later
briefingRoom.aircraftActivator.responsiveMode = false


function briefingRoom.aircraftActivator.getAircraftTime(str)
  local tempa, tempb = string.find(str, "%-TQ%-%d+")
  local mid = string.sub(str, tempa,tempb)
  local mida, midb = string.find(mid, "%d+")
  return tonumber(string.sub(mid, mida,midb))
end

function briefingRoom.aircraftActivator.getRandomInterval()
  return math.random(briefingRoom.aircraftActivator.INTERVAL[1], briefingRoom.aircraftActivator.INTERVAL[2])
end

function briefingRoom.aircraftActivator.pushFromReserveQueue()
  if table.count(briefingRoom.aircraftActivator.reserveQueue) == 0 then -- no extra queues available
    briefingRoom.debugPrint("Tried to push extra aircraft to the activation queue, but found none")
    return
  end

  -- add aircraft groups from the reserve queue to the current queue
  local numberOfGroupsToAdd = math.max(1, math.min(briefingRoom.aircraftActivator.reserveQueueInitialCount / (table.count(briefingRoom.mission.objectives) + 1), table.count(briefingRoom.aircraftActivator.reserveQueue)))

  for i=0,numberOfGroupsToAdd do
    briefingRoom.debugPrint("Pushed aircraft group #"..tostring(briefingRoom.aircraftActivator.reserveQueue[1]).." into the activation queue")
    table.insert(briefingRoom.aircraftActivator.currentQueue, briefingRoom.aircraftActivator.reserveQueue[1])
    table.remove(briefingRoom.aircraftActivator.reserveQueue, 1)
  end
end

-- Every INTERVAL seconds, check for aircraft groups to activate in the queue
function briefingRoom.aircraftActivator.update(args, time)
  local minsPassed = math.floor((timer.getAbsTime() - timer.getTime0())/60)
  for k, name in pairs(briefingRoom.aircraftActivator.timeQueue) do
    local actTime = briefingRoom.aircraftActivator.getAircraftTime(name)
    briefingRoom.debugPrint("Looking for aircraft groups to activate, "..name.." ActTime:"..tostring(actTime).." Time:"..tostring(minsPassed), 1)
    for k,objective in pairs(briefingRoom.mission.objectives) do
      if string.match(name, objective.name) then 
        if objective.startMinutes > -1 then
          actTime = actTime + objective.startMinutes
          briefingRoom.debugPrint("Adjusted ActTime for objective "..objective.name.." to "..tostring(actTime), 1)
        else 
          actTime = 999999999999999999999 -- objective is not active, do not spawn aircraft
          briefingRoom.debugPrint("Objective "..objective.name.." not active ignoring "..name, 1)
        end
      end
    end
    if actTime <= minsPassed then
      table.insert(briefingRoom.aircraftActivator.currentQueue, name)
      table.removeValue(briefingRoom.aircraftActivator.timeQueue, name)
      briefingRoom.debugPrint(name.." Pushed to current queue", 1)
    end
  end
  briefingRoom.debugPrint("Looking for aircraft groups to activate, found "..tostring(table.count(briefingRoom.aircraftActivator.currentQueue)).." Time:"..tostring(minsPassed), 1)
  if table.count(briefingRoom.aircraftActivator.currentQueue) == 0 then -- no aircraft in the queue at the moment
    return time + briefingRoom.aircraftActivator.getRandomInterval() -- schedule next update and return
  end
  
  local acGroup = Group.getByName(briefingRoom.aircraftActivator.currentQueue[1]) -- get the group
  if acGroup ~= nil then -- activate the group, if it exists
    acGroup:activate()
    local Start = {
      id = 'Start',
      params = {
      }
    }
    acGroup:getController():setCommand(Start)
    briefingRoom.debugPrint("Activating aircraft group "..acGroup:getName())
  else
    briefingRoom.debugPrint("Failed to activate aircraft group "..tostring(briefingRoom.aircraftActivator.currentQueue[1]))
  end
  table.remove(briefingRoom.aircraftActivator.currentQueue, 1) -- remove the ID from the queue
  if acGroup == nil or string.match(acGroup:getName(), "%-IQ%-") then
    return time + 1
  end
  return time + briefingRoom.aircraftActivator.getRandomInterval() -- schedule next update
end

function briefingRoom.aircraftActivator.possibleResponsiveSpawn()
  if briefingRoom.aircraftActivator.responsiveMode and briefingRoom.mission.hasStarted then
    local roll = math.random(1, 100)
    briefingRoom.debugPrint("Possible Responsive Spawn rolled: "..tostring(roll))
    if roll < 25 then -- aprox 25% chance of spawn
      briefingRoom.aircraftActivator.pushFromReserveQueue()
    end
  end
end

function briefingRoom.aircraftActivator.convertToStatic()
  local queue = dcsExtensions.getGroupNamesContaining("%-STATIC%-") -- aircraft to be converted to static version on mission load for performance
  for k, name in pairs(queue) do
    local acGroup = Group.getByName(name) -- get the group
    local mistGroupData = mist.getGroupData(name)
    for l, unit in pairs(acGroup:getUnits()) do
      local unitpos = unit:getPosition()
      local unitPoint = dcsExtensions.toVec2(unit:getPoint())
      local vars = 
      {
        type = unit:getTypeName(),
        country = unit:getCountry(),
        category = unit:getCategory(),
        x = unitPoint['x'],
        y = unitPoint['y'],
        heading =  mist.getHeading(unit),
        livery_id = mistGroupData.units[l].livery_id
      }
      unit:destroy()
      mist.dynAddStatic(vars)
    end
    acGroup:destroy()
  end
end

briefingRoom.aircraftActivator.convertToStatic()

briefingRoom.aircraftActivator.reserveQueueInitialCount = table.count(briefingRoom.aircraftActivator.reserveQueue)
