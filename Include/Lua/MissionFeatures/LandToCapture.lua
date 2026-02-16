briefingRoom.mission.missionFeatures.LandToCapture = {}
briefingRoom.mission.missionFeatures.LandToCapture.wasHasStarted = false -- has the enemy been attacked yet?

briefingRoom.mission.missionFeatures.LandToCapture.eventHandler = {}
function briefingRoom.mission.missionFeatures.LandToCapture.eventHandler:onEvent(event)
    if event.id == world.event.S_EVENT_LAND and
        event.place ~= nil and
        event.initiator ~= nil and
        event.initiator.getPlayerName and
        event.initiator:getCoalition() ~= event.place:getCoalition()
    then
        local pos = event.place:getPoint()
        local spawnUnits = { {
            ["y"] = pos.z + math.random(-500, 500),
            ["type"] = "JTAC",
            ["name"] = "JTAC"..tostring(math.random(1, 1000)),
            ["heading"] = 0,
            ["playerCanDrive"] = true,
            ["skill"] = "Excellent",
            ["x"] = pos.x + math.random(-500, 500),
            }}
            mist.dynAdd({
            units = spawnUnits,
            country = event.initiator:getCountry(),
            category = Group.Category.GROUND
            })
    end
end

-- Enable event handler
world.addEventHandler(briefingRoom.mission.missionFeatures.LandToCapture.eventHandler)