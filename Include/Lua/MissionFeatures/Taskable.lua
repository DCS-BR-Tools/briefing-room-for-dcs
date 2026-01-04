briefingRoom.taskables = {}
function briefingRoom.taskables.markerManager(dataObj)
    local table = {}
    function table:onEvent(event)
        if event.id == world.event.S_EVENT_MARK_REMOVED then
            if dataObj.markID ~= nil and event.idx == dataObj.markID then
                if not dataObj.disableCooRemovedRadioMessage then
                    briefingRoom.radioManager.play(": $LANG_DISCARDCOORDINATES$", "RadioCoordinatesDiscardedM")
                end
                dataObj.markID = nil
            end
        elseif event.id == world.event.S_EVENT_MARK_ADDED then
            local markText = string.lower(tostring(event.text or ""))
            if markText == dataObj.MARKER_NAME then
                if dataObj.markID ~= nil then
                    dataObj.disableCooRemovedRadioMessage = true
                    trigger.action.removeMark(dataObj.markID)
                    dataObj.disableCooRemovedRadioMessage = false
                end
                dataObj.markID = event.idx
                briefingRoom.radioManager.play(dataObj.LANG_UNIT .. ": $LANG_UPDATECOORDINATES$",
                    "RadioCoordinatesUpdatedM")
                return
            end
        elseif event.id == world.event.S_EVENT_MARK_CHANGE then
            local markText = string.lower(tostring(event.text or ""))

            if markText == dataObj.MARKER_NAME then
                briefingRoom.radioManager.play(dataObj.LANG_UNIT .. ": $LANG_UPDATECOORDINATES$",
                    "RadioCoordinatesUpdatedM")
                if dataObj.markID ~= nil then
                    dataObj.disableCooRemovedRadioMessage = true
                    trigger.action.removeMark(dataObj.markID)
                    dataObj.disableCooRemovedRadioMessage = false
                end
                dataObj.markID = event.idx
            elseif dataObj.markID ~= nil and event.idx == dataObj.markID then
                briefingRoom.radioManager.play(dataObj.LANG_UNIT .. ": $LANG_DISCARDCOORDINATES$",
                    "RadioCoordinatesDiscardedM")
                dataObj.markID = nil
            end
        end
    end

    return table
end

function briefingRoom.taskables.launchCurry(dataObj, groupName, newTaskFunc)
    briefingRoom.radioManager.play("$LANG_PILOT$: " .. dataObj.LANG_REQEUEST, dataObj.RADIO_REQEUEST)

    if dataObj.markID == nil then
        briefingRoom.radioManager.play(dataObj.LANG_UNIT .. ": " .. dataObj.LANG_NO_COORDS, "RadioArtilleryNoCoordinates"
        , briefingRoom.radioManager.getAnswerDelay())
        return
    end
    local mark = table.find(world.getMarkPanels(), function(o, k, i) return o.idx == dataObj.markID end)
    if mark == nil then
        briefingRoom.radioManager.play(dataObj.LANG_UNIT .. ": " .. dataObj.LANG_NO_COORDS, "RadioArtilleryNoCoordinates"
        , briefingRoom.radioManager.getAnswerDelay())
        return
    end

    local group = Group.getByName(groupName)
    if group == nil then return end

    group:activate()
    local Start = {
        id = 'Start',
        params = {
        }
    }
    group:getController():setCommand(Start)
    timer.scheduleFunction(briefingRoom.taskables.setTaskCurry(newTaskFunc, dataObj.markID,
        groupName), {}, timer.getTime() + 10)
    briefingRoom.radioManager.play(dataObj.LANG_UNIT .. ": " .. dataObj.LANG_AFFIRM, dataObj.RADIO_AFFIRM,
        briefingRoom.radioManager.getAnswerDelay())
end

function briefingRoom.taskables.setTaskCurry(newTaskFunc, markerId, groupName)
    return function()
        if markerId == nil then return end

        local mark = table.find(world.getMarkPanels(), function(o, k, i) return o.idx == markerId end)
        if mark == nil then return end

        local group = Group.getByName(groupName)
        if group == nil then return end

        group:getController():setTask(newTaskFunc(group, mark))
    end
end
