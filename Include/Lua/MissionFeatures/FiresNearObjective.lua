-- Add fire and smoke near objective
for i=math.random(4, 15),1,-1 do
  local unitVec2 = { x = $MISSIONCENTERX$, y = $MISSIONCENTERY$ }
  unitVec2.x = unitVec2.x + math.random(-15000, 15000)
  unitVec2.y = unitVec2.y + math.random(-15000, 15000)
  local spawnPoint = { x = unitVec2.x, y = land.getHeight(unitVec2), z = unitVec2.y }
  trigger.action.effectSmokeBig(spawnPoint, math.random(1, 4), math.random(7, 10) * 0.1)
end
