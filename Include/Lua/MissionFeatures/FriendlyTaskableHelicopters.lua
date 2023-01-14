briefingRoom.mission.missionFeatures.friendlyTaskableHelicopters = {}
briefingRoom.mission.missionFeatures.friendlyTaskableHelicopters.MARKER_NAME = "helo"
briefingRoom.mission.missionFeatures.friendlyTaskableHelicopters.LANG_UNIT = "$LANG_ATTACKCHOPPERS$"
briefingRoom.mission.missionFeatures.friendlyTaskableHelicopters.LANG_REQEUEST = "$LANG_ATTACKCHOPPERSREQUEST$"
briefingRoom.mission.missionFeatures.friendlyTaskableHelicopters.RADIO_REQEUEST = "RadioPilotBeginYourAttack"
briefingRoom.mission.missionFeatures.friendlyTaskableHelicopters.LANG_AFFIRM = "$LANG_BEGINATTACK$"
briefingRoom.mission.missionFeatures.friendlyTaskableHelicopters.RADIO_AFFIRM = "RadioOtherPilotBeginAttack"
briefingRoom.mission.missionFeatures.friendlyTaskableHelicopters.LANG_NO_COORDS = "$LANG_ATTACKCHOPPERSNOCOORDINATES$"
briefingRoom.mission.missionFeatures.friendlyTaskableHelicopters.markID = nil -- ID of the mark on the map
briefingRoom.mission.missionFeatures.friendlyTaskableHelicopters.disableCooRemovedRadioMessage = false


-- Radio command to launch bombing run (called from F10 menu)
function briefingRoom.mission.missionFeatures.friendlyTaskableHelicopters.launchBombingRun()
  briefingRoom.taskables.launchCurry(briefingRoom.mission.missionFeatures.friendlyTaskableHelicopters,
    briefingRoom.mission.missionFeatures.groupNames.friendlyTaskableHelicopters, function(group, mark)
    local currPos = mist.getLeadPos(group)
    return {
      id = 'Mission',
      airborne = true,
      params = {
        route = {
          points = {
            [1] = {
              speed = 200,
              x = dcsExtensions.lerp(currPos.x, mark.pos.x, 0.1),
              y = dcsExtensions.lerp(currPos.z, mark.pos.z, 0.1),
              type = 'Turning Point',
              ETA_locked = false,
              ETA = 100,
              alt = 200,
              alt_type = "RADIO",
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
              speed = 230,
              x = dcsExtensions.lerp(currPos.x, mark.pos.x, 0.6),
              y = dcsExtensions.lerp(currPos.z, mark.pos.z, 0.6),
              type = 'Turning Point',
              ETA_locked = false,
              ETA = 100,
              alt = 200,
              alt_type = "RADIO",
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
                          }, -- end of ["params"]
                        }, -- end of ["action"]
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
                          }, -- end of ["params"]
                        }, -- end of ["action"]
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
                          }, -- end of ["params"]
                        }, -- end of ["action"]
                      },
                    },
                  },
                },
              },
            },
            [3] = {
              speed = 200,
              x = dcsExtensions.lerp(currPos.x, mark.pos.x, 0.85),
              y = dcsExtensions.lerp(currPos.z, mark.pos.z, 0.85),
              type = 'Turning Point',
              ETA_locked = false,
              ETA = 100,
              alt = 200,
              alt_type = "RADIO",
              speed_locked = false,
              action = "Turning Point",
              name = "CAS",
              task = {
                id = "ComboTask",
                params = {
                  tasks = {
                    [1] =
                    {
                      ["enabled"] = true,
                      ["key"] = "CAS",
                      ["id"] = "EngageTargets",
                      ["number"] = 1,
                      ["auto"] = true,
                      ["params"] =
                      {
                        ["targetTypes"] =
                        {
                          [1] = "Helicopters",
                          [2] = "Ground Units",
                          [3] = "Light armed ships",
                        }, -- end of ["targetTypes"]
                        ["priority"] = 0,
                      }, -- end of ["params"]
                    }, -- end of [1]
                    [2] = {
                      enabled = true,
                      auto = false,
                      id = "EngageTargetsInZone",
                      number = 2,
                      params = {
                        y = mark.pos.z,
                        x = mark.pos.x,
                        targetTypes = {
                          [1] = "Helicopters",
                          [2] = "Ground Units",
                          [3] = "Light armed ships",
                        },
                        value = "All;",
                        noTargetTypes = {
                        },
                        priority = 0,
                        zoneRadius = 5000,
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
              alt = 200,
              alt_type = "RADIO",
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
missionCommands.addCommandForCoalition(briefingRoom.playerCoalition, "$LANG_ATTACKCHOPPERMENU$",
  briefingRoom.f10Menu.missionMenu, briefingRoom.mission.missionFeatures.friendlyTaskableHelicopters.launchBombingRun,
  nil)


-- Enable event handler
world.addEventHandler(briefingRoom.taskables.markerManager(briefingRoom.mission.missionFeatures.friendlyTaskableHelicopters))
