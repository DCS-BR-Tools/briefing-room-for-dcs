-- Extracts groups out of mission lua files. Its hacked together but generally create "mission.lua" where you will run the script from. Paste mission file contence in the "mission.lua" file. Run script
-- Currently this extracts all RED static objects as a single group and all Red vehicle groups
-- It dumps a bunch of ini files based off group names this should be a good starting point for implementing groups of units.

require "mission" -- Mission lua file

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

-- Static Units as single group
if mission.coalition.red.country[1].static ~= nil then
    local ids = "DCSID="
    local coordinates = "Offset.Coordinates="
    local headings = "Offset.Heading="
    local shape = "Shape="
    local originX = mission.coalition.red.country[1].static.group[1].x
    local originY = mission.coalition.red.country[1].static.group[1].y
    for _, value in orderedPairs(mission.coalition.red.country[1].static.group) do --actualcode
        ids = ids .. value.units[1].type .. ","
        coordinates = coordinates .. (value.units[1].x - originX) .. "," .. (value.units[1].y - originY) .. ";"
        headings = headings .. value.units[1].heading .. ","
        if value.units[1].shape_name == nil then
            shape = shape .. ","
        else
            shape = shape .. value.units[1].shape_name .. ","
        end
    end

    local file = io.open(mission.coalition.red.country[1].static.group.name .. ".ini", "w")
    file:write(ids .. "\n")
    file:write(coordinates .. "\n")
    file:write(headings .. "\n")
    file:write(shape .. "\n")
    file:close()
end


-- Red Ground Groups
for _, groupValue in orderedPairs(mission.coalition.red.country[1].vehicle.group) do --actualcode
    local ids = "DCSID="
    local coordinates = "Offset.Coordinates="
    local headings = "Offset.Heading="
    local shape = "Shape="
    local originX = groupValue.x
    local originY = groupValue.y
    for _, value in orderedPairs(groupValue.units) do --actualcode
        ids = ids .. value.type .. ","
        coordinates = coordinates .. (value.x - originX) .. "," .. (value.y - originY) .. ";"
        headings = headings .. value.heading .. ","
    end
    local file = io.open(groupValue.name .. ".ini", "w")
    file:write(ids .. "\n")
    file:write(coordinates .. "\n")
    file:write(headings .. "\n")
    file:close()
end
