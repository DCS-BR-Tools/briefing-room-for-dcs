options = 
{
    ["playerName"] = "Player",
    ["miscellaneous"] = 
    {
        ["allow_server_screenshots"] = false,
        ["headmove"] = false,
        ["TrackIR_external_views"] = true,
        ["f5_nearest_ac"] = true,
        ["f11_free_camera"] = true,
        ["F2_view_effects"] = 1,
        ["f10_awacs"] = true,
        ["Coordinate_Display"] = "Lat Long",
        ["accidental_failures"] = false,
        ["autologin"] = false,
        ["force_feedback_enabled"] = true,
        ["collect_stat"] = true,
        ["chat_window_at_start"] = true,
        ["synchronize_controls"] = false,
        ["show_pilot_body"] = true,
    }, -- end of ["miscellaneous"]
    ["difficulty"] = 
    {
        ["fuel"] = false,
        ["easyRadar"] = false,
        ["miniHUD"] = false,
        ["optionsView"] = "optview_all",
        ["setGlobal"] = false,
        ["avionicsLanguage"] = "native",
        ["cockpitVisualRM"] = false,
        ["map"] = true,
        ["spectatorExternalViews"] = true,
        ["userSnapView"] = true,
        ["iconsTheme"] = "nato",
        ["weapons"] = false,
        ["padlock"] = true,
        ["birds"] = 0,
        ["permitCrash"] = true,
        ["immortal"] = false,
        ["cockpitStatusBarAllowed"] = false,
        ["wakeTurbulence"] = false,
        ["easyFlight"] = false,
        ["hideStick"] = false,
        ["radio"] = false,
        ["geffect"] = "realistic",
        ["easyCommunication"] = true,
        ["reports"] = true,
        ["units"] = "imperial",
        ["unrestrictedSATNAV"] = false,
        ["autoTrimmer"] = false,
        ["externalViews"] = true,
        ["controlsIndicator"] = true,
        ["RBDAI"] = true,
        ["tips"] = true,
        ["userMarks"] = true,
        ["labels"] = 1,
    }, -- end of ["difficulty"]
    ["VR"] = 
    {
        ["mirror_crop"] = false,
        ["enable"] = false,
        ["custom_IPD_enable"] = false,
        ["box_mouse_cursor"] = true,
        ["msaaMaskSize"] = 0.42,
        ["pixel_density"] = 1,
        ["use_mouse"] = false,
        ["mirror_use_DCS_resolution"] = false,
        ["prefer_built_in_audio"] = true,
        ["interaction_with_grip_only"] = false,
        ["bloom"] = true,
        ["mirror_source"] = 0,
        ["custom_IPD"] = 63.5,
        ["hand_controllers"] = false,
    }, -- end of ["VR"]
    ["graphics"] = 
    {
        ["messagesFontScale"] = 2,
        ["forestDetailsFactor"] = 1,
        ["rainDroplets"] = true,
        ["LensEffects"] = 0,
        ["box_mouse_cursor"] = false,
        ["anisotropy"] = 3,
        ["water"] = 2,
        ["motionBlur"] = 0,
        ["visibRange"] = "High",
        ["aspect"] = 1.7777777777778,
        ["lights"] = 2,
        ["shadows"] = 4,
        ["MSAA"] = 1,
        ["SSAA"] = 0,
        ["civTraffic"] = "high",
        ["forestDistanceFactor"] = 1,
        ["cockpitGI"] = 1,
        ["terrainTextures"] = "max",
        ["multiMonitorSetup"] = "1camera",
        ["shadowTree"] = false,
        ["chimneySmokeDensity"] = 5,
        ["fullScreen"] = true,
        ["DOF"] = 0,
        ["clouds"] = 2,
        ["sceneryDetailsFactor"] = 1,
        ["flatTerrainShadows"] = 0,
        ["useDeferredShading"] = 1,
        ["textures"] = 2,
        ["width"] = 2560,
        ["SSLR"] = 1,
        ["effects"] = 3,
        ["SSAO"] = 1,
        ["outputGamma"] = 2.2,
        ["sync"] = true,
        ["heatBlr"] = 1,
        ["preloadRadius"] = 60000,
        ["scaleGui"] = 1,
        ["height"] = 1440,
        ["clutterMaxDistance"] = 1000,
    }, -- end of ["graphics"]
    ["plugins"] = 
    {
        ["Su-25T"] = 
        {
            ["CPLocalList"] = "default",
        }, -- end of ["Su-25T"]
        ["M-2000C"] = 
        {
            ["FUEL_DETOT_AUTO"] = false,
            ["AOA_SHOWINHUD"] = false,
            ["TDC_GatePPI"] = 5,
            ["TDC_PPI_is_Polar"] = false,
            ["CPLocalList"] = "default",
            ["REFUEL_HACK"] = false,
            ["UNI_ALIGNED"] = false,
            ["UNI_NODRIFT"] = false,
            ["TDC_KBPrecission"] = 100,
        }, -- end of ["M-2000C"]
        ["A-10C_2"] = 
        {
            ["hmdEye"] = 1,
            ["CPLocalList"] = "aged",
            ["defaultGunMode"] = 0,
        }, -- end of ["A-10C_2"]
        ["FC3"] = 
        {
            ["CPLocalList_Su-25"] = "default",
            ["CPLocalList_Su-27"] = "default",
            ["CPLocalList_A-10A"] = "default",
            ["CPLocalList_Su-33"] = "default",
            ["CPLocalList_MiG-29S"] = "default",
            ["CPLocalList_MiG-29A"] = "default",
            ["CPLocalList_J-11A"] = "default",
            ["CPLocalList_MiG-29G"] = "default",
            ["CPLocalList_F-15C"] = "default",
        }, -- end of ["FC3"]
        ["F/A-18C"] = 
        {
            ["abDetent"] = 0,
            ["canopyReflections"] = 0,
            ["hmdEye"] = 2,
            ["CPLocalList"] = "default",
            ["F18RealisticTDC"] = false,
            ["mfdReflections"] = 1,
        }, -- end of ["F/A-18C"]
        ["Ka-50"] = 
        {
            ["Ka50TrimmingMethod"] = 0,
            ["CPLocalList"] = "default",
            ["Ka50RudderTrimmer"] = false,
            ["helmetCircleDisplacement"] = 11,
        }, -- end of ["Ka-50"]
        ["AV8BNA"] = 
        {
            ["INS_Alignment"] = 0,
            ["INS_GYROHasNAV"] = false,
            ["CPLocalList"] = "default",
            ["USE_REAL_TDC"] = false,
            ["MPCD_EXPORT"] = false,
        }, -- end of ["AV8BNA"]
        ["F-16C"] = 
        {
            ["abDetent"] = 0,
            ["canopyReflections"] = 0,
            ["hmdEye"] = 1,
            ["CPLocalList"] = "default",
            ["canopyTint"] = 1,
            ["mfdReflections"] = 0,
        }, -- end of ["F-16C"]
        ["TF-51D"] = 
        {
            ["assistance"] = 100,
            ["CPLocalList"] = "default",
            ["autoRudder"] = false,
        }, -- end of ["TF-51D"]
        ["VRFree"] = 
        {
            ["enable"] = false,
            ["set_debug"] = false,
            ["mouseClickSrc"] = 0,
        }, -- end of ["VRFree"]
        ["Supercarrier"] = 
        {
            ["enable_FLOLS_overlay"] = true,
            ["Use_native_ATC_text"] = false,
        }, -- end of ["Supercarrier"]
        ["CaptoGlove"] = 
        {
            ["shoulderJointZ_Right"] = 23,
            ["armBending"] = 60,
            ["shoulderJointX_Right"] = -15,
            ["mouseClickSrc"] = 0,
            ["shoulderJointZ_Left"] = 23,
            ["shoulderJointX_Left"] = -15,
            ["set_debug"] = false,
            ["pitchOffsetGlove_Left"] = 0,
            ["yawOffsetGlove_Left"] = 0,
            ["yawOffsetShoulder_Right"] = 0,
            ["useBending"] = false,
            ["shoulderLength_Right"] = 40,
            ["pitchOffsetGlove_Right"] = 0,
            ["shoulderJointY_Left"] = -23,
            ["forearmLength_Left"] = 30,
            ["shoulderLength_Left"] = 40,
            ["set_attach"] = false,
            ["pitchOffsetShoulder_Right"] = 0,
            ["forearmLength_Right"] = 30,
            ["pitchOffsetShoulder_Left"] = 0,
            ["set_symmetrically"] = false,
            ["yawOffsetShoulder_Left"] = 0,
            ["enable"] = false,
            ["shoulderJointY_Right"] = -23,
            ["yawOffsetGlove_Right"] = 0,
        }, -- end of ["CaptoGlove"]
        ["A-10C"] = 
        {
            ["CPLocalList"] = "default",
        }, -- end of ["A-10C"]
    }, -- end of ["plugins"]
    ["format"] = 1,
    ["sound"] = 
    {
        ["main_output"] = "",
        ["FakeAfterburner"] = true,
        ["volume"] = 63,
        ["headphones_on_external_views"] = true,
        ["subtitles"] = true,
        ["world"] = 100,
        ["hear_in_helmet"] = true,
        ["cockpit"] = 100,
        ["main_layout"] = "",
        ["hp_output"] = "",
        ["radioSpeech"] = true,
        ["voice_chat_output"] = "0:{0.0.0.00000000}.{1b144b90-ebf5-4192-9dd3-70311c058ea4}",
        ["voice_chat"] = false,
        ["microphone_use"] = 1,
        ["GBreathEffect"] = true,
        ["switches"] = 100,
        ["play_audio_while_minimized"] = false,
        ["headphones"] = 100,
        ["music"] = 0,
        ["voice_chat_input"] = "",
        ["gui"] = 100,
    }, -- end of ["sound"]
    ["views"] = 
    {
        ["cockpit"] = 
        {
            ["mirrors"] = false,
            ["reflections"] = false,
            ["avionics"] = 3,
        }, -- end of ["cockpit"]
    }, -- end of ["views"]
} -- end of options
