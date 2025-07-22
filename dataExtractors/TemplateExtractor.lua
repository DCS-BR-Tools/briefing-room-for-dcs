-- Extracts groups out of mission to create Templates that contain multiple groups joined together by names

require "mission" -- Mission lua file
require "dataExtractors/TemplateCommon"
json = require "json"



file = io.open("Templates.json", "w")
io.output(file)


local outputMap = {}
for _, marker in orderedPairs(mission.drawings.layers[5].objects) do
    outputMap[marker.name] = {
        name = marker.name,
        coords = { marker.mapX, marker.mapY },
        groups = {},
        groupsIndex = 1,
        immovable = false
    }
end
-- Group Locations


for _, country in orderedPairs(mission.coalition.red.country) do
    if (country.vehicle ~= nil) then
        for _, groupValue in orderedPairs(country.vehicle.group) do
            local groupName = groupValue.name
            local templateName = mysplit(groupName, "-")[1]
            local originX = groupValue.x
            local originY = groupValue.y
            local locations = {}
            local locIndex = 1
            for _, value in orderedPairs(groupValue.units) do
                local families = switchFamilies(value.type, unitTypeToFamilies)
                if families[1] == "UNKNOWN" then
                    families = nil
                end
                locations[locIndex] = {
                    heading = value.heading,
                    coords = { originX - value.x, originY - value.y },
                    unitFamilies = families,
                    originalType = value.type,
                    isScenery = false,
                    isSpecificType = families == nil,
                }
                locIndex = locIndex + 1
            end
            outputMap[templateName].groups[outputMap[templateName].groupsIndex] = {
                coords = { outputMap[templateName].coords[1] - originX, outputMap[templateName].coords[2] - originY },
                units = locations
            }
            outputMap[templateName].groupsIndex = outputMap[templateName].groupsIndex + 1
        end
    end

     if (country.static ~= nil) then
        for _, groupValue in orderedPairs(country.static.group) do
            local groupName = groupValue.name
            local templateName = mysplit(groupName, "-")[1]
            local originX = groupValue.x
            local originY = groupValue.y
            local locations = {}
            local locIndex = 1
            for _, value in orderedPairs(groupValue.units) do
                local families = switchFamilies(value.type, unitTypeToFamilies)
                if families[1] == "UNKNOWN" then
                    families = nil
                end
                locations[locIndex] = {
                    heading = value.heading,
                    coords = { originX - value.x, originY - value.y },
                    unitFamilies = families,
                    originalType = value.type,
                    isScenery = true,
                    isSpecificType = families == nil,
                }
                locIndex = locIndex + 1
            end
            outputMap[templateName].groups[outputMap[templateName].groupsIndex] = {
                coords = { outputMap[templateName].coords[1] - originX, outputMap[templateName].coords[2] - originY },
                units = locations
            }
            outputMap[templateName].groupsIndex = outputMap[templateName].groupsIndex + 1
        end
    end
end

local output = {}
local index = 1 
for _, marker in orderedPairs(outputMap) do
    output[index] = marker
    index = index + 1
end

io.write(json.encode(output))

io.close(file)
