-- Extracts groups out of mission lua files. Its hacked together but generally create "mission.lua" where you will run the script from. Paste mission file contence in the "mission.lua" file. Run script
-- Currently this extracts all RED static objects as a single group and all Red vehicle groups
-- It dumps a bunch of ini files based off group names this should be a good starting point for implementing groups of units.

require "mission" -- Mission lua file
require "dataExtractors/TemplateCommon"

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
        coordinates = coordinates .. (originX - value.units[1].x) .. "," .. (originY - value.units[1].y) .. ";"
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
        coordinates = coordinates .. (originX - value.x) .. "," .. (originY - value.y) .. ";"
        headings = headings .. value.heading .. ","
    end
    local file = io.open(groupValue.name .. ".ini", "w")
    file:write(ids .. "\n")
    file:write(coordinates .. "\n")
    file:write(headings .. "\n")
    file:close()
end
