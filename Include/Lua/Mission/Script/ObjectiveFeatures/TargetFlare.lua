function briefingRoom.mission.objectiveFeaturesCommon.targetDesignationFlareDoFlare(args)
  trigger.action.signalFlare(args.position, trigger.flareColor.Yellow, 0)
end

function briefingRoom.mission.objectiveFeaturesCommon.targetDesignationFlare(args)
  local objectiveIndex = args[1]
  briefingRoom.radioManager.play("$LANG_PILOT$: $LANG_FLAIRREQUEST$", "RadioPilotMarkSelfWithFlare")
  local objective = briefingRoom.mission.objectives[objectiveIndex]
  local objectiveFeature = briefingRoom.mission.objectiveFeatures[objectiveIndex]

  if objectiveFeature.targetDesignationFlareFlaresLeft <= 0 then
    briefingRoom.radioManager.play(objective.name.." $LANG_JTAC$: $LANG_FLAIRNOFLAIRS$", "RadioSupportShootingFlareOut", briefingRoom.radioManager.getAnswerDelay())
    return
  end

  if table.count(briefingRoom.mission.objectives[objectiveIndex].unitNames) == 0 then -- no target units left
    briefingRoom.radioManager.play(objective.name.." $LANG_JTAC$:$LANG_NOTARGET$", "RadioSupportNoTarget", briefingRoom.radioManager.getAnswerDelay())
    return
  end

  local unit = dcsExtensions.getUnitOrStatic(briefingRoom.mission.objectives[objectiveIndex].unitNames[1])
  if unit == nil then -- no unit nor static found with the ID
    briefingRoom.radioManager.play(objective.name.." $LANG_JTAC$:$LANG_NOTARGET$", "RadioSupportNoTarget", briefingRoom.radioManager.getAnswerDelay())
    return
  end
  
  objectiveFeature.targetDesignationFlareFlaresLeft = objectiveFeature.targetDesignationFlareFlaresLeft - 1

  local args = { ["position"] = unit:getPoint() }

  if briefingRoom.mission.objectiveFeatures[objectiveIndex].flareInnacurate then 
    local heading = math.random(0, 359)
    local distance = math.floor(math.random(5, 20)) * 100
    args.position.x = args.position.x - math.cos(heading * DEGREES_TO_RADIANS) * distance;
    args.position.z = args.position.z - math.sin(heading * DEGREES_TO_RADIANS) * distance;
    briefingRoom.radioManager.play(objective.name.." $LANG_JTAC$: $LANG_FLAIRAFFIRM$ "..dcsExtensions.degreesToCardinalDirection(heading).." of the Flare.", "RadioSupportShootingFlare", briefingRoom.radioManager.getAnswerDelay(), briefingRoom.mission.objectiveFeaturesCommon.targetDesignationFlareDoFlare, args)
  else
    briefingRoom.radioManager.play(objective.name.." $LANG_JTAC$: $LANG_FLAIRAFFIRM$", "RadioSupportShootingFlare", briefingRoom.radioManager.getAnswerDelay(), briefingRoom.mission.objectiveFeaturesCommon.targetDesignationFlareDoFlare, args)
  end
end

function briefingRoom.mission.objectiveFeaturesCommon.registerTargetDesignationFlare(objectiveIndex, innacurate)
  briefingRoom.mission.objectiveFeatures[objectiveIndex].flareInnacurate = innacurate or false 
  briefingRoom.mission.objectiveFeatures[objectiveIndex].targetDesignationFlareFlaresLeft = 5
  table.insert(briefingRoom.mission.objectives[objectiveIndex].f10Commands, {text = "$LANG_FLAIRMENU$", func = briefingRoom.mission.objectiveFeaturesCommon.targetDesignationFlare, args =  {objectiveIndex}})
end