-- Extracts groups out of mission lua files. Its hacked together but generally create "mission.lua" where you will run the script from. Paste mission file contence in the "mission.lua" file. Run script
-- Currently this extracts all RED static objects as a single group and all Red vehicle groups
-- It dumps a bunch of ini files based off group names this should be a good starting point for implementing groups of units.

require "mission" -- Mission lua file
json = require "json"

function __genOrderedIndex(t)
    local orderedIndex = {}
    for key in pairs(t) do
        table.insert(orderedIndex, key)
    end
    table.sort(orderedIndex)
    return orderedIndex
end

function orderedNext(t, state)
    -- Equivalent of the next function, but returns the keys in the alphabetic
    -- order. We use a temporary ordered key table that is stored in the
    -- table being iterated.

    local key = nil
    --print("orderedNext: state = "..tostring(state) )
    if state == nil then
        -- the first time, generate the index
        t.__orderedIndex = __genOrderedIndex(t)
        key = t.__orderedIndex[1]
    else
        -- fetch the next value
        for i = 1, table.getn(t.__orderedIndex) do
            if t.__orderedIndex[i] == state then
                key = t.__orderedIndex[i + 1]
            end
        end
    end

    if key then
        return key, t[key]
    end

    -- no more value to return, cleanup
    t.__orderedIndex = nil
    return
end

function orderedPairs(t)
    -- Equivalent of the pairs() function on tables. Allows to iterate
    -- in order
    return orderedNext, t, nil
end

function mysplit(inputstr, sep)
   -- if sep is null, set it as space
   if sep == nil then
      sep = '%s'
   end
   -- define an array
   local t={}
   -- split string based on sep   
   for str in string.gmatch(inputstr, '([^'..sep..']+)') 
   do
      -- insert the substring in table
      table.insert(t, str)
   end
   -- return the array
   return t
end

file = io.open("LocationTemplates.json", "w")
io.output(file)

local unitTypes = {
    ["KAMAZ Truck"] = {"VehicleSupply"},
    ["GAZ-66"] = {"VehicleSupply"},
    ["Tor 9A331"] = {"VehicleSAMShort", "VehicleSAMShortIR", "VehicleAAA", "VehicleAAAStatic", "InfantryMANPADS"},
    ["S-300PS 5P85D ln"] = {"VehicleSAMLauncher"},
    ["S-300PS 5P85C ln"] = {"VehicleSAMLauncher"},
    ["S-300PS 40B6MD sr"] = {"VehicleSAMsr"},
    ["S-300PS 40B6M tr"] = {"VehicleSAMtr"},
    ["S-300PS 64H6E sr"] = {"VehicleSAMsr", "VehicleEWR", "VehicleSAMtr"},
    ["S-300PS 54K6 cp"] = {"VehicleSAMCmd"},
    ["1L13 EWR"] = {"VehicleEWR"},
    ["Infantry AK"] = {"Infantry"},
    ["BTR-80"] = {"VehicleAPC"},
    ["T-55"] = {"VehicleMBT"},
    ["Scud_B"] = {"VehicleMissile"},
    ["L118_Unit"] = {"VehicleArtillery"},
    ["MTLB"] = {"VehicleTransport"},
    default = {"UNKNOWN"}
}

local function switch(x, cases)
    return cases[x] or cases.default
  end

-- Red Ground Groups
local output = {}
local index = 1
for _, groupValue in orderedPairs(mission.coalition.red.country[1].vehicle.group) do --actualcode
    local originX = groupValue.x
    local originY = groupValue.y
    local locations = {}
    local locIndex = 1
    for _, value in orderedPairs(groupValue.units) do --actualcode
        locations[locIndex] = {
            coords = { originX - value.x, originY - value.y },
            heading = value.heading,
            originalType = value.type,
            unitTypes = switch(value.type, unitTypes)
        }
        locIndex = locIndex + 1
    end
    output[index] = { coords = { originX, originY }, locationType = mysplit(groupValue.name, "-")[1], locations = locations }
    index = index + 1
end

io.write(json.encode(output))

io.close(file)
