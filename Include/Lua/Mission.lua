mission = 
{
    ["trig"] = 
    {
        ["actions"] = 
        {
            [1] = [[
                a_do_script_file(getValueResourceByKey(\"ResKey_MIST\"));
                a_do_script_file(getValueResourceByKey(\"ResKey_LuaExtensions\"));
                a_do_script_file(getValueResourceByKey(\"ResKey_DCSExtensions\"));
                a_do_script_file(getValueResourceByKey(\"ResKey_Init\"));
                a_do_script_file(getValueResourceByKey(\"ResKey_RadioManager\"));
                a_do_script_file(getValueResourceByKey(\"ResKey_AircraftActivator\"));
                a_do_script_file(getValueResourceByKey(\"ResKey_TransportManager\"));
                a_do_script_file(getValueResourceByKey(\"ResKey_EventHandler\"));
                a_do_script_file(getValueResourceByKey(\"ResKey_Script\"));
                a_do_script_file(getValueResourceByKey(\"ResKey_Start\"));
                ]],
            [2] = "a_end_mission(\"$LUAPLAYERCOALITION$\", \"\", 0); mission.trig.func[2]=nil;",
            [3] = "a_end_mission(\"$LUAPLAYERCOALITION$\", getValueDictByKey(\"\"), 0); mission.trig.func[3]=nil;",
            $TRIGACTIONS$
        }, -- end of ["actions"]
        ["events"] = 
        {
        }, -- end of ["events"]
        ["custom"] = 
        {
        }, -- end of ["custom"]
        ["func"] = 
        {
            [2] = "if mission.trig.conditions[2]() then mission.trig.actions[2]() end",
            $TRIGFUNCS$
        }, -- end of ["func"]
        ["flag"] = 
        {
            [1] = true,
            [2] = true,
            [3] = true,
            $TRIGFLAGS$
        }, -- end of ["flag"]
        ["conditions"] = 
        {
            [1] = "return(true)",
            [2] = "return(c_flag_is_true(\"BR_END_MISSION\") )",
            [3] = "return(c_flag_is_true(\"BR_END_MISSION_NOW\") )",
            $TRIGCONDITIONS$
        }, -- end of ["conditions"]
        ["customStartup"] = 
        {
        }, -- end of ["customStartup"]
        ["funcStartup"] = 
        {
            [1] = "if mission.trig.conditions[1]() then mission.trig.actions[1]() end",
        }, -- end of ["funcStartup"]
    }, -- end of ["trig"]
    ["requiredModules"] = 
    {
        $REQUIREDMODULES$
    }, -- end of ["requiredModules"]
    ["date"] = 
    {
        ["Day"] = $DATEDAY$,
        ["Year"] = $DATEYEAR$,
        ["Month"] = $DATEMONTH$,
    }, -- end of ["date"]
    ["result"] = 
    {
        ["offline"] = 
        {
            ["conditions"] = 
            {
                [1] = "return(c_flag_is_true(\"BR_MISSION_COMPLETE\") )",
            }, -- end of ["conditions"]
            ["actions"] = 
            {
                [1] = "a_set_mission_result(100)",
            }, -- end of ["actions"]
            ["func"] = 
            {
                [1] = "if mission.result.offline.conditions[1]() then mission.result.offline.actions[1]() end",
            }, -- end of ["func"]
        }, -- end of ["offline"]
        ["total"] = 1,
        ["blue"] = 
        {
            ["conditions"] = 
            {
            }, -- end of ["conditions"]
            ["actions"] = 
            {
            }, -- end of ["actions"]
            ["func"] = 
            {
            }, -- end of ["func"]
        }, -- end of ["blue"]
        ["red"] = 
        {
            ["conditions"] = 
            {
            }, -- end of ["conditions"]
            ["actions"] = 
            {
            }, -- end of ["actions"]
            ["func"] = 
            {
            }, -- end of ["func"]
        }, -- end of ["red"]
    }, -- end of ["result"]
    ["groundControl"] = 
    {
        ["isPilotControlVehicles"] = $CAPILOTCONTROL$,
        ["roles"] = 
        {
            ["artillery_commander"] = 
            {
                ["neutrals"] = 0,
                ["blue"] = $CACMDBLU$,
                ["red"] = $CACMDRED$,
            }, -- end of ["artillery_commander"]
            ["instructor"] = 
            {
                ["neutrals"] = 0,
                ["blue"] = 0,
                ["red"] = 0,
            }, -- end of ["instructor"]
            ["observer"] = 
            {
                ["neutrals"] = 0,
                ["blue"] = 0,
                ["red"] = 0,
            }, -- end of ["observer"]
            ["forward_observer"] = 
            {
                ["neutrals"] = 0,
                ["blue"] = $CAJTACBLU$,
                ["red"] = $CAJTACRED$,
            }, -- end of ["forward_observer"]
        }, -- end of ["roles"]
    }, -- end of ["groundControl"]
    ["maxDictId"] = 0,
    ["pictureFileNameN"] = 
    {
    }, -- end of ["pictureFileNameN"]
    ["drawings"] = 
    {
        ["options"] = 
        {
            ["hiddenOnF10Map"] = 
            {
                ["Observer"] = 
                {
                    ["Neutral"] = false,
                    ["Blue"] = false,
                    ["Red"] = false,
                }, -- end of ["Observer"]
                ["Instructor"] = 
                {
                    ["Neutral"] = false,
                    ["Blue"] = false,
                    ["Red"] = false,
                }, -- end of ["Instructor"]
                ["ForwardObserver"] = 
                {
                    ["Neutral"] = false,
                    ["Blue"] = false,
                    ["Red"] = false,
                }, -- end of ["ForwardObserver"]
                ["Spectrator"] = 
                {
                    ["Neutral"] = false,
                    ["Blue"] = false,
                    ["Red"] = false,
                }, -- end of ["Spectrator"]
                ["ArtilleryCommander"] = 
                {
                    ["Neutral"] = false,
                    ["Blue"] = false,
                    ["Red"] = false,
                }, -- end of ["ArtilleryCommander"]
                ["Pilot"] = 
                {
                    ["Neutral"] = false,
                    ["Blue"] = false,
                    ["Red"] = false,
                }, -- end of ["Pilot"]
            }, -- end of ["hiddenOnF10Map"]
        }, -- end of ["options"]
        ["layers"] = 
        {
            [1] = 
            {
                ["visible"] = true,
                ["name"] = "Red",
                ["objects"] = 
                {
                }, -- end of ["objects"]
            }, -- end of [1]
            [2] = 
            {
                ["visible"] = true,
                ["name"] = "Blue",
                ["objects"] = 
                {
                }, -- end of ["objects"]
            }, -- end of [2]
            [3] = 
            {
                ["visible"] = true,
                ["name"] = "Neutral",
                ["objects"] = 
                {
                }, -- end of ["objects"]
            }, -- end of [3]
            [4] = 
            {
                ["visible"] = true,
                ["name"] = "Common",
                ["objects"] = 
                {
                    $DRAWINGS$
                }, -- end of ["objects"]
            }, -- end of [4]
            [5] = 
            {
                ["visible"] = true,
                ["name"] = "Author",
                ["objects"] = 
                {
                }, -- end of ["objects"]
            }, -- end of [5]
        }, -- end of ["layers"]
    }, -- end of ["drawings"]
    ["goals"] = 
    {
        [1] = 
        {
            ["rules"] = 
            {
                [1] = 
                {
                    ["flag"] = "BR_MISSION_COMPLETE",
                    ["predicate"] = "c_flag_is_true",
                    ["zone"] = "",
                }, -- end of [1]
            }, -- end of ["rules"]
            ["side"] = "OFFLINE",
            ["score"] = 100,
            ["predicate"] = "score",
            ["comment"] = "MissionComplete",
        }, -- end of [1]
    }, -- end of ["goals"]
    ["descriptionNeutralsTask"] = "DictKey_editorNotes",
    ["weather"] = 
    {
        ["atmosphere_type"] = 0,
        ["wind"] = 
        {
            ["at8000"] = 
            {
                ["speed"] = $WEATHERWINDSPEED3$,
                ["dir"] = $WEATHERWINDDIRECTION3$,
            }, -- end of ["at8000"]
            ["at2000"] = 
            {
                ["speed"] = $WEATHERWINDSPEED2$,
                ["dir"] = $WEATHERWINDDIRECTION2$,
            }, -- end of ["at2000"]
            ["atGround"] = 
            {
                ["speed"] = $WEATHERWINDSPEED1$,
                ["dir"] = $WEATHERWINDDIRECTION1$,
            }, -- end of ["atGround"]
        }, -- end of ["wind"]
        ["enable_fog"] = $WEATHERFOG$,
        ["groundTurbulence"] = $WEATHERGROUNDTURBULENCE$,
        ["halo"] = 
        {
            ["preset"] = "auto",
        }, -- end of ["halo"]
        ["enable_dust"] = $WEATHERDUST$,
        ["season"] = 
        {
            ["temperature"] = $WEATHERTEMPERATURE$,
        }, -- end of ["season"]
        ["type_weather"] = 0,
        ["modifiedTime"] = false,
        ["cyclones"] = 
        {
        }, -- end of ["cyclones"]
        ["name"] = "Default",
        ["fog"] = 
        {
            ["thickness"] = $WEATHERFOGTHICKNESS$,
            ["visibility"] = $WEATHERFOGVISIBILITY$,
        }, -- end of ["fog"]
        ["fog2"] = 
		{
			["mode"] = 2,
		}, -- end of ["fog2"]
        ["dust_density"] = $WEATHERDUSTDENSITY$,
        ["qnh"] = $WEATHERQNH$,
        ["visibility"] = 
        {
            ["distance"] = $WEATHERVISIBILITY$,
        }, -- end of ["visibility"]
        ["clouds"] = 
        {
            ["thickness"] = $WEATHERCLOUDSTHICKNESS$,
            ["density"] = 0,
            ["preset"] = "$WEATHERCLOUDSPRESET$",
            ["base"] = $WEATHERCLOUDSBASE$,
            ["iprecptns"] = 0,
        }, -- end of ["clouds"]
    }, -- end of ["weather"]
    ["theatre"] = "$THEATERID$",
    ["triggers"] = 
    {
        ["zones"] = 
        {
            $ZONES$
        }, -- end of ["zones"]
    }, -- end of ["triggers"]
    ["map"] = 
    {
        ["centerY"] = $MISSIONAIRBASEY$,
        ["zoom"] = 512000.000,
        ["centerX"] = $MISSIONAIRBASEX$,
    }, -- end of ["map"]
    ["coalitions"] = 
    {
        ["neutrals"] = $COALITIONNEUTRAL$,
        ["blue"] = $COALITIONBLUE$,
        ["red"] = $COALITIONRED$
    }, -- end of ["coalitions"]
    ["descriptionText"] = "DictKey_briefing",
    ["pictureFileNameR"] = 
    {
    }, -- end of ["pictureFileNameR"]
    ["descriptionBlueTask"] = "DictKey_descriptionBlueTask_3",
    ["descriptionRedTask"] = "DictKey_descriptionRedTask_2",
    ["pictureFileNameB"] = 
    {
      [1] = "ResKey_TitleImage_$MISSIONID$",
    }, -- end of ["pictureFileNameB"]
    ["coalition"] = 
    {
        ["neutrals"] = 
        {
            ["bullseye"] = 
            {
                ["y"] = 0,
                ["x"] = 0,
            }, -- end of ["bullseye"]
            ["nav_points"] = 
            {
            }, -- end of ["nav_points"]
            ["name"] = "neutrals",
            ["country"] = 
            {
                $COUNTRIESNEUTRAL$
            }, -- end of ["country"]
        }, -- end of ["neutrals"]
        ["blue"] = 
        {
            ["bullseye"] = 
            {
                ["y"] = $BULLSEYEBLUEY$,
                ["x"] = $BULLSEYEBLUEX$,
            }, -- end of ["bullseye"]
            ["nav_points"] = 
            {
            }, -- end of ["nav_points"]
            ["name"] = "blue",
            ["country"] = 
            {
                $COUNTRIESBLUE$
            }, -- end of ["country"]
        }, -- end of ["blue"]
        ["red"] = 
        {
            ["bullseye"] = 
            {
                ["y"] = $BULLSEYEREDY$,
                ["x"] = $BULLSEYEREDX$,
            }, -- end of ["bullseye"]
            ["nav_points"] = 
            {
            }, -- end of ["nav_points"]
            ["name"] = "red",
            ["country"] = 
            {
                $COUNTRIESRED$
            }, -- end of ["country"]
        }, -- end of ["red"]
    }, -- end of ["coalition"]
    ["sortie"] = "DictKey_missionName",
    ["version"] = 19,
    ["trigrules"] = 
    {
        [1] = 
        {
            ["rules"] = 
            {
            }, -- end of ["rules"]
            ["eventlist"] = "",
            ["predicate"] = "triggerStart",
            ["actions"] = 
            {
                [1] = 
                {
                    ["file"] = "ResKey_MIST",
                    ["predicate"] = "a_do_script_file",
                },
                [2] = 
                {
                    ["file"] = "ResKey_LuaExtensions",
                    ["predicate"] = "a_do_script_file",
                },
                [3] = 
                {
                    ["file"] = "ResKey_DCSExtensions",
                    ["predicate"] = "a_do_script_file",
                },
                [4] = 
                {
                    ["file"] = "ResKey_Init",
                    ["predicate"] = "a_do_script_file",
                },
                [5] = 
                {
                    ["file"] = "ResKey_RadioManager",
                    ["predicate"] = "a_do_script_file",
                },
                [6] = 
                {
                    ["file"] = "ResKey_AircraftActivator",
                    ["predicate"] = "a_do_script_file",
                },
                [7] = 
                {
                    ["file"] = "ResKey_TransportManager",
                    ["predicate"] = "a_do_script_file",
                },
                [8] = 
                {
                    ["file"] = "ResKey_EventHandler",
                    ["predicate"] = "a_do_script_file",
                },
                [9] = 
                {
                    ["file"] = "ResKey_Script",
                    ["predicate"] = "a_do_script_file",
                },
                [10] = 
                {
                    ["file"] = "ResKey_Start",
                    ["predicate"] = "a_do_script_file",
                },
            }, -- end of ["actions"]
            ["comment"] = "Run main mission script",
        }, -- end of [1]
        [2] = 
        {
            ["rules"] = 
            {
                [1] = 
                {
                    ["flag"] = "BR_END_MISSION",
                    ["predicate"] = "c_flag_is_true",
                    ["zone"] = "",
                }, -- end of [1]
            }, -- end of ["rules"]
            ["comment"] = "Ends mission when triggered by flag",
            ["eventlist"] = "",
            ["predicate"] = "triggerOnce",
            ["actions"] = 
            {
                [1] = 
                {
                    ["text"] = "",
                    ["start_delay"] = 600,
                    ["zone"] = "",
                    ["predicate"] = "a_end_mission",
                    ["winner"] = "$LUAPLAYERCOALITION$",
                    ["KeyDict_text"] = "",
                }, -- end of [1]
            }, -- end of ["actions"]
        }, -- end of [2]
        [3] = 
        {
            ["rules"] = 
            {
                [1] = 
                {
                    ["flag"] = "BR_END_MISSION_NOW",
                    ["predicate"] = "c_flag_is_true",
                    ["zone"] = "",
                }, -- end of [1]
            }, -- end of ["rules"]
            ["comment"] = "Ends mission when triggered by flag",
            ["eventlist"] = "",
            ["predicate"] = "triggerOnce",
            ["actions"] = 
            {
                [1] = 
                {
                    ["text"] = "",
                    ["start_delay"] = 0,
                    ["zone"] = "",
                    ["predicate"] = "a_end_mission",
                    ["winner"] = "$LUAPLAYERCOALITION$",
                    ["KeyDict_text"] = "",
                }, -- end of [1]
            }, -- end of ["actions"]
        }, -- end of [3]
        $TRIGRULES$
    }, -- end of ["trigrules"]
    ["currentKey"] = 0,
    ["start_time"] = $STARTTIME$,
    ["forcedOptions"] = 
    {
        ["unrestrictedSATNAV"] = true,
        ["userMarks"] = true,
$FORCEDOPTIONS$
    }, -- end of ["forcedOptions"]
    ["failures"] = 
    {
    }, -- end of ["failures"]
} -- end of mission
