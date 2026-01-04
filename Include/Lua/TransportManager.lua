-- ===================================================================================
-- 2.4 - TRANSPORT MANAGER: common event handler used during the mission
-- ===================================================================================
briefingRoom.transportManager = {}
briefingRoom.transportManager.transportRoster = {}
briefingRoom.transportManager.maxTroops = 10
briefingRoom.transportManager.maxTroopsByType = {
  SA342Mistral=2,
  SA342Minigun=2,
  SA342L=2,
  SA342M=2,
  ["UH-1H"]=8,
  ["Mi-8MT"]=24,
  ["UH-60L"]=11,
  ["Mi-24P"]=8,
  ["AH-64D_BLK_II"]=2,
  ["CH-47Fbl1"]=31,
  OH58D=2,
  ["Ka-50"]=1,
  ["Ka-50_3"]=1,
  ["OH-6A"]=4,
}

function briefingRoom.transportManager.initTransport(transportUnitName)
  briefingRoom.transportManager.transportRoster[transportUnitName] = {
    troops = {}
  }
end

function briefingRoom.transportManager.troopsMoveToGetIn(transportUnitName, unitNames)
  local _leader = Unit.getByName(unitNames[1])
  local _helo = Unit.getByName(transportUnitName)
  local _group = _leader:getGroup()
  local _destination = dcsExtensions.toVec2(_helo:getPoint())
  local _distance = dcsExtensions.getDistance(_destination, dcsExtensions.toVec2(_leader:getPoint()))
  local _time = math.floor((_distance * 135) / 500)
  -- BLOCK TAKEN FROM MIST CSAR

    local _path = {}
    table.insert(_path, mist.ground.buildWP(_leader:getPoint(), 'Off Road', 50))
    table.insert(_path, mist.ground.buildWP(_destination, 'Off Road', 50))

    local _mission = {
        id = 'Mission',
        params = {
            route = {
                points = _path
            },
        },
    }

    -- delayed 2 second to work around bug
    timer.scheduleFunction(function(_arg)
        local _grp = Group.getByName(_arg[1])

        if _grp ~= nil then
            local _controller = _grp:getController();
            Controller.setOption(_controller, AI.Option.Ground.id.ALARM_STATE, AI.Option.Ground.val.ALARM_STATE.GREEN)
            Controller.setOption(_controller, AI.Option.Ground.id.ROE, AI.Option.Ground.val.ROE.WEAPON_HOLD)
            _controller:setTask(_arg[2])
        end
    end
        , { _group:getName(), _mission }, timer.getTime() + 2)
    -- BLOCK TAKEN FROM MIST CSAR
    local groupUnitNames = {}
    for index, data in pairs(_group:getUnits()) do
      table.insert(groupUnitNames, data:getName())
    end
    timer.scheduleFunction(function(_arg)
      briefingRoom.transportManager.addTroopCargo(_arg[1], _arg[2])
    end, { transportUnitName, groupUnitNames }, timer.getTime() + _time)
end

function briefingRoom.transportManager.addTroopCargo(transportUnitName, unitNames)
  if not table.containsKey(briefingRoom.transportManager.transportRoster, transportUnitName) then
    briefingRoom.transportManager.initTransport(transportUnitName)
  end
  local addedCount = 0
  local transportUnit = Unit.getByName(transportUnitName)
  local maxUnitTroops = briefingRoom.transportManager.maxTroops
  local transportUnitType = transportUnit:getTypeName()
  briefingRoom.debugPrint("Transport Unit Type: "..transportUnitType)
  if table.containsKey(briefingRoom.transportManager.maxTroopsByType, transportUnitType) then
    maxUnitTroops = briefingRoom.transportManager.maxTroopsByType[transportUnitType]
  end
  briefingRoom.debugPrint("Max Troops: "..maxUnitTroops)
  for index, unitName in ipairs(unitNames) do
    local unitCount = table.count(briefingRoom.transportManager.transportRoster[transportUnitName].troops)
    briefingRoom.debugPrint("Troop Count Troops: "..unitCount)
    if unitCount == maxUnitTroops then
      briefingRoom.radioManager.play("$LANG_TROOP$: $LANG_TRANSPORTFULL$ ($LANG_TOTALTROOPS$: "..maxUnitTroops..")", "RadioTroopFull")
      return true
    end

    local isUnitAlreadyInHelo = false
    for k,v in pairs(briefingRoom.transportManager.transportRoster) do
      if table.containsKey(v.troops, unitName) then
        isUnitAlreadyInHelo = true
      end
    end

    local unit = Unit.getByName(unitName)
    if unit ~= nil and not isUnitAlreadyInHelo then
      briefingRoom.transportManager.transportRoster[transportUnitName].troops[unitName] = {
        ["type"] = unit:getTypeName(),
        ["name"] = unit:getName(),
        ["country"] = unit: getCountry()
      }
      unit:destroy()
      addedCount = addedCount + 1
    end
  end
  if addedCount > 0 then
    briefingRoom.radioManager.play("$LANG_TROOP$: $LANG_TRANSPORTALLIN$ ($LANG_TOTALTROOPS$: "..table.count(briefingRoom.transportManager.transportRoster[transportUnitName].troops)..")", "RadioTroopAllIn")
  end
end

function briefingRoom.transportManager.removeTroopCargo(transportUnitName, unitNames, unitPos)
  local transportUnit = Unit.getByName(transportUnitName)
  if not table.containsKey(briefingRoom.transportManager.transportRoster, transportUnitName) then
    briefingRoom.debugPrint("transport unload bailing no roster")
    return {}
  end
  local transportUnitPoint = unitPos or transportUnit:getPoint()
  if transportUnitPoint == nil then
    briefingRoom.debugPrint("transport unload bailing no pos")
    return {}
  end
  local removed = {}
  local spawnUnits = {}
  local country = nil
  for index, unitName in ipairs(unitNames) do
    if table.containsKey(briefingRoom.transportManager.transportRoster[transportUnitName].troops, unitName) then
      local unitDeets = briefingRoom.transportManager.transportRoster[transportUnitName].troops[unitName]
      country = unitDeets.country
      briefingRoom.transportManager.transportRoster[transportUnitName].troops[unitName] = nil
      table.insert(removed, unitName)
      table.insert(spawnUnits, {
        ["y"] = transportUnitPoint.z + math.random(-30, 30),
        ["type"] = unitDeets.type,
        ["name"] = unitDeets.name,
        ["heading"] = 0,
        ["playerCanDrive"] = true,
        ["skill"] = "Excellent",
        ["x"] = transportUnitPoint.x + math.random(-30, 30),
      })
    end
  end
  if table.count(spawnUnits) > 0 then
    mist.dynAdd({
      units = spawnUnits,
      country = country,
      category = Group.Category.GROUND
    })
  end

  if unitPos == nil then
    briefingRoom.radioManager.play("$LANG_TROOP$: $LANG_TRANSPORTEVERYONEOUT$ ($LANG_REMAININGTROOPS$: "..table.count(briefingRoom.transportManager.transportRoster[transportUnitName].troops)..")", "RadioTroopTakeoff")
  end
  return removed
end