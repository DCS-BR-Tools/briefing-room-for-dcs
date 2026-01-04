
function briefingRoom.mission.objectiveFeaturesCommon.targetDesignationIlluminationBombDoBomb(args)
  args.position.y = args.position.y + 1250 + math.random(0, 500)
  trigger.action.illuminationBomb(args.position, 100000)
end


function briefingRoom.mission.objectiveFeaturesCommon.targetDesignationIlluminationBomb(args)
  local objectiveIndex = args[1]
  briefingRoom.radioManager.play("$LANG_PILOT$: $LANG_ILLUMINATIONREQUEST$", "RadioPilotDropIlluminationBomb")
  local objective = briefingRoom.mission.objectives[objectiveIndex]
  local objectiveFeature = briefingRoom.mission.objectiveFeatures[objectiveIndex]
  -- out of bombs
  if objectiveFeature.targetDesignationIlluminationBombBombsLeft <= 0 then
    briefingRoom.radioManager.play(objective.name.." $LANG_RECON$: $LANG_ILLUMINATIONREJECT$", "RadioSupportIlluminationBombOut", briefingRoom.radioManager.getAnswerDelay())
    return
  end
  
  if table.count(briefingRoom.mission.objectives[objectiveIndex].unitNames) == 0 then -- no target units left
    briefingRoom.radioManager.play(objective.name.." $LANG_RECON$:$LANG_NOTARGET$", "RadioSupportNoTarget", briefingRoom.radioManager.getAnswerDelay())
    return
  end

  local unit = dcsExtensions.getUnitOrStatic(briefingRoom.mission.objectives[objectiveIndex].unitNames[1])
  if unit == nil then -- no unit nor static found with the ID
    briefingRoom.radioManager.play(objective.name.." $LANG_RECON$:$LANG_NOTARGET$", "RadioSupportNoTarget", briefingRoom.radioManager.getAnswerDelay())
    return
  end

  objectiveFeature.targetDesignationIlluminationBombBombsLeft = objectiveFeature.targetDesignationIlluminationBombBombsLeft - 1

  local args = { ["position"] = unit:getPoint() }
  if briefingRoom.mission.objectiveFeatures[objectiveIndex].illuminationBombInnacurate then 
    local heading = math.random(0, 359)
    local distance = math.floor(math.random(5, 20)) * 100
    args.position.x = args.position.x - math.cos(heading * DEGREES_TO_RADIANS) * distance;
    args.position.z = args.position.z - math.sin(heading * DEGREES_TO_RADIANS) * distance;
    briefingRoom.radioManager.play(objective.name.." $LANG_RECON$: $LANG_ILLUMINATIONAFFIRM$ "..dcsExtensions.degreesToCardinalDirection(heading).." of the Illumination.", "RadioSupportIlluminationBomb", briefingRoom.radioManager.getAnswerDelay(), briefingRoom.mission.objectiveFeaturesCommon.targetDesignationIlluminationBombDoBomb, args)
  else
    briefingRoom.radioManager.play(objective.name.." $LANG_RECON$: $LANG_ILLUMINATIONAFFIRM$", "RadioSupportIlluminationBomb", briefingRoom.radioManager.getAnswerDelay(), briefingRoom.mission.objectiveFeaturesCommon.targetDesignationIlluminationBombDoBomb, args)
  end
end

function briefingRoom.mission.objectiveFeaturesCommon.registerTargetDesignationIlluminationBomb(objectiveIndex, innacurate)
  briefingRoom.mission.objectiveFeatures[objectiveIndex].illuminationBombInnacurate = innacurate or false 
  -- Number of bombs available
  briefingRoom.mission.objectiveFeatures[objectiveIndex].targetDesignationIlluminationBombBombsLeft = 4
   
-- Add the command to the F10 menu
table.insert(briefingRoom.mission.objectives[objectiveIndex].f10Commands, {text = "$LANG_ILLUMINATIONMENU$", func = briefingRoom.mission.objectiveFeaturesCommon.targetDesignationIlluminationBomb, args =  {objectiveIndex}})

end