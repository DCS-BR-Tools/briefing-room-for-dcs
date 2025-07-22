require "mission" -- Mission lua file
require "dataExtractors/TemplateCommon"
json = require "json"

file = io.open ("SpawnPoints.json", "w")
io.output(file)

-- Waypoints

local index = 1
local ouput = {}
for _,group in orderedPairs(mission.coalition.red.country[1].vehicle.group) do --actualcode
    for _,point in orderedPairs(group.route.points) do --actualcode
        ouput[index] = {coords = {point.x,point.y}, BRtype ="LandSmall"}
        index = index + 1
    end
end

for _,group in orderedPairs(mission.coalition.neutrals.country[1].vehicle.group) do --actualcode
    for _,point in orderedPairs(group.route.points) do --actualcode
        ouput[index] = {coords = {point.x,point.y}, BRtype ="LandMedium"}
        index = index + 1
    end
end

for _,group in orderedPairs(mission.coalition.blue.country[1].vehicle.group) do --actualcode
    for _,point in orderedPairs(group.route.points) do --actualcode
        ouput[index] = {coords = {point.x,point.y}, BRtype ="LandLarge"}
        index = index + 1
    end
end

io.write(json.encode(ouput))

io.close(file)

