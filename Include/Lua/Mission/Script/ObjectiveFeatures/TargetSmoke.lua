
function briefingRoom.mission.objectiveFeaturesCommon.targetDesignationSmokeMarker(args)
  local objectiveIndex = args[1]
  local objective = briefingRoom.mission.objectives[objectiveIndex]
  briefingRoom.radioManager.play("$LANG_PILOT$: $LANG_SMOKEREQUEST$", "RadioPilotMarkTargetWithSmoke")

  if table.count(briefingRoom.mission.objectives[objectiveIndex].unitNames) == 0 then -- no target units left
    briefingRoom.radioManager.play(objective.name.." $LANG_JTAC$:$LANG_NOTARGET$", "RadioSupportNoTarget", briefingRoom.radioManager.getAnswerDelay())
    return
  end
  
  local unit = dcsExtensions.getUnitOrStatic(briefingRoom.mission.objectives[objectiveIndex].unitNames[1])
  if unit == nil then -- no unit nor static found with the ID
      briefingRoom.radioManager.play(objective.name.." $LANG_JTAC$:$LANG_NOTARGET$", "RadioSupportNoTarget", briefingRoom.radioManager.getAnswerDelay())
      return
  end
  
  local timeNow = timer.getAbsTime()
  
  if timeNow < briefingRoom.mission.objectiveFeatures[objectiveIndex].targetDesignationSmokeMarkerNextSmoke then -- Smoke not ready
    briefingRoom.radioManager.play(objective.name.." $LANG_JTAC$: $LANG_SMOKEALREADY$", "RadioSupportTargetAlreadyMarkedWithSmoke", briefingRoom.radioManager.getAnswerDelay())
    return
  end
  
  -- Set cooldown for next smoke
  briefingRoom.mission.objectiveFeatures[objectiveIndex].targetDesignationSmokeMarkerNextSmoke = timeNow + SMOKE_DURATION
  
  -- Play radio message and setup smoke creating function
  local args = { position = unit:getPoint(), color = trigger.smokeColor.Red }
  if unit:getCoalition() == briefingRoom.playerCoalition then args.color = trigger.smokeColor.Green end
  if briefingRoom.mission.objectiveFeatures[objectiveIndex].smokeInnacurate then 
    local heading = math.random(0, 359)
    local distance = math.floor(math.random(5, 20)) * 100
    args.position.x = args.position.x - math.cos(heading * DEGREES_TO_RADIANS) * distance;
    args.position.z = args.position.z - math.sin(heading * DEGREES_TO_RADIANS) * distance;
    briefingRoom.radioManager.play(objective.name.." $LANG_JTAC$: $LANG_SMOKEAFFIRM$ "..dcsExtensions.degreesToCardinalDirection(heading).." of the smoke.", "RadioSupportTargetMarkedWithSmoke", briefingRoom.radioManager.getAnswerDelay(), briefingRoom.mission.objectiveFeaturesCommon.targetDesignationSmokeMarkerDoSmoke, args)
  else
    briefingRoom.radioManager.play(objective.name.." $LANG_JTAC$: $LANG_SMOKEAFFIRM$", "RadioSupportTargetMarkedWithSmoke", briefingRoom.radioManager.getAnswerDelay(), briefingRoom.mission.objectiveFeaturesCommon.targetDesignationSmokeMarkerDoSmoke, args)
  end
end

function briefingRoom.mission.objectiveFeaturesCommon.targetDesignationSmokeMarkerDoSmoke(args)
  local smokePosition = args.position
  trigger.action.smoke(smokePosition, args.color)
  return nil
end

function briefingRoom.mission.objectiveFeaturesCommon.registerTargetDesignationSmoke(objectiveIndex, innacurate)
  briefingRoom.mission.objectiveFeatures[objectiveIndex].smokeInnacurate = innacurate or false 
  briefingRoom.mission.objectiveFeatures[objectiveIndex].targetDesignationSmokeMarkerNextSmoke = -999999
  -- Add the command to the F10 menu
  table.insert(briefingRoom.mission.objectives[objectiveIndex].f10Commands, {text = "$LANG_SMOKEMENU$", func = briefingRoom.mission.objectiveFeaturesCommon.targetDesignationSmokeMarker, args = {objectiveIndex}})
end
