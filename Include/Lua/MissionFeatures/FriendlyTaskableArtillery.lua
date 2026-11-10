briefingRoom.mission.missionFeatures.supportArtillery = {}
briefingRoom.mission.missionFeatures.supportArtillery.FIRE_MISSIONS_PER_OBJECTIVE = 3
briefingRoom.mission.missionFeatures.supportArtillery.AUTO_AIM_RADIUS = 1000 -- in meters
briefingRoom.mission.missionFeatures.supportArtillery.INACCURACY = 100       -- in meters
briefingRoom.mission.missionFeatures.supportArtillery.MARKER_NAME = "arty"
briefingRoom.mission.missionFeatures.supportArtillery.LANG_UNIT = "$LANG_FIRESUPPORT$"
briefingRoom.mission.missionFeatures.supportArtillery.SHELLS_PER_FIRE_MISSION = 10
briefingRoom.mission.missionFeatures.supportArtillery.fireMissionsLeft = 0
briefingRoom.mission.missionFeatures.supportArtillery.markID = nil -- ID of the mark on the map
briefingRoom.mission.missionFeatures.supportArtillery.shellsLeftInFireMission = 0
briefingRoom.mission.missionFeatures.supportArtillery.disableCooRemovedRadioMessage = false
briefingRoom.mission.missionFeatures.supportArtillery.menuCommand = nil

function briefingRoom.mission.missionFeatures.supportArtillery.isArtilleryAvailable()
  local groupName = briefingRoom.mission.missionFeatures.groupNames.friendlyTaskableArtillery
  if dcsExtensions.getAliveUnitInGroup(groupName) == nil then
    if briefingRoom.mission.missionFeatures.supportArtillery.menuCommand ~= nil then
      missionCommands.removeItemForCoalition(briefingRoom.playerCoalition,
        briefingRoom.mission.missionFeatures.supportArtillery.menuCommand)
      briefingRoom.mission.missionFeatures.supportArtillery.menuCommand = nil
    end
    return false
  end
  return true
end

function briefingRoom.mission.missionFeatures.supportArtillery.getAutoAimPoint(basePoint)
  local impactPoint = table.deepCopy(basePoint)
  local impactPointV2 = dcsExtensions.toVec2(impactPoint)
  local enemyUnits = dcsExtensions.getCoalitionUnits(briefingRoom.enemyCoalition)
  local candidates = {}
  for _, u in ipairs(enemyUnits) do
    if not u:inAir() and dcsExtensions.getDistance(dcsExtensions.toVec2(u:getPoint()), impactPointV2) <
        briefingRoom.mission.missionFeatures.supportArtillery.AUTO_AIM_RADIUS then
      table.insert(candidates, u)
    end
  end
  if #candidates > 0 then
    local targetUnit = math.randomFromTable(candidates)
    impactPoint = table.deepCopy(targetUnit:getPoint())
  end
  return impactPoint
end

function briefingRoom.mission.missionFeatures.supportArtillery.doShell(args, time)
  if not briefingRoom.mission.missionFeatures.supportArtillery.isArtilleryAvailable() then
    briefingRoom.mission.missionFeatures.supportArtillery.shellsLeftInFireMission = 0
    return nil
  end

  briefingRoom.mission.missionFeatures.supportArtillery.shellsLeftInFireMission = briefingRoom.mission.missionFeatures
      .supportArtillery
      .shellsLeftInFireMission - 1

  for i = 1, 3 do
    local impactPoint = table.deepCopy(args.position)
    if i == 1 then
      impactPoint = briefingRoom.mission.missionFeatures.supportArtillery.getAutoAimPoint(args.position)
    end

    local inaccuracy = math.randomPointInCircle({ ["x"] = 0, ["y"] = 0 },
      briefingRoom.mission.missionFeatures.supportArtillery.INACCURACY)
    impactPoint.x = impactPoint.x + inaccuracy.x
    impactPoint.z = impactPoint.z + inaccuracy.y
    impactPoint.y = land.getHeight({ ["x"] = impactPoint.x, ["y"] = impactPoint.z }) + 0.5

    trigger.action.explosion(impactPoint, 100)
  end

  if briefingRoom.mission.missionFeatures.supportArtillery.shellsLeftInFireMission <= 0 then -- no shells left in this fire mission
    return nil
  else
    return time + 1
  end
end

-- Internal function to begin executing fire mission (called when radio message is complete)
function briefingRoom.mission.missionFeatures.supportArtillery.doFireMission(args)
  if not briefingRoom.mission.missionFeatures.supportArtillery.isArtilleryAvailable() then
    return
  end

  local groupName = briefingRoom.mission.missionFeatures.groupNames.friendlyTaskableArtillery
  local unit = dcsExtensions.getAliveUnitInGroup(groupName)
  local range = briefingRoom.mission.missionFeatures.threatRanges and
      briefingRoom.mission.missionFeatures.threatRanges.friendlyTaskableArtillery
  if unit ~= nil and range ~= nil and range.max > 0 then
    local dist = dcsExtensions.getDistance(dcsExtensions.toVec2(unit:getPoint()), dcsExtensions.toVec2(args.position))
    if dist >= range.min and dist <= range.max then
      local group = Group.getByName(groupName)
      if group ~= nil then
        local targetPoint = briefingRoom.mission.missionFeatures.supportArtillery.getAutoAimPoint(args.position)
        local targetVec2 = dcsExtensions.toVec2(targetPoint)
        local fireTask = {
          id = 'FireAtPoint',
          params = {
            point = targetVec2,
            x = targetVec2.x,
            y = targetVec2.y,
            zoneRadius = briefingRoom.mission.missionFeatures.supportArtillery.INACCURACY,
            radius = briefingRoom.mission.missionFeatures.supportArtillery.INACCURACY,
            expendQty = briefingRoom.mission.missionFeatures.supportArtillery.SHELLS_PER_FIRE_MISSION,
            expendQtyEnabled = true,
            alt_type = 1,
          }
        }
        group:getController():setTask(fireTask)
        return
      end
    end
  end

  briefingRoom.mission.missionFeatures.supportArtillery.shellsLeftInFireMission = briefingRoom.mission.missionFeatures
      .supportArtillery
      .SHELLS_PER_FIRE_MISSION
  timer.scheduleFunction(briefingRoom.mission.missionFeatures.supportArtillery.doShell, args,
    timer.getTime() + math.random(30, 60))
end

-- Radio command to launch fire mission (called from F10 menu)
function briefingRoom.mission.missionFeatures.supportArtillery.launchFireMission()
  briefingRoom.radioManager.play("$LANG_PILOT$: $LANG_FIREREQUEST$", "RadioPilotArtillery")

  if not briefingRoom.mission.missionFeatures.supportArtillery.isArtilleryAvailable() or
      briefingRoom.mission.missionFeatures.supportArtillery.fireMissionsLeft <= 0 then
    briefingRoom.radioManager.play("$LANG_FIRESUPPORT$: $LANG_FIREREJECT$", "RadioArtilleryNoAmmo",
      briefingRoom.radioManager.getAnswerDelay())
    return
  end

  local marks = world.getMarkPanels()
  for _, m in ipairs(marks) do
    if briefingRoom.mission.missionFeatures.supportArtillery.markID ~= nil and
        m.idx == briefingRoom.mission.missionFeatures.supportArtillery.markID then
      local args = { ["position"] = table.deepCopy(m.pos) }
      briefingRoom.mission.missionFeatures.supportArtillery.fireMissionsLeft = briefingRoom.mission.missionFeatures
          .supportArtillery
          .fireMissionsLeft - 1
      briefingRoom.radioManager.play("$LANG_FIRESUPPORT$: $LANG_FIREAFFIRM$", "RadioArtilleryFiring",
        briefingRoom.radioManager.getAnswerDelay(), briefingRoom.mission.missionFeatures.supportArtillery.doFireMission,
        args)
      return
    end
  end

  briefingRoom.radioManager.play("$LANG_FIRESUPPORT$: $LANG_FIRENOCOORDINATES$", "RadioArtilleryNoCoordinates",
    briefingRoom.radioManager.getAnswerDelay())
end

-- Set the correct number of fire missions
briefingRoom.mission.missionFeatures.supportArtillery.fireMissionsLeft = briefingRoom.mission.missionFeatures
    .supportArtillery
    .FIRE_MISSIONS_PER_OBJECTIVE * math.max(1, table.count(briefingRoom.mission.objectives))

-- Add F10 menu command
briefingRoom.mission.missionFeatures.supportArtillery.menuCommand = missionCommands.addCommandForCoalition(
  briefingRoom.playerCoalition, "$LANG_FIREMENU$", briefingRoom.f10Menu.missionMenu,
  briefingRoom.mission.missionFeatures.supportArtillery.launchFireMission, nil)

-- Enable event handler
local artilleryMarkerHandler = briefingRoom.taskables.markerManager(briefingRoom.mission.missionFeatures
  .supportArtillery)
local baseArtilleryOnEvent = artilleryMarkerHandler.onEvent
function artilleryMarkerHandler:onEvent(event)
  if event.id == world.event.S_EVENT_DEAD or event.id == world.event.S_EVENT_UNIT_LOST then
    briefingRoom.mission.missionFeatures.supportArtillery.isArtilleryAvailable()
  end
  baseArtilleryOnEvent(self, event)
end

world.addEventHandler(artilleryMarkerHandler)
