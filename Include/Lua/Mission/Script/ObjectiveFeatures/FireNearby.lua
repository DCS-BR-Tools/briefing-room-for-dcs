function briefingRoom.mission.objectiveFeaturesCommon.registerFireNearby(objectiveIndex)
    local unit = dcsExtensions.getUnitOrStatic(briefingRoom.mission.objectives[objectiveIndex].unitNames[1])
    if unit == nil then -- no unit nor static found with the ID
        briefingRoom.radioManager.play(objective.name .. " $LANG_JTAC$: $LANG_NOTARGET$", "RadioSupportNoTarget",
            briefingRoom.radioManager.getAnswerDelay())
        return
    end
    local unitVec3 = unit:getPoint()
    local unitVec2 = { x = unitVec3.x, y = unitVec3.z }
    unitVec2.x = unitVec2.x + math.random(-3000, 3000)
    unitVec2.y = unitVec2.y + math.random(-3000, 3000)
    local spawnPoint = { x = unitVec2.x, y = land.getHeight(unitVec2), z = unitVec2.y }
    trigger.action.effectSmokeBig(spawnPoint, math.random(2, 3), math.random(7, 10) * 0.1)
end
