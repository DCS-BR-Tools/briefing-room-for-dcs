-- Extracts groups out of mission lua files to create location templates for that given theater.

require "mission" -- Mission lua file
require "dataExtractors/TemplateCommon"
json = require "json"


file = io.open("LocationTemplates.json", "w")
io.output(file)

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
                    coords = { originX - value.x, originY - value.y },
                    heading = value.heading,
                    originalType = value.type,
                    unitFamilies = switchFamilies(value.type, unitTypeToFamilies)
                }
                locIndex = locIndex + 1
            end
            output[index] = { coords = { originX, originY }, locationType = mysplit(groupValue.name, "-")[1], units =
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
                    coords = { originX - value.x, originY - value.y },
                    heading = value.heading,
                    originalType = value.type,
                    unitFamilies = switchFamilies(value.type, unitTypeToFamilies)
                }
                locIndex = locIndex + 1
            end
            output[index] = { coords = { originX, originY }, locationType = mysplit(groupValue.name, "-")[1], units =
            locations }
            index = index + 1
        end
    end --actualcode
end

io.write(json.encode(output))

io.close(file)
