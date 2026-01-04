
briefingRoom.mission.objectiveFeaturesCommon.targetDesignationLaser = {}

function briefingRoom.mission.objectiveFeaturesCommon.targetDesignationLaser.laserWatch(args, time)
  local objectiveIndex = args[1]
  local objFeature = briefingRoom.mission.objectiveFeatures[objectiveIndex]
  local objective = briefingRoom.mission.objectives[objectiveIndex]
  -- if lasing target is set...
  if objFeature.targetDesignationLaser.laserTarget == nil then
    return time + 1 -- next update in one second
  end

  if not objFeature.targetDesignationLaser.laserTarget:isExist() or not table.contains(objective.unitNames, objFeature.targetDesignationLaser.laserTarget:getName()) then -- target is considered complete
    briefingRoom.debugPrint("JTAC objectiveIndex: $LANG_LASERTARGETDESTROYED$", 1)
    local unit = briefingRoom.mission.objectiveFeaturesCommon.targetDesignationLaser.setRandomTarget(objectiveIndex)
    if unit == nil then
      briefingRoom.mission.objectiveFeaturesCommon.targetDesignationLaser.deleteLaser(objectiveIndex)
      briefingRoom.radioManager.play(objective.name.." $LANG_JTAC$: $LANG_LASERNOTARGET$", "RadioSupportLasingNoMoreTargets", briefingRoom.radioManager.getAnswerDelay())
      return
    end
    briefingRoom.radioManager.play(objective.name.." $LANG_JTAC$: $LANG_LASERNEXTTARGET$", "RadioSupportLasingNextTarget", briefingRoom.radioManager.getAnswerDelay())
  end

  briefingRoom.mission.objectiveFeaturesCommon.targetDesignationLaser.updateLaserPos(objectiveIndex)
  return time + 1
end

function briefingRoom.mission.objectiveFeaturesCommon.targetDesignationLaser.turnOn(args)
  local objectiveIndex = args[1]
  local objFeature = briefingRoom.mission.objectiveFeatures[objectiveIndex]
  local objective = briefingRoom.mission.objectives[objectiveIndex]
  briefingRoom.radioManager.play("$LANG_PILOT$: $LANG_LASERREQUEST$", "RadioPilotLaseTarget")

  -- already lasing something
  if objFeature.targetDesignationLaser.laserTarget ~= nil then
    briefingRoom.radioManager.play(objective.name.." $LANG_JTAC$: $LANG_LASERALREADYPAINTING$ "..tostring(objFeature.targetDesignationLaser.laserCode)..".", "RadioSupportTargetLasingAlready", briefingRoom.radioManager.getAnswerDelay())
    return
  end

  -- no target units left
  local unit = briefingRoom.mission.objectiveFeaturesCommon.targetDesignationLaser.setRandomTarget(objectiveIndex)
  if unit == nil then
    briefingRoom.radioManager.play(objective.name.." $LANG_JTAC$: $LANG_LASERNOTARGETREMAINING$", "RadioSupportNoTarget", briefingRoom.radioManager.getAnswerDelay())
    return
  end
  briefingRoom.radioManager.play(objective.name.." $LANG_JTAC$: $LANG_LASERAFFIRM$ "..tostring(objFeature.targetDesignationLaser.laserCode)..".", "RadioSupportLasingOk", briefingRoom.radioManager.getAnswerDelay())
  missionCommands.addCommandForCoalition(briefingRoom.playerCoalition, "$LANG_LASERMENUNEWTARGET$", briefingRoom.f10Menu.objectives[objectiveIndex], briefingRoom.mission.objectiveFeaturesCommon.targetDesignationLaser.newTarget, {objectiveIndex})
  missionCommands.addCommandForCoalition(briefingRoom.playerCoalition, "$LANG_LASERMENUSTOP$", briefingRoom.f10Menu.objectives[objectiveIndex], briefingRoom.mission.objectiveFeaturesCommon.targetDesignationLaser.turnOff, {objectiveIndex})
end

function briefingRoom.mission.objectiveFeaturesCommon.targetDesignationLaser.turnOff(args)
  local objectiveIndex = args[1]
  local objFeature = briefingRoom.mission.objectiveFeatures[objectiveIndex]
  local objective = briefingRoom.mission.objectives[objectiveIndex]
  briefingRoom.radioManager.play("$LANG_PILOT$: Terminate. Laser off.", "RadioPilotLaseTargetStop")
  -- not lasing anything
  if objFeature.targetDesignationLaser.laserTarget == nil then
    briefingRoom.radioManager.play(objective.name.." $LANG_JTAC$: $LANG_LASERALREADYOFF$", "RadioSupportLasingNotLasing", briefingRoom.radioManager.getAnswerDelay())
    return
  end

  briefingRoom.mission.objectiveFeaturesCommon.targetDesignationLaser.deleteLaser(objectiveIndex)
  briefingRoom.radioManager.play(objective.name.." $LANG_JTAC$: $LANG_LASEROFF$", "RadioSupportLasingStopped", briefingRoom.radioManager.getAnswerDelay())
end

-- Get new target
function briefingRoom.mission.objectiveFeaturesCommon.targetDesignationLaser.newTarget(args)
  local objectiveIndex = args[1]
  local objective = briefingRoom.mission.objectives[objectiveIndex]
  briefingRoom.radioManager.play("$LANG_PILOT$: $LANG_LASERNEWTARGET$", "RadioPilotLaseDiffrentTarget")

  -- no target units left
  local unit = briefingRoom.mission.objectiveFeaturesCommon.targetDesignationLaser.setRandomTarget(objectiveIndex)
  if unit == nil then
    briefingRoom.radioManager.play(objective.name.." $LANG_JTAC$:$LANG_NOTARGET$", "RadioSupportNoTarget", briefingRoom.radioManager.getAnswerDelay())
    return
  end
  briefingRoom.radioManager.play(objective.name.." $LANG_JTAC$: $LANG_LASERNEXTTARGET$", "RadioSupportLasingNextTarget", briefingRoom.radioManager.getAnswerDelay())
end
function briefingRoom.mission.objectiveFeaturesCommon.targetDesignationLaser.updateLaserPos(objectiveIndex)
  local objFeature = briefingRoom.mission.objectiveFeatures[objectiveIndex]
  local targetPos = objFeature.targetDesignationLaser.laserTarget:getPoint()
  local targetSpeed = objFeature.targetDesignationLaser.laserTarget:getVelocity()
  -- adds a small offset so that the laser is always where the (moving) target will be, not where it is
  targetPos.x = targetPos.x + targetSpeed.x
  targetPos.y = targetPos.y + 2.0
  targetPos.z = targetPos.z + targetSpeed.z
  if objFeature.targetDesignationLaser.laserSpot == nil then
    objFeature.targetDesignationLaser.laserIRSpot = Spot.createInfraRed(objFeature.targetDesignationLaser.laserTarget, { x = math.random(-1000,1000), y = 2000, z = math.random(-1000,1000) }, targetPos)
    objFeature.targetDesignationLaser.laserSpot = Spot.createLaser(objFeature.targetDesignationLaser.laserTarget, { x = math.random(-1000,1000), y = 2000, z = math.random(-1000,1000) }, targetPos, objFeature.targetDesignationLaser.laserCode)
    briefingRoom.debugPrint("JTAC objectiveIndex: Created Laser "..objFeature.targetDesignationLaser.laserSpot:getCode()..":"..tostring(targetPos.x)..","..tostring(targetPos.y)..","..tostring(targetPos.z), 1)
  else -- spot already exists, update its position
    objFeature.targetDesignationLaser.laserIRSpot:setPoint(targetPos)
    objFeature.targetDesignationLaser.laserSpot:setPoint(targetPos)
    briefingRoom.debugPrint("JTAC objectiveIndex: Update Laser Pos "..objFeature.targetDesignationLaser.laserSpot:getCode()..":"..tostring(targetPos.x)..","..tostring(targetPos.y)..","..tostring(targetPos.z), 1)
  end

end

function briefingRoom.mission.objectiveFeaturesCommon.targetDesignationLaser.deleteLaser(objectiveIndex)
  local objFeature = briefingRoom.mission.objectiveFeatures[objectiveIndex]
  if objFeature.targetDesignationLaser.laserSpot ~= nil then
    Spot.destroy(objFeature.targetDesignationLaser.laserSpot)
    objFeature.targetDesignationLaser.laserSpot = nil
    Spot.destroy(objFeature.targetDesignationLaser.laserIRSpot)
    objFeature.targetDesignationLaser.laserIRSpot = nil
  end

  -- unset target and play radio message
  objFeature.targetDesignationLaser.laserTarget = nil
  briefingRoom.debugPrint("JTAC objectiveIndex: Deleted Laser", 1)
end

function briefingRoom.mission.objectiveFeaturesCommon.targetDesignationLaser.setRandomTarget(objectiveIndex)
  local objective = briefingRoom.mission.objectives[objectiveIndex]
  local randomUnitName = math.randomFromHashTable(table.filter(objective.unitNames, function(o, k, i)
    local u = dcsExtensions.getUnitOrStatic(o)
    if u == nil then
      return false
    end
    return u:isExist()
  end))
  if randomUnitName == "" or randomUnitName == nil then
    return nil
  end
  local unit = dcsExtensions.getUnitOrStatic(randomUnitName)
    if unit == nil then -- no unit nor static found with the ID
      return nil
  end
  briefingRoom.mission.objectiveFeatures[objectiveIndex].targetDesignationLaser.laserTarget = unit
  briefingRoom.debugPrint("JTAC objectiveIndex: Assigned Laser Target:"..randomUnitName, 1)
  return unit
end

function briefingRoom.mission.objectiveFeaturesCommon.registerTargetDesignationLaser(objectiveIndex, laserCode)
  briefingRoom.mission.objectiveFeatures[objectiveIndex].targetDesignationLaser = { }
  briefingRoom.mission.objectiveFeatures[objectiveIndex].targetDesignationLaser.laserSpot = nil -- current laser spot
  briefingRoom.mission.objectiveFeatures[objectiveIndex].targetDesignationLaser.laserIRSpot = nil -- current laser spot
  briefingRoom.mission.objectiveFeatures[objectiveIndex].targetDesignationLaser.laserTarget = nil -- current lased target
  briefingRoom.mission.objectiveFeatures[objectiveIndex].targetDesignationLaser.laserCode = laserCode -- current lased target


-- Begin updating laser designation
timer.scheduleFunction(briefingRoom.mission.objectiveFeaturesCommon.targetDesignationLaser.laserWatch, {objectiveIndex}, timer.getTime() + 1)

table.insert(briefingRoom.mission.objectives[objectiveIndex].f10Commands, {text = "$LANG_LASERMENUNEW$", func = briefingRoom.mission.objectiveFeaturesCommon.targetDesignationLaser.turnOn, args =  {objectiveIndex}})
end