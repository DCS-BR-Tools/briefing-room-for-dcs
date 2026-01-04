-- ===================================================================================
-- 3.8 - STARTUP
-- ===================================================================================

-- All done, enable event handler so the mission can begin
world.addEventHandler(briefingRoom.eventHandler)

for i=1,table.count(briefingRoom.mission.objectives) do
  table.insert(briefingRoom.mission.objectives[i].f10Commands, {text = "$LANG_WAYPOINTCOORDINATESREQUEST$", func = briefingRoom.f10MenuCommands.getWaypointCoordinates, args =  i})
  briefingRoom.mission.objectives[i].waypointRadioCommandIndex = table.count(briefingRoom.mission.objectives[i].f10Commands)

  if briefingRoom.mission.objectives[i].progressionHiddenBrief == false then
    briefingRoom.f10MenuCommands.activateObjective(i)
  end
end

if $INSTANTSTART$ then
  briefingRoom.mission.coreFunctions.beginMission()
end