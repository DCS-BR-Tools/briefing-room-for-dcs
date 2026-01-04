briefingRoom.mission.missionFeatures.friendlyTaskableBomber = {}
briefingRoom.mission.missionFeatures.friendlyTaskableBomber.MARKER_NAME = "bomber"
briefingRoom.mission.missionFeatures.friendlyTaskableBomber.LANG_UNIT = "$LANG_BOMBER$"
briefingRoom.mission.missionFeatures.friendlyTaskableBomber.LANG_REQEUEST = "$LANG_BOMBERREQUEST$"
briefingRoom.mission.missionFeatures.friendlyTaskableBomber.RADIO_REQEUEST = "RadioPilotBeginYourBombingRun"
briefingRoom.mission.missionFeatures.friendlyTaskableBomber.LANG_AFFIRM = "$LANG_BOMBERAFFIRM$"
briefingRoom.mission.missionFeatures.friendlyTaskableBomber.RADIO_AFFIRM = "RadioOtherPilotBeginBombing"
briefingRoom.mission.missionFeatures.friendlyTaskableBomber.LANG_NO_COORDS = "$LANG_BOMBERNOCOORIDNATES$"
briefingRoom.mission.missionFeatures.friendlyTaskableBomber.markID = nil -- ID of the mark on the map
briefingRoom.mission.missionFeatures.friendlyTaskableBomber.disableCooRemovedRadioMessage = false


-- Radio command to launch bombing run (called from F10 menu)
function briefingRoom.mission.missionFeatures.friendlyTaskableBomber.launchBombingRun()
  briefingRoom.taskables.launchCurry(briefingRoom.mission.missionFeatures.friendlyTaskableBomber,
    briefingRoom.mission.missionFeatures.groupNames.friendlyTaskableBomber, function(group, mark)
      local currPos = mist.getLeadPos(group)
      return {
        id = 'Mission',
        airborne = true,
        params = {
          route = {
            points = {
              [1] = {
                speed = 260,
                x = dcsExtensions.lerp(currPos.x, mark.pos.x, 0.1),
                y = dcsExtensions.lerp(currPos.z, mark.pos.z, 0.1),
                type = 'Turning Point',
                ETA_locked = false,
                ETA = 100,
                alt = 9144,
                alt_type = "BARO",
                speed_locked = false,
                action = "Turning Point",
                name = " ",
                task = {
                  id = "ComboTask",
                  params = {
                    tasks = {
                    },
                  },
                },
              },
              [2] = {
                speed = 260,
                x = dcsExtensions.lerp(currPos.x, mark.pos.x, 0.6),
                y = dcsExtensions.lerp(currPos.z, mark.pos.z, 0.6),
                type = 'Turning Point',
                ETA_locked = false,
                ETA = 100,
                alt = 9144,
                alt_type = "BARO",
                speed_locked = false,
                action = "Turning Point",
                name = "INGRESS",
                task = {
                  id = "ComboTask",
                  params = {
                    tasks = {
                      [1] = {
                        enabled = true,
                        auto = false,
                        id = "WrappedAction",
                        number = 1,
                        params = {
                          action =
                          {
                            id = "Option",
                            params =
                            {
                              value = 2,
                              name = 13,
                            },
                          },
                        },
                      },
                      [2] = {
                        enabled = true,
                        auto = false,
                        id = "WrappedAction",
                        number = 2,
                        params = {
                          action =
                          {
                            id = "Option",
                            params =
                            {
                              value = 2,
                              name = 1,
                            },
                          },
                        },
                      },
                      [3] = {
                        enabled = true,
                        auto = false,
                        id = "WrappedAction",
                        number = 3,
                        params = {
                          action =
                          {
                            id = "Option",
                            params =
                            {
                              value = 1,
                              name = 4,
                            },
                          },
                        },
                      },
                    },
                  },
                },
              },
              [3] = {
                speed = 320,
                x = dcsExtensions.lerp(currPos.x, mark.pos.x, 0.7),
                y = dcsExtensions.lerp(currPos.z, mark.pos.z, 0.7),
                type = 'Turning Point',
                ETA_locked = false,
                ETA = 100,
                alt = 9144,
                alt_type = "BARO",
                speed_locked = false,
                action = "Turning Point",
                name = "BOMB",
                task = {
                  id = "ComboTask",
                  params = {
                    tasks = {
                      [1] = {
                        enabled = true,
                        auto = false,
                        id = "Bombing",
                        number = 1,
                        params = {
                          direction = 0,
                          attackQtyLimit = false,
                          attackQty = 1,
                          expend = "Auto",
                          directionEnabled = false,
                          groupAttack = true,
                          altitude = 2000,
                          altitudeEnabled = false,
                          weaponType = 9663676414,
                          y = mark.pos.z,
                          x = mark.pos.x,
                        },
                      },
                    },
                  },
                },
              },
              [4] = {
                speed = 200,
                x = dcsExtensions.lerp(currPos.x, mark.pos.x, 0.5),
                y = dcsExtensions.lerp(currPos.z, mark.pos.z, 0.5),
                type = 'Turning Point',
                ETA_locked = false,
                ETA = 100,
                alt = 9144,
                alt_type = "BARO",
                speed_locked = false,
                action = "Turning Point",
                name = "ORBIT",
                task = {
                  id = "ComboTask",
                  params = {
                    tasks = {
                      [1] = {
                        enabled = true,
                        auto = false,
                        id = "Orbit",
                        number = 1,
                        params = {
                          pattern = "Circle",
                        },
                      },
                    },
                  },
                },
              },
            },
          },
        },
      }
    end)
end

-- Add F10 menu command
missionCommands.addCommandForCoalition(briefingRoom.playerCoalition, "$LANG_BOMBERMENU$",
  briefingRoom.f10Menu.missionMenu, briefingRoom.mission.missionFeatures.friendlyTaskableBomber.launchBombingRun, nil)

-- Enable event handler
world.addEventHandler(briefingRoom.taskables.markerManager(briefingRoom.mission.missionFeatures.friendlyTaskableBomber))
