-- ===================================================================================
-- 1.1 - CONSTANTS AND INITIALIZATION
-- ===================================================================================

DEGREES_TO_RADIANS = 0.0174533 -- multiply by this constant to convert degrees to radians
LASER_CODE = 1688 -- laser code to use for AI target designation
METERS_TO_NM = 0.000539957 -- number of nautical miles in a meter
NM_TO_METERS = 1852.0 -- number of meters in a nautical mile
SMOKE_DURATION = 300 -- smoke markers last for 5 minutes (300 seconds) in DCS World

briefingRoom = {} -- Main BriefingRoom table
briefingRoom.playerPilotNames = { $SCRIPTCLIENTPILOTNAMES$ }
briefingRoom.playerCoalition = $LUAPLAYERCOALITION$
briefingRoom.enemyCoalition = $LUAENEMYCOALITION$

-- Debug logging function
briefingRoom.printDebugMessages = false -- Disable debug messages logging, can be enabled later through mission features
function briefingRoom.debugPrint(message, duration)
  if not briefingRoom.printDebugMessages then return end -- Do not print debug messages if not in debug mode

  message = message or ""
  message = "BRIEFINGROOM: "..tostring(message)
  duration = duration or 3

  trigger.action.outText(message, duration, false)
  env.info(message, false)
end