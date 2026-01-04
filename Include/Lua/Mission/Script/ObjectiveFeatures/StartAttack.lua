function briefingRoom.mission.objectivesTriggersCommon.startAttack(args)
    local objectiveIndex = args[1]
    briefingRoom.radioManager.play("$LANG_PILOT$: $LANG_LAUNCHATTACKREQUEST$", "RadioPilotBeginYourAttack")
    local groupNames = dcsExtensions.getGroupNamesContaining("%-STGT%-$OBJECTIVENAME$")
    briefingRoom.debugPrint("Activating Attack group objectiveIndex: " .. table.count(groupNames), 1)
    for _, value in pairs(groupNames) do
        local acGroup = Group.getByName(value) -- get the group
        if acGroup ~= nil then               -- activate the group, if it exists
            acGroup:activate()
            local Start = {
                id = 'Start',
                params = {}
            }
            acGroup:getController():setCommand(Start)
            briefingRoom.debugPrint("Activating Attack group objectiveIndex: " .. value, 1)
        end
    end
    briefingRoom.radioManager.play("$LANG_TROOP$: $LANG_BEGINATTACK$", "RadioOtherPilotBeginAttack")
    local idx = briefingRoom.mission.objectiveFeatures[objectiveIndex].startAttackCommandIndex
    missionCommands.removeItemForCoalition(briefingRoom.playerCoalition,
        briefingRoom.mission.objectives[objectiveIndex].f10Commands[idx].commandPath)
end

function briefingRoom.mission.objectiveFeaturesCommon.registerStartAttack(objectiveIndex)
    table.insert(briefingRoom.mission.objectives[objectiveIndex].f10Commands,
        { text = "$LANG_LAUNCHATTACK$", func = briefingRoom.mission.objectivesTriggersCommon.startAttack, args = { objectiveIndex } })
    briefingRoom.mission.objectiveFeatures[objectiveIndex].startAttackCommandIndex = table.count(briefingRoom.mission
    .objectives[objectiveIndex].f10Commands)
end
