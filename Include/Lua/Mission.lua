mission = 
{
    ["trig"] = 
    {
        ["actions"] = 
        {
            [1] = [[
                $TRIGSCRIPTACTIONS$
                ]],
            [2] = "a_end_mission(\"$LUAPLAYERCOALITION$\", \"\", 0); mission.trig.func[2]=nil;",
            [3] = "a_end_mission(\"$LUAPLAYERCOALITION$\", getValueDictByKey(\"\"), 0); mission.trig.func[3]=nil;",
            $TRIGACTIONS$
        },
        ["events"] = 
        {
        },
        ["custom"] = 
        {
        },
        ["func"] = 
        {
            [2] = "if mission.trig.conditions[2]() then mission.trig.actions[2]() end",
            $TRIGFUNCS$
        },
        ["flag"] = 
        {
            [1] = true,
            [2] = true,
            [3] = true,
            $TRIGFLAGS$
        },
        ["conditions"] = 
        {
            [1] = "return(true)",
            [2] = "return(c_flag_is_true(\"BR_END_MISSION\") )",
            [3] = "return(c_flag_is_true(\"BR_END_MISSION_NOW\") )",
            $TRIGCONDITIONS$
        },
        ["customStartup"] = 
        {
        },
        ["funcStartup"] = 
        {
            [1] = "if mission.trig.conditions[1]() then mission.trig.actions[1]() end",
        },
    },
    ["requiredModules"] = 
    {
        $REQUIREDMODULES$
    },
    ["date"] = 
    {
        ["Day"] = $DATEDAY$,
        ["Year"] = $DATEYEAR$,
        ["Month"] = $DATEMONTH$,
    },
    ["result"] = 
    {
        ["offline"] = 
        {
            ["conditions"] = 
            {
                [1] = "return(c_flag_is_true(\"BR_MISSION_COMPLETE\") )",
            },
            ["actions"] = 
            {
                [1] = "a_set_mission_result(100)",
            },
            ["func"] = 
            {
                [1] = "if mission.result.offline.conditions[1]() then mission.result.offline.actions[1]() end",
            },
        },
        ["total"] = 1,
        ["blue"] = 
        {
            ["conditions"] = 
            {
            },
            ["actions"] = 
            {
            },
            ["func"] = 
            {
            },
        },
        ["red"] = 
        {
            ["conditions"] = 
            {
            },
            ["actions"] = 
            {
            },
            ["func"] = 
            {
            },
        },
    },
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
            },
            ["instructor"] = 
            {
                ["neutrals"] = 0,
                ["blue"] = 0,
                ["red"] = 0,
            },
            ["observer"] = 
            {
                ["neutrals"] = 0,
                ["blue"] = 0,
                ["red"] = 0,
            },
            ["forward_observer"] = 
            {
                ["neutrals"] = 0,
                ["blue"] = $CAJTACBLU$,
                ["red"] = $CAJTACRED$,
            },
        },
    },
    ["maxDictId"] = 0,
    ["pictureFileNameN"] = 
    {
    },
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
                },
                ["Instructor"] = 
                {
                    ["Neutral"] = false,
                    ["Blue"] = false,
                    ["Red"] = false,
                },
                ["ForwardObserver"] = 
                {
                    ["Neutral"] = false,
                    ["Blue"] = false,
                    ["Red"] = false,
                },
                ["Spectrator"] = 
                {
                    ["Neutral"] = false,
                    ["Blue"] = false,
                    ["Red"] = false,
                },
                ["ArtilleryCommander"] = 
                {
                    ["Neutral"] = false,
                    ["Blue"] = false,
                    ["Red"] = false,
                },
                ["Pilot"] = 
                {
                    ["Neutral"] = false,
                    ["Blue"] = false,
                    ["Red"] = false,
                },
            },
        },
        ["layers"] = 
        {
            [1] = 
            {
                ["visible"] = true,
                ["name"] = "Red",
                ["objects"] = 
                {
                },
            },
            [2] = 
            {
                ["visible"] = true,
                ["name"] = "Blue",
                ["objects"] = 
                {
                },
            },
            [3] = 
            {
                ["visible"] = true,
                ["name"] = "Neutral",
                ["objects"] = 
                {
                },
            },
            [4] = 
            {
                ["visible"] = true,
                ["name"] = "Common",
                ["objects"] = 
                {
                    $DRAWINGS$
                },
            },
            [5] = 
            {
                ["visible"] = true,
                ["name"] = "Author",
                ["objects"] = 
                {
                },
            },
        },
    },
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
                },
            },
            ["side"] = "OFFLINE",
            ["score"] = 100,
            ["predicate"] = "score",
            ["comment"] = "MissionComplete",
        },
    },
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
            },
            ["at2000"] = 
            {
                ["speed"] = $WEATHERWINDSPEED2$,
                ["dir"] = $WEATHERWINDDIRECTION2$,
            },
            ["atGround"] = 
            {
                ["speed"] = $WEATHERWINDSPEED1$,
                ["dir"] = $WEATHERWINDDIRECTION1$,
            },
        },
        ["enable_fog"] = $WEATHERFOG$,
        ["groundTurbulence"] = $WEATHERGROUNDTURBULENCE$,
        ["halo"] = 
        {
            ["preset"] = "auto",
        },
        ["enable_dust"] = $WEATHERDUST$,
        ["season"] = 
        {
            ["temperature"] = $WEATHERTEMPERATURE$,
        },
        ["type_weather"] = 0,
        ["modifiedTime"] = false,
        ["cyclones"] = 
        {
        },
        ["name"] = "Default",
        ["fog"] = 
        {
            ["thickness"] = $WEATHERFOGTHICKNESS$,
            ["visibility"] = $WEATHERFOGVISIBILITY$,
        },
        ["fog2"] = 
		{
			["mode"] = 2,
		},
        ["dust_density"] = $WEATHERDUSTDENSITY$,
        ["qnh"] = $WEATHERQNH$,
        ["visibility"] = 
        {
            ["distance"] = $WEATHERVISIBILITY$,
        },
        ["clouds"] = 
        {
            ["thickness"] = $WEATHERCLOUDSTHICKNESS$,
            ["density"] = 0,
            ["preset"] = "$WEATHERCLOUDSPRESET$",
            ["base"] = $WEATHERCLOUDSBASE$,
            ["iprecptns"] = 0,
        },
    },
    ["theatre"] = "$THEATERID$",
    ["triggers"] = 
    {
        ["zones"] = 
        {
            $ZONES$
        },
    },
    ["map"] = 
    {
        ["centerY"] = $MISSIONAIRBASEY$,
        ["zoom"] = 512000.000,
        ["centerX"] = $MISSIONAIRBASEX$,
    },
    ["coalitions"] = 
    {
        ["neutrals"] = $COALITIONNEUTRAL$,
        ["blue"] = $COALITIONBLUE$,
        ["red"] = $COALITIONRED$
    },
    ["descriptionText"] = "DictKey_briefing",
    ["pictureFileNameR"] = 
    {
    },
    ["descriptionBlueTask"] = "DictKey_descriptionBlueTask_3",
    ["descriptionRedTask"] = "DictKey_descriptionRedTask_2",
    ["pictureFileNameB"] = 
    {
      [1] = "ResKey_TitleImage_$MISSIONID$",
    },
    ["coalition"] = 
    {
        ["neutrals"] = 
        {
            ["bullseye"] = 
            {
                ["y"] = 0,
                ["x"] = 0,
            },
            ["nav_points"] = 
            {
            },
            ["name"] = "neutrals",
            ["country"] = 
            {
                $COUNTRIESNEUTRAL$
            },
        },
        ["blue"] = 
        {
            ["bullseye"] = 
            {
                ["y"] = $BULLSEYEBLUEY$,
                ["x"] = $BULLSEYEBLUEX$,
            },
            ["nav_points"] = 
            {
            },
            ["name"] = "blue",
            ["country"] = 
            {
                $COUNTRIESBLUE$
            },
        },
        ["red"] = 
        {
            ["bullseye"] = 
            {
                ["y"] = $BULLSEYEREDY$,
                ["x"] = $BULLSEYEREDX$,
            },
            ["nav_points"] = 
            {
            },
            ["name"] = "red",
            ["country"] = 
            {
                $COUNTRIESRED$
            },
        },
    },
    ["sortie"] = "DictKey_missionName",
    ["version"] = 19,
    ["trigrules"] = 
    {
        [1] = 
        {
            ["rules"] = 
            {
            },
            ["eventlist"] = "",
            ["predicate"] = "triggerStart",
            ["actions"] = 
            {
                $TRIGSCRIPTRULES$
            },
            ["comment"] = "Run main mission script",
        },
        [2] = 
        {
            ["rules"] = 
            {
                [1] = 
                {
                    ["flag"] = "BR_END_MISSION",
                    ["predicate"] = "c_flag_is_true",
                    ["zone"] = "",
                },
            },
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
                },
            },
        },
        [3] = 
        {
            ["rules"] = 
            {
                [1] = 
                {
                    ["flag"] = "BR_END_MISSION_NOW",
                    ["predicate"] = "c_flag_is_true",
                    ["zone"] = "",
                },
            },
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
                },
            },
        },
        $TRIGRULES$
    },
    ["currentKey"] = 0,
    ["start_time"] = $STARTTIME$,
    ["forcedOptions"] = 
    {
        ["unrestrictedSATNAV"] = true,
        ["userMarks"] = true,
$FORCEDOPTIONS$
    },
    ["failures"] = 
    {
    },
} 
