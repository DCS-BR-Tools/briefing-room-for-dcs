-- ===================================================================================
-- 1.2 - LUA EXTENSIONS: Provides additional core functions to Lua
-- ===================================================================================
TWO_PI = math.pi * 2 -- two times Pi
-- ==================
-- 1.2.1 - CONVERTERS
-- ==================

-- Converts a value to a boolean
function toboolean(val)
  if val == nil or val == 0 or val == false then return false end
  if type(val) == "string" and string.lower(val) == "false" then return false end
  return true
end

-- Like the built-in "tonumber" functions, but returns 0 instead of nil in case of an error
function tonumber0(val)
  local numVal = tonumber(val)
  if numVal == nil then return 0 end
  return numVal
end

-- =======================
-- 1.2.2 - MATH EXTENSIONS
-- =======================

-- Makes sure the value is between min and max and returns the clamped value
function math.clamp(val, min, max)
  return math.min(math.max(val, min), max)
end

-- Returns a random floating-point number between min and max
function math.randomFloat(min, max)
  if min >= max then return a end
  return min + math.random() * (max - min)
end

-- Returns a random floating point number between t[1] and t[2]
function math.randomFloatTable(t)
  return math.randomFloat(t[1], t[2])
end

-- Returns a random value from numerically-indexed table t
function math.randomFromTable(t)
  return t[math.random(#t)]
end

-- Returns a random value from hash table t
function math.randomFromHashTable(t)
  local keyset = {}
  for k in pairs(t) do
      table.insert(keyset, k)
  end
  -- now you can reliably return a random key
  return t[keyset[math.random(#keyset)]]
end

-- Returns a random point in circle of center center and of radius radius
function math.randomPointInCircle(center, radius)
  local dist = math.random() * radius
  local angle = math.random() * TWO_PI

  local x = center.x + math.cos(angle) * dist
  local y = center.y + math.sin(angle) * dist

  return { ["x"] = x, ["y"] = y }
end

-- =========================
-- 1.2.3 - STRING EXTENSIONS
-- =========================

-- Returns true if string str ends with needle
function string.endsWith(str, needle)
  return needle == "" or str:sub(-#needle) == needle
end

-- Search a string for all keys in a table and replace them with the matching value
function string.replace(str, repTable)
  for k,v in pairs(repTable) do
    str = string.gsub(str, k, v)
  end
  return str
end

-- Split string str in an array of substring, using the provided separator
function string.split(str, separator)
  separator = separator or "%s"

  local t = { }
  for s in string.gmatch(str, "([^"..separator.."]+)") do
    table.insert(t, s)
  end

  return t
end

-- Returns true if string str starts with needle
function string.startsWith(str, needle)
  return str:sub(1, #needle) == needle
end

-- Returns the value matching the case-insensitive key in enumTable
function string.toEnum(str, enumTable, defaultVal)
  local cleanStr = string.trim(string.lower(str))

  for key,val in pairs(enumTable) do
    if key:lower() == cleanStr then return val end
  end

  return defaultVal
end

-- Returns string str withtout leading and closing spaces
function string.trim(str)
  return str:match "^%s*(.-)%s*$"
end

-- ========================
-- 1.2.4 - TABLE EXTENSIONS
-- ========================

-- Returns true if table t contains value val
function table.contains(t, val)
  for _,v in pairs(t) do
    if v == val then return true end
  end
  return false
end


-- Returns Key count
function table.count(t)
  local count = 0
  for _ in pairs(t) do count = count + 1 end
  return count
end

-- Returns true if table t contains key key
function table.containsKey(t, key)
  for k,_v in pairs(t) do
    if k == key then return true end
  end
  return false
end

function table.merge(t1,t2)
  for i=1,#t2 do
      t1[#t1+1] = t2[i]
  end
  return t1
end

-- Creates a new table which countains count elements from table valTable
function table.createFromRandomElements(valTable, count)
  local t = { }
  for i=1,count do table.insert(t, math.randomFromTable(valTable)) end
  return t
end

-- Creates a new table which countains count times the value val
function table.createFromSameElement(val, count)
  local t = { }
  for i=1,count do table.insert(t, val) end
  return t
end

-- Returns a deep copy of the table, doesn't work with recursive tables (code from http://lua-users.org/wiki/CopyTable)
function table.deepCopy(orig)
  if type(orig) ~= 'table' then return orig end

  local copy = {}
  for orig_key, orig_value in next, orig, nil do
    copy[table.deepCopy(orig_key)] = table.deepCopy(orig_value)
  end
  setmetatable(copy, table.deepCopy(getmetatable(orig)))

  return copy
end

-- Returns the key associated to a value in a table, or nil if not found
function table.getKeyFromValue(t, val)
  for k,v in pairs(t) do
    if v == val then return k end
  end
  return nil
end

-- Removes one instance of a value from a table
function table.removeValue(t, val)
  for k,v in pairs(t)do
    if v == val then
      table.remove(t, k)
      return
    end
  end
end

-- Shuffles a table
function table.shuffle(t)
  local len, random = #t, math.random
  for i = len, 2, -1 do
    local j = random( 1, i )
    t[i], t[j] = t[j], t[i]
  end
  return t
end

function table.removeNils(t)
  local ans = {}
  for _,v in pairs(t) do
    ans[ #ans+1 ] = v
  end
  return ans
end

-- THIS FUNCTION IS NOT SUITABLE for counting filtered output
function table.filter(t, filterIter)
  local out = {}

  for k, v in pairs(t) do
    if filterIter(v, k, t) then out[k] = v end
  end

  return out
end

function table.find(t, filterIter)
  for k, v in pairs(t) do
    if filterIter(v, k, t) then return v end
  end

  return nil
end