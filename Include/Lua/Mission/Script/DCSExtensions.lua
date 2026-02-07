
-- ===================================================================================
-- 1.3 - DCS WORLD EXTENSIONS: Provides additional functions to DCS World scripting
-- ===================================================================================

dcsExtensions = { } -- main dcsExtensions table

-- Returns an angle in degrees to the nearest cardinal direction, as a string
function dcsExtensions.degreesToCardinalDirection(angle)
  angle = math.clamp(angle % 360, 0, 359)
  local val = math.floor((angle / 22.5) + 0.5)
  local directions = { "north", "north-north-east", "north-east", "east-north-east", "east", "east-south-east", "south-east", "south-south-east", "south", "south-south-west", "south-west", "west-south-west", "west", "west-north-west", "north-west", "north-north-west" }
  return directions[(val % 16) + 1]
end

-- Returns a table with all units controlled by a player
function dcsExtensions.getAllPlayers()
  local players = { }
  
  for coaName, i in pairs(coalition.side) do
    for _,g in pairs(coalition.getGroups(i)) do
      for __,u in pairs(g:getUnits()) do
        if u:getPlayerName() ~= nil then
          table.insert(players, u)
        end
      end
    end
  end

  return players
end

-- Returns the distance between two vec2s
function dcsExtensions.getDistance(vec2a, vec2b)
  return math.sqrt(math.pow(vec2a.x - vec2b.x, 2) + math.pow(vec2a.y - vec2b.y, 2))
end

-- Is an unit alive?
function dcsExtensions.isUnitAlive(name)
  if name == nil then return false end
  local unit = Unit.getByName(name)
  if unit == nil then return false end
  if unit:isActive() == false then return false end
  if unit:getLife() < 1 then return false end

  return true
end

function dcsExtensions.getUnitOrStatic(name)
  local unit = Unit.getByName(name)
  if unit == nil then -- no unit found with the ID, try searching for a static
    unit = StaticObject.getByName(name)
  end
  return unit
end


-- Returns the first unit alive in group with ID groupID, or nil if group doesn't exist or is completely destroyed
function dcsExtensions.getAliveUnitInGroup(groupName)
  local g = Group.getByName(groupName)
  if g == nil then return nil end

  for __,u in ipairs(g:getUnits()) do
    if u:getLife() >= 1 and u:isActive() then
      return u
    end
  end

  return nil
end

-- Returns all units belonging to the given coalition
function dcsExtensions.getCoalitionUnits(coalID)
  local units = { }
  for _,g in pairs(coalition.getGroups(coalID)) do
    for __,u in pairs(g:getUnits()) do
      if u:isActive() then
        if u:getLife() >= 1 then
          table.insert(units, u)
        end
      end
    end
  end

  return units
end


function dcsExtensions.getCoalitionStaticObjects(coalID)
  local units = { }
  for _,u in pairs(coalition.getStaticObjects(coalID)) do
    if u:getLife() >= 1 then
      table.insert(units, u)
    end
  end

  return units
end


function dcsExtensions.getGroupNamesContaining(search)
  local groups = { }
  for coaName, i in pairs(coalition.side) do
    for _,g in pairs(coalition.getGroups(i)) do
        if string.match(g:getName(), search) then
          table.insert(groups, g:getName())
      end
    end
  end

  return groups
end

function dcsExtensions.getUnitNamesByGroupNameSuffix(suffix)
  local unitNames = {}
  for coaName, i in pairs(coalition.side) do
    for _,g in pairs(coalition.getGroups(i)) do
        if string.endsWith(g:getName(), suffix) then
          for _,u in pairs(g:getUnits()) do
            table.insert(unitNames, u:getName())
          end
      end
    end
    for _,u in pairs(coalition.getStaticObjects(i)) do
      if string.endsWith(u:getName(), suffix) then
          table.insert(unitNames, u:getName())
      end
    end
        for _,u in pairs(coalition.getAirbases(i)) do
      if string.endsWith(u:getName(), suffix) then
          table.insert(unitNames, u:getName())
      end
    end
  end
  return unitNames
end

function dcsExtensions.getUnitNamesByGroupNameSuffixExcludeScenery(suffix)
  local unitNames = dcsExtensions.getUnitNamesByGroupNameSuffix(suffix)
  local filteredUnitNames = {}
  for _, unitName in pairs(unitNames) do
    if not string.startsWith(unitName, "SCENERY-") then
      table.insert(filteredUnitNames, unitName)
    end
  end
  return filteredUnitNames
end

-- Converts a timecode (in seconds since midnight) in a hh:mm:ss string
function dcsExtensions.timeToHMS(timecode)
  local h = math.floor(timecode / 3600)
  timecode = timecode - h * 3600
  local m = math.floor(timecode / 60)
  timecode = timecode - m * 60
  local s = timecode

  return string.format("%.2i:%.2i:%.2i", h, m, s)
end

-- Converts a pair of x, y coordinates or a vec3 to a vec2
function dcsExtensions.toVec2(xOrVector, y)
  if y == nil then
    if xOrVector.z then return { ["x"] = xOrVector.x, ["y"] = xOrVector.z } end
    return { ["x"] = xOrVector.x, ["y"] = xOrVector.y } -- return xOrVector if it was already a vec2
  else
    return { ["x"] = xOrVector, ["y"] = y }
  end
end

-- Converts a triplet of x, y, z coordinates or a vec2 to a vec3
function dcsExtensions.toVec3(xOrVector, y, z)
  if y == nil or z == nil then
    if xOrVector.z then return { ["x"] = xOrVector.x, ["y"] = xOrVector.y, ["z"] = xOrVector.z } end  -- return xOrVector if it was already a vec3
    return { ["x"] = xOrVector.x, ["y"] = 0, ["z"] = xOrVector.y }
  else
    return { ["x"] = xOrVector, ["y"] = y, ["z"] = z }
  end
end

-- Converts a vec2 or ver3 into a human-readable string
function dcsExtensions.vectorToString(vec)
  if vec.z == nil then -- no Z coordinate, vec is a Vec2
    return tostring(vec.x)..","..tostring(vec.y)
  else
    return tostring(vec.x)..","..tostring(vec.y)..","..tostring(vec.z)
  end
end

-- Turns a vec2 to a string with LL/MGRS coordinates
-- Based on code by Bushmanni - https://forums.eagle.ru/showthread.php?t=99480
function dcsExtensions.vec2ToStringCoordinates(vec2)
  local pos = { x = vec2.x, y = land.getHeight({x = vec2.x, y = vec2.y}), z = vec2.y }
  local cooString = ""

  local LLposN, LLposE, alt = coord.LOtoLL(pos)
  local LLposfixN, LLposdegN = math.modf(LLposN)
  LLposdegN = LLposdegN * 60
  local LLposdegN2, LLposdegN3 = math.modf(LLposdegN)
  local LLposdegN3Decimal = LLposdegN3 * 1000
  LLposdegN3 = LLposdegN3 * 60

  local LLposfixE, LLposdegE = math.modf(LLposE)
  LLposdegE = LLposdegE * 60
  local LLposdegE2, LLposdegE3 = math.modf(LLposdegE)
  local LLposdegE3Decimal = LLposdegE3 * 1000
  LLposdegE3 = LLposdegE3 * 60

  local LLns = "N"
  if LLposfixN < 0 then LLns = "S" end
  local LLew = "E"
  if LLposfixE < 0 then LLew = "W" end

  local LLposNstring = LLns.." "..string.format("%.2i°%.2i'%.2i''", LLposfixN, LLposdegN2, LLposdegN3)
  local LLposEstring = LLew.." "..string.format("%.3i°%.2i'%.2i''", LLposfixE, LLposdegE2, LLposdegE3)
  cooString = "L/L: "..LLposNstring.." "..LLposEstring

  local LLposNstring = LLns.." "..string.format("%.2i°%.2i.%.3i", LLposfixN, LLposdegN2, LLposdegN3Decimal)
  local LLposEstring = LLew.." "..string.format("%.3i°%.2i.%.3i", LLposfixE, LLposdegE2, LLposdegE3Decimal)
  cooString = cooString.."\nL/L: "..LLposNstring.." "..LLposEstring

  local mgrs = coord.LLtoMGRS(LLposN, LLposE)
  local mgrsString = mgrs.MGRSDigraph.." "..mgrs.UTMZone.." "..tostring(mgrs.Easting).." "..tostring(mgrs.Northing)
  cooString = cooString.."\nMGRS: "..mgrsString
  cooString = cooString.."\n$LANG_ALTITUDE$: "..math.floor(alt * 3.281).."ft"

  return cooString
end

function dcsExtensions.lerp(value1, value2, linearInterpolation)
  return value1 * (1 - linearInterpolation) + value2 * linearInterpolation;
end