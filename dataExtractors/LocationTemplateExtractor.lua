-- Extracts groups out of mission lua files. Its hacked together but generally create "mission.lua" where you will run the script from. Paste mission file contence in the "mission.lua" file. Run script
-- Currently this extracts all RED static objects as a single group and all Red vehicle groups
-- It dumps a bunch of ini files based off group names this should be a good starting point for implementing groups of units.

require "mission" -- Mission lua file
json = require "json"

-- Returns true if string str ends with needle
function string.endsWith(str, needle)
  return needle == "" or str:sub(-#needle) == needle
end

function string.startsWith(str, needle)
  return str:sub(1, #needle) == needle
end

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
    local t = {}
    -- split string based on sep
    for str in string.gmatch(inputstr, '([^' .. sep .. ']+)')
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
    ["KAMAZ Truck"] = { "VehicleSupply" },
    ["GAZ-66"] = { "VehicleSupply" },
    ["Tor 9A331"] = { "VehicleSAMShort", "VehicleSAMShortIR", "VehicleAAA", "VehicleAAAStatic", "InfantryMANPADS" },
    ["Roland ADS"] = { "VehicleSAMShort", "VehicleSAMShortIR", "VehicleAAA", "VehicleAAAStatic", "InfantryMANPADS" },
    ["2S6 Tunguska"] = { "VehicleSAMShort", "VehicleSAMShortIR", "VehicleAAA", "VehicleAAAStatic", "InfantryMANPADS" },
    ["Vulcan"] = { "VehicleSAMShort", "VehicleSAMShortIR", "VehicleAAA", "VehicleAAAStatic", "InfantryMANPADS" },
    ["M1097 Avenger"] = { "VehicleSAMShort", "VehicleSAMShortIR", "VehicleAAA", "VehicleAAAStatic", "InfantryMANPADS" },
    ["S-300PS 5P85D ln"] = { "VehicleSAMLauncher" },
    ["S-300PS 5P85C ln"] = { "VehicleSAMLauncher" },
    ["S-300PS 40B6MD sr"] = { "VehicleSAMsr" },
    ["S-300PS 40B6M tr"] = { "VehicleSAMtr" },
    ["S-300PS 64H6E sr"] = { "VehicleSAMsr", "VehicleEWR", "VehicleSAMtr" },
    ["Patriot str"] = { "VehicleSAMsr", "VehicleSAMtr" },
    ["Hawk pcp"] = { "VehicleSAMCmd" },
    ["Hawk cwar"] = {"VehicleSAMcmd"},
    ["S-300PS 54K6 cp"] = { "VehicleSAMCmd" },
    ["Patriot cp"] = { "VehicleSAMCmd" },
    ["Patriot AMG"] = { "VehicleSAMCmd" },
    ["Patriot ECS"] = { "VehicleSAMCmd" },
    ["Patriot EPP"] = { "VehicleSAMCmd" },
    
    ["Roland Radar"] = { "VehicleEWR" },
    ["1L13 EWR"] = { "VehicleEWR" },
    ["Infantry AK"] = { "Infantry" },
    ["BTR-80"] = { "VehicleAPC" },
    ["Leopard1A3"] = { "VehicleMBT" },
    ["T-55"] = { "VehicleMBT" },
    ["Scud_B"] = { "VehicleMissile" },
    ["L118_Unit"] = { "VehicleArtillery" },
    ["MTLB"] = { "VehicleTransport" },
    -- temp
    ["Hummer"] = { "VehicleTransport" },
    ["M978 HEMTT Tanker"] = { "VehicleTransport" },
    ["Ural-375 PBU"] = { "VehicleTransport" },
    ["ZiL-131 APA-80"] = { "VehicleTransport" },
    ["ATMZ-5"] = { "VehicleTransport" },
    ["ATZ-10"] = { "VehicleTransport" },
    ["Ural-4320-31"] = { "VehicleSupply" },
    ["ZIL-131 KUNG"] = { "VehicleSupply" },
    ["Ural-4320T"] = { "VehicleSupply" },
    ["M 818"] = { "VehicleSupply" },
    ["Ural-375"] = { "VehicleSupply" },
    ["Ural-4320 APA-5D"] = { "VehicleTransport" },
    ["Land_Rover_101_FC"] = { "VehicleTransport" },
    ["Land_Rover_109_S3"] = { "VehicleTransport" },
    ["snr s-125 tr"] = { "VehicleSAMtr" },
    ["SNR_75V"] = { "VehicleSAMtr" },
    ["rapier_fsa_optical_tracker_unit"] = { "VehicleSAMtr" },
    ["Hawk tr"] = { "VehicleSAMtr" },
    ["S_75M_Volhov"] = { "VehicleSAMLauncher" },
    ["5p73 s-125 ln"] = { "VehicleSAMLauncher" },
    ["S-200_Launcher"] = { "VehicleSAMLauncher" },
    ["Kub 2P25 ln"] = { "VehicleSAMLauncher" },
    ["rapier_fsa_launcher"] = { "VehicleSAMLauncher" },
    ["Hawk ln"] = { "VehicleSAMLauncher" },
    ["Patriot ln"] = { "VehicleSAMLauncher" },

    ["p-19 s-125 sr"] = { "VehicleSAMsr" },
    ["RPC_5N62V"] = { "VehicleSAMsr" },
    ["RLS_19J6"] = { "VehicleSAMsr" },
    ["Kub 1S91 str"] = { "VehicleSAMsr" },
    ["rapier_fsa_blindfire_radar"] = { "VehicleSAMsr" },
    ["Hawk sr"] = { "VehicleSAMsr" },


    default = { "UNKNOWN" }
}

local function switch(x, cases)
    return cases[x] or cases.default
end

-- Red Ground Groups
local output = {}
local index = 1
for _, country in orderedPairs(mission.coalition.red.country) do
    if (country.vehicle ~= nil) then
        for _, groupValue in orderedPairs(country.vehicle.group) do --actualcode
            local originX = groupValue.x
            local originY = groupValue.y
            local locations = {}
            local locIndex = 1
            for _, value in orderedPairs(groupValue.units) do --actualcode
                locations[locIndex] = {
                    coords = {  value.x - originX, value.y - originY },
                    heading = value.heading,
                    originalType = value.type,
                    unitTypes = switch(value.type, unitTypes),
                    specificType = (value.name ~= nil and string.endsWith(value.name, '-SPECIFIC') and value.type or nil),
                    isScenery =  (value.name ~= nil and string.startsWith(value.name, 'SCENERY-'))
                }
                locIndex = locIndex + 1
            end
            output[index] = { coords = { originX, originY }, locationType = mysplit(groupValue.name, "-")[1], locations =
            locations }
            index = index + 1
        end
    end --actualcode
end

for _, country in orderedPairs(mission.coalition.blue.country) do
    if (country.vehicle ~= nil) then
        for _, groupValue in orderedPairs(country.vehicle.group) do --actualcode
            local originX = groupValue.x
            local originY = groupValue.y
            local locations = {}
            local locIndex = 1
            for _, value in orderedPairs(groupValue.units) do --actualcode
                locations[locIndex] = {
                    coords = {  value.x - originX, value.y - originY },
                    heading = value.heading,
                    originalType = value.type,
                    unitTypes = switch(value.type, unitTypes),
                    specificType = (value.name ~= nil and string.endsWith(value.name, '-SPECIFIC') and value.type or nil),
                    isScenery =  (value.name ~= nil and string.startsWith(value.name, 'SCENERY-'))
                }
                locIndex = locIndex + 1
            end
            output[index] = { coords = { originX, originY }, locationType = mysplit(groupValue.name, "-")[1], locations =
            locations }
            index = index + 1
        end
    end --actualcode
end

io.write(json.encode(output))

io.close(file)
