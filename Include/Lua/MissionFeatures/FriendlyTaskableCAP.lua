briefingRoom.mission.missionFeatures.friendlyTaskableCAP = {}
briefingRoom.mission.missionFeatures.friendlyTaskableCAP.MARKER_NAME = "cap"
briefingRoom.mission.missionFeatures.friendlyTaskableCAP.LANG_UNIT = "$LANG_CAP$"
briefingRoom.mission.missionFeatures.friendlyTaskableCAP.LANG_REQEUEST = "$LANG_CAPREQUEST$"
briefingRoom.mission.missionFeatures.friendlyTaskableCAP.RADIO_REQEUEST = "RadioPilotCAPSupport"
briefingRoom.mission.missionFeatures.friendlyTaskableCAP.LANG_AFFIRM = "$LANG_CAPAFFIRM$"
briefingRoom.mission.missionFeatures.friendlyTaskableCAP.RADIO_AFFIRM = "RadioHQCAPSupport"
briefingRoom.mission.missionFeatures.friendlyTaskableCAP.LANG_NO_COORDS = "$LANG_CAPNOCOORDINATES$"
briefingRoom.mission.missionFeatures.friendlyTaskableCAP.markID = nil -- ID of the mark on the map
briefingRoom.mission.missionFeatures.friendlyTaskableCAP.disableCooRemovedRadioMessage = false


-- Radio command to launch bombing run (called from F10 menu)
function briefingRoom.mission.missionFeatures.friendlyTaskableCAP.launchBombingRun()
  briefingRoom.taskables.launchCurry(briefingRoom.mission.missionFeatures.friendlyTaskableCAP,
    briefingRoom.mission.missionFeatures.groupNames.friendlyTaskableCAP, function(group, mark)
    return {
      id = 'Mission',
      airborne = true,
      params = {
        route = {
          points = {
            [1] = {
              speed = 200,
              x = mark.pos.x,
              y = mark.pos.z,
              type = 'Turning Point',
              ETA_locked = false,
              ETA = 100,
              alt = 7620,
              alt_type = "BARO",
              speed_locked = false,
              action = "Fly Over Point",
              name = "CAP",
              task = {
                id = "ComboTask",
                params = {
                  tasks = {
                    [1] = {
                      enabled = true,
                      auto = false,
                      id = "EngageTargets",
                      number = 1,
                      params = {
                        targetTypes = {
                          [1] = "Air",
                        },
                        value = "Air;",
                        noTargetTypes = {
                        },
                        priority = 0,
                      },
                    },
                    [2] = {
                      enabled = true,
                      auto = false,
                      id = "Orbit",
                      number = 2,
                      params = {
                        pattern = "Circle",
                      }
                    }
                  }
                }
              }
            },
          }
        },
      },
    }
  end)
end

-- Add F10 menu command
missionCommands.addCommandForCoalition(briefingRoom.playerCoalition, "$LANG_CAPMENU$", briefingRoom.f10Menu.missionMenu,
  briefingRoom.mission.missionFeatures.friendlyTaskableCAP.launchBombingRun, nil)


-- Enable event handler
world.addEventHandler(briefingRoom.taskables.markerManager(briefingRoom.mission.missionFeatures.friendlyTaskableCAP))
