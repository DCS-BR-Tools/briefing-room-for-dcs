briefingRoom.mission.missionFeatures.friendlyTaskableSEAD = {}
briefingRoom.mission.missionFeatures.friendlyTaskableSEAD.MARKER_NAME = "sead"
briefingRoom.mission.missionFeatures.friendlyTaskableSEAD.LANG_UNIT = "$LANG_SEAD$"
briefingRoom.mission.missionFeatures.friendlyTaskableSEAD.LANG_REQEUEST = "$LANG_SEADREQUEST$"
briefingRoom.mission.missionFeatures.friendlyTaskableSEAD.RADIO_REQEUEST = "RadioPilotSEADSupport"
briefingRoom.mission.missionFeatures.friendlyTaskableSEAD.LANG_AFFIRM = "$LANG_SEADAFFIRM$"
briefingRoom.mission.missionFeatures.friendlyTaskableSEAD.RADIO_AFFIRM = "RadioHQSEADSupport"
briefingRoom.mission.missionFeatures.friendlyTaskableSEAD.LANG_NO_COORDS = "$LANG_SEADNOCOORDINATES$"
briefingRoom.mission.missionFeatures.friendlyTaskableSEAD.markID = nil -- ID of the mark on the map
briefingRoom.mission.missionFeatures.friendlyTaskableSEAD.disableCooRemovedRadioMessage = false


-- Radio command to launch bombing run (called from F10 menu)
function briefingRoom.mission.missionFeatures.friendlyTaskableSEAD.launchBombingRun()
  briefingRoom.taskables.launchCurry(briefingRoom.mission.missionFeatures.friendlyTaskableSEAD,
    briefingRoom.mission.missionFeatures.groupNames.friendlyTaskableSEAD, function(group, mark)
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
                alt = 6400,
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
                speed = 250,
                x = dcsExtensions.lerp(currPos.x, mark.pos.x, 0.6),
                y = dcsExtensions.lerp(currPos.z, mark.pos.z, 0.6),
                type = 'Turning Point',
                ETA_locked = false,
                ETA = 100,
                alt = 6400,
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
                speed = 250,
                x = dcsExtensions.lerp(currPos.x, mark.pos.x, 0.65),
                y = dcsExtensions.lerp(currPos.z, mark.pos.z, 0.65),
                type = 'Turning Point',
                ETA_locked = false,
                ETA = 100,
                alt = 6400,
                alt_type = "BARO",
                speed_locked = false,
                action = "Turning Point",
                name = "SEAD",
                task = {
                  id = "ComboTask",
                  params = {
                    tasks = {
                      [1] =
                      {
                        ["enabled"] = true,
                        ["key"] = "SEAD",
                        ["id"] = "EngageTargets",
                        ["number"] = 1,
                        ["auto"] = true,
                        ["params"] =
                        {
                          ["targetTypes"] =
                          {
                            [1] = "Air Defence",
                          },
                          ["priority"] = 0,
                        },
                      },
                      [2] = {
                        enabled = true,
                        auto = false,
                        id = "EngageTargetsInZone",
                        number = 2,
                        params = {
                          y = mark.pos.z,
                          x = mark.pos.x,
                          targetTypes = {
                            [1] = "Air Defence",
                          },
                          value = "All;",
                          noTargetTypes = {
                            [1] = "Helicopters",
                            [2] = "Infantry",
                            [3] = "Fortifications",
                            [4] = "Tanks",
                            [5] = "IFV",
                            [6] = "APC",
                            [7] = "Artillery",
                            [8] = "Unarmed vehicles",
                            [9] = "Light armed ships",
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
                alt = 6400,
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
missionCommands.addCommandForCoalition(briefingRoom.playerCoalition, "$LANG_SEADMENU$", briefingRoom.f10Menu.missionMenu
, briefingRoom.mission.missionFeatures.friendlyTaskableSEAD.launchBombingRun, nil)

-- Enable event handler
world.addEventHandler(briefingRoom.taskables.markerManager(briefingRoom.mission.missionFeatures.friendlyTaskableSEAD))
