--[[
    04 April 2025 (Stevey666) - 3.1
	  - Set default cluster munitions option to false, set this to true in the options if you want it
      - Added missing radio commands for Cascade Scaling
	  - Adjust default cascading to 2 (from 1)
	  - Adjusted Ural-4320 to be a tanker and ammo carrier for cargo cookoff
	  - Prevent weapons not in the list from being tracked
	  - Moved some logging behind the debug mode flag
	  - Ordnance Protection, added a max height ordnance protection will snap explosion to ground
	  - Ordnance Protection, fixed enable/disable option
	  - Added Giant Explosion feature
	  - Adjusted some hydra70 values on recom. from ETBSmorgan
	  
	  
    09 March 2025 (Stevey666) - 3.0
      - Added ordinance protection gives a few options - stop the additional larger_explosion that tends to blow up your own bombs if theyre dropped at the same place if its within x m
	  - Additional ordnance protection option that will cause a snap to ground larger_explosion if its within x meters of a recent larger explosion and within x seconds (can set in options)
      - Added vehicle scanning around a weapon to allow for..
	  - Cook offs - you can set vehicles that will cook off i.e ammo trucks, number of explosions, debris explosions, power adjustable
	  - Fuel/Tanker explosion and flames - when a fuel tanker blows it will through up a big flame - adjustable in the scripts
	  - Added section for vehicles for the above
	  - Added radio commands for everything
	  - Added in cluster munitions changes (note: barely tested, its not particularly accurate or that useful at this point so leaving disabled)
	  - Potential bug - testing, stacking too many units together may cause a MIST error if you're using mist
	  
	  - Setting this as 3.0 as I'd like to be responsive to requests, updates etc - creating a new fork to track this
	

    10 Feb 2025 (Stevey666) - 2.0.7
      - Fixed AGM 154/Adjusted weapons
      - Added overall damage scaling 
      - Added modifier for shaped charges (i.e. Mavericks), adjusted weapon list accordingly
      - Adjusted blast radius and damage calculations, created option for dynamic blast radius
      - Adjusted cascading explosions, added additional "cascade_scaling" modifier and cascade explode threshold modifier. Units wont explode on initial impact unless health drops under threshold
      - Added always_cascade_explode option so you can set it to the old ways of making everything in the blast wave go kaboom
      - Added in game radio commands to change the new options ingame without having to reload everything in mission editor to test it out

    12 November 2024 (by JGi | Quéton 1-1)
    - Tweak down radius 100>90 (Thanks Arhibeau)
    - Tweak down some values

    20 January 2024 (by JGi | Quéton 1-1)
    - Added missing weapons to explTable
    - Sort weapons in explTable by type
    - Added aircraft type in log when missing

    03 May 2023 (KERV)
      Correction AGM 154 (https://forum.dcs.world/topic/289290-splash-damage-20-script-make-explosions-better/page/5/#comment-5207760)
  
    06 March 2023 (Kerv)
    - Add some data for new ammunition

    16 April 2022
      spencershepard (GRIMM):
      - Added new/missing weapons to explTable
      - Added new option rocket_multiplier

    31 December 2021
      spencershepard (GRIMM):
      - Added many new weapons
      - Added filter for weapons.shells events
      - Fixed mission weapon message option
      - Changed default for damage_model option
  
    21 December 2021
      spencershepard (GRIMM):
      SPLASH DAMAGE 2.0:
      - Added blast wave effect to add timed and scaled secondary explosions on top of game objects
      - Object geometry within blast wave changes damage intensity
      - Damage boost for structures since they are hard to kill, even if very close to large explosions
      - Increased some rocket values in explTable
      - Missing weapons from explTable will display message to user and log to DCS.log so that we can add what's missing
      - Damage model for ground units that will disable their weapons and ability to move with partial damage before they are killed
      - Added options table to allow easy adjustments before release
      - General refactoring and restructure

    28 October 2020
      FrozenDroid: 
      - Uncommented error logging, actually made it an error log which shows a message box on error.
      - Fixed the too restrictive weapon filter (took out the HE warhead requirement)

    2 October 2020
      FrozenDroid:
      - Added error handling to all event handler and scheduled functions. Lua script errors can no longer bring the server down.
      - Added some extra checks to which weapons to handle, make sure they actually have a warhead (how come S-8KOM's don't have a warhead field...?)
--]]

----[[ ##### SCRIPT CONFIGURATION ##### ]]----
splash_damage_options = {
  --debug options
  ["game_messages"] = false, --enable some messages on screen
  ["debug"] = false,  --enable debugging messages 
  ["weapon_missing_message"] = false, --false disables messages alerting you to weapons missing from the explTable
  ["track_pre_explosion_debug"] = false, --Toggle to enable/disable pre-explosion tracking debugging

  ["enable_radio_menu"] = true, --enables the in-game radio menu for modifying settings
  
  ["static_damage_boost"] = 2000, --apply extra damage to Unit.Category.STRUCTUREs with wave explosions
  ["wave_explosions"] = true, --secondary explosions on top of game objects, radiating outward from the impact point and scaled based on size of object and distance from weapon impact point
  ["larger_explosions"] = true, --secondary explosions on top of weapon impact points, dictated by the values in the explTable
  ["damage_model"] = true, --allow blast wave to affect ground unit movement and weapons
  ["blast_search_radius"] = 90, --this is the max size of any blast wave radius, since we will only find objects within this zone
  ["cascade_damage_threshold"] = 0.1, --if the calculated blast damage doesn't exceed this value, there will be no secondary explosion damage on the unit. If this value is too small, the appearance of explosions far outside of an expected radius looks incorrect.
  ["blast_stun"] = false, --not implemented
  ["unit_disabled_health"] = 30, --if health is below this value after our explosions, disable its movement 
  ["unit_cant_fire_health"] = 40, --if health is below this value after our explosions, set ROE to HOLD to simulate damage weapon systems
  ["infantry_cant_fire_health"] = 60,  --if health is below this value after our explosions, set ROE to HOLD to simulate severe injury

  ["rocket_multiplier"] = 1.3, --multiplied by the explTable value for rockets
  ["overall_scaling"] = 1,    --overall scaling for explosive power
  
  ["apply_shaped_charge_effects"] = true, --apply reduction in blastwave etc for shaped charge munitions
  ["shaped_charge_multiplier"] = 0.2,  --multiplier that reduces blast radius and explosion power for shaped charge munitions.
  
  ["use_dynamic_blast_radius"] = true,   --if true, blast radius is calculated from explosion power; if false, blast_search_radius (90) is used
  ["dynamic_blast_radius_modifier"] = 2,  --multiplier for the blast radius
  
  ["cascade_scaling"] = 2,    --multiplier for secondary (cascade) blast damage, 1 damage fades out too soon, 2 or 3 damage seems a good balance
  ["cascade_explode_threshold"] = 60,   --only trigger cascade explosion if the unit's current health is <= this percent of its maximum, setting can help blow nearby jeeps but not tanks
  ["always_cascade_explode"] = false, --switch if you want everything to explode like with the original script
  

  --track_pre_explosion/enable_cargo_effects should both be the same value
  ["track_pre_explosion"] = true, --Toggle to enable/disable pre-explosion tracking
  ["enable_cargo_effects"] = true, --Toggle for enabling/disabling cargo explosions and cook-offs  
  ["cargo_damage_threshold"] = 60, --Health % below which cargo explodes (0 = destroyed only)
  ["debris_effects"] = true, --Enable debris from cargo cook-offs
  ["debris_power"] = 1, --Power of each debris explosion
  ["debris_count_min"] = 6, --Minimum debris pieces per cook-off
  ["debris_count_max"] = 12, --Maximum debris pieces per cook-off
  ["debris_max_distance"] = 10, --Max distance debris can travel (meters), the min distance from the vehicle will be 10% of this

  ["ordnance_protection"] = true, --Toggle ordinance protection features
  ["ordnance_protection_radius"] = 10, --Distance in meters to protect nearby bombs
  ["detect_ordnance_destruction"] = true, --Toggle detection of ordnance destroyed by large explosions
  ["snap_to_ground_if_destroyed_by_large_explosion"] = true, --If the ordnance protection fails or is disabled we can snap larger_explosions to the ground (if enabled - power as set in weapon list) - so an explosion still does hit the ground
  ["max_snapped_height"] = 80, --max height it will snap to ground from
  ["recent_large_explosion_snap"] = true, --enable looking for a recent large_explosion generated by the script
  ["recent_large_explosion_range"] = 100, --range its looking for in meters for a recent large_explosion generated by the script
  ["recent_large_explosion_time"] = 4, --in seconds how long ago there was a recent large_explosion generated by the script

  --Cluster bomb settings
  ["cluster_enabled"] = false,
  ["cluster_base_length"] = 150,           --Base forward spread (meters)
  ["cluster_base_width"] = 200,            --Base lateral spread (meters)
  ["cluster_max_length"] = 300,            --Max forward spread (meters)
  ["cluster_max_width"] = 400,             --Max lateral spread (meters)
  ["cluster_min_length"] = 100,            --Min forward spread
  ["cluster_min_width"] = 150,             --Min lateral spread
  ["cluster_bomblet_reductionmodifier"] = true, --Use equation to reduce number of bomblets (to make it look better)
  ["cluster_bomblet_damage_modifier"] = 1,  --Adjustable global modifier for bomblet explosive power

--Giant Explosion Options - Remember, any target you want to blow up needs to be named "GiantExplosionTarget(X)"  (X) being any value/name etc
  ["giant_explosion_enabled"] = false,  --Toggle to enable/disable Giant Explosion
  ["giant_explosion_power"] = 6000,    --Power in kg of TNT (default 8 tons)
  ["giant_explosion_scale"] = 1,     --Size scale factor (default 1)
  ["giant_explosion_duration"] = 3.0,  --Total duration in seconds (default 3s)
  ["giant_explosion_count"] = 250,      --Number of explosions (default 300)
  ["giant_explosion_target_static"] = true, --Toggle to true for static targets (store position once), false for dynamic (update every second)
  ["giant_explosion_poll_rate"] = 1,    --Polling rate in seconds for flag checks (default 1s)
}

local script_enable = 1
refreshRate = 0.1
----[[ ##### End of SCRIPT CONFIGURATION ##### ]]----

--Helper function: Trim whitespace.
local function trim(s)
  return s:match("^%s*(.-)%s*$")
end

cargoUnits = {

--[[
flamesize:

1 = small smoke and fire
2 = medium smoke and fire
3 = large smoke and fire
4 = huge smoke and fire
5 = small smoke
6 = medium smoke 
7 = large smoke
8 = huge smoke 
]]--	

  --1) M92 R11 Volvo driveable (Fuel Truck Tanker)
  ["r11_volvo_drivable"] = { 
      cargoExplosion = true,
      cargoExplosionMult = 2.0,
  cargoExplosionPower = 200,
      cargoCookOff = false,
      cookOffCount = 0,
      cookOffPower = 0,
      cookOffDuration = 0,
      cookOffRandomTiming = false,
      cookOffPowerRandom = 50,
      isTanker = true,
      flameSize = 3,
      flameDuration = 5,
  },

  --2) Refueler ATMZ-5
  ["ATMZ-5"] = {
      cargoExplosion = true,
      cargoExplosionMult = 2.0,
  cargoExplosionPower = 200,
      cargoCookOff = false,
      cookOffCount = 0,
      cookOffPower = 0,
      cookOffDuration = 0,
      cookOffRandomTiming = false,
      cookOffPowerRandom = 50,
      isTanker = true,
      flameSize = 3,
      flameDuration = 5,
  },

  --3) Refueler ATZ-10
  ["ATZ-10"] = {
      cargoExplosion = true,
      cargoExplosionMult = 2,
  cargoExplosionPower = 200,
      cargoCookOff = false,
      cookOffCount = 0,
      cookOffPower = 0,
      cookOffDuration = 0,
      cookOffRandomTiming = false,
      cookOffPowerRandom = 50,
      isTanker = true,
      flameSize = 3,
      flameDuration = 5,
  },

  --4) Refueler ATZ-5 
  ["ATZ-5"] = {
      cargoExplosion = true,
      cargoExplosionMult = 1.8,
  cargoExplosionPower = 200,
      cargoCookOff = false,
      cookOffCount = 0,
      cookOffPower = 0,
      cookOffDuration = 0,
      cookOffRandomTiming = false,
      cookOffPowerRandom = 50,
      isTanker = true,
      flameSize = 3,
      flameDuration = 5,
  },

  --5) Refueler M978 HEMTT (Fuel truck tanker)
  ["M978 HEMTT Tanker"] = {
      cargoExplosion = true,
      cargoExplosionMult = 2.0,
  cargoExplosionPower = 200,
      cargoCookOff = false,
      cookOffCount = 0,
      cookOffPower = 0,
      cookOffDuration = 0,
      cookOffRandomTiming = false,
      cookOffPowerRandom = 50,
      isTanker = true,
      flameSize = 3,
      flameDuration = 5,
  },

  --##### AMMO CARRIERS #####
  ["GAZ-66"] = {
      cargoExplosion = true,
      cargoExplosionMult = 1,
  cargoExplosionPower = 200,
      cargoCookOff = true,
      cookOffCount = 4,
      cookOffPower = 1,
      cookOffDuration = 20,
      cookOffRandomTiming = true,
      cookOffPowerRandom = 50,
      isTanker = false,
      flameSize = 1,
      flameDuration = 30,
  },
--#Technically this is both ammo and fuel looking at the model
  ["Ural-4320"] = {
      cargoExplosion = true,
      cargoExplosionMult = 1,
  cargoExplosionPower = 200,
      cargoCookOff = true,
      cookOffCount = 4,
      cookOffPower = 1,
      cookOffDuration = 20,
      cookOffRandomTiming = true,
      cookOffPowerRandom = 50,
      isTanker = true,
      flameSize = 1,
      flameDuration = 30,
  },

  ["ZIL-135"] = {
      cargoExplosion = true,
      cargoExplosionMult = 1,
  cargoExplosionPower = 200,
      cargoCookOff = true,
      cookOffCount = 5,
      cookOffPower = 1,
      cookOffDuration = 20,
      cookOffRandomTiming = true,
      cookOffPowerRandom = 50,
      isTanker = false,
      flameSize = 1,
      flameDuration = 30,
  },
}

--Weapon Explosive Table
explTable = {
  --*** WWII BOMBS ***
  ["British_GP_250LB_Bomb_Mk1"] = { explosive = 100, shaped_charge = false },
  ["British_GP_250LB_Bomb_Mk4"] = { explosive = 100, shaped_charge = false },
  ["British_GP_250LB_Bomb_Mk5"] = { explosive = 100, shaped_charge = false },
  ["British_GP_500LB_Bomb_Mk1"] = { explosive = 213, shaped_charge = false },
  ["British_GP_500LB_Bomb_Mk4"] = { explosive = 213, shaped_charge = false },
  ["British_GP_500LB_Bomb_Mk4_Short"] = { explosive = 213, shaped_charge = false },
  ["British_GP_500LB_Bomb_Mk5"] = { explosive = 213, shaped_charge = false },
  ["British_MC_250LB_Bomb_Mk1"] = { explosive = 100, shaped_charge = false },
  ["British_MC_250LB_Bomb_Mk2"] = { explosive = 100, shaped_charge = false },
  ["British_MC_500LB_Bomb_Mk1_Short"] = { explosive = 213, shaped_charge = false },
  ["British_MC_500LB_Bomb_Mk2"] = { explosive = 213, shaped_charge = false },
  ["British_SAP_250LB_Bomb_Mk5"] = { explosive = 100, shaped_charge = false },
  ["British_SAP_500LB_Bomb_Mk5"] = { explosive = 213, shaped_charge = false },
  ["British_AP_25LBNo1_3INCHNo1"] = { explosive = 4, shaped_charge = false },
  ["British_HE_60LBSAPNo2_3INCHNo1"] = { explosive = 4, shaped_charge = false },
  ["British_HE_60LBFNo1_3INCHNo1"] = { explosive = 4, shaped_charge = false },

  ["SC_50"] = { explosive = 20, shaped_charge = false },
  ["ER_4_SC50"] = { explosive = 20, shaped_charge = false },
  ["SC_250_T1_L2"] = { explosive = 100, shaped_charge = false },
  ["SC_501_SC250"] = { explosive = 100, shaped_charge = false },
  ["Schloss500XIIC1_SC_250_T3_J"] = { explosive = 100, shaped_charge = false },
  ["SC_501_SC500"] = { explosive = 213, shaped_charge = false },
  ["SC_500_L2"] = { explosive = 213, shaped_charge = false },
  ["SD_250_Stg"] = { explosive = 100, shaped_charge = false },
  ["SD_500_A"] = { explosive = 213, shaped_charge = false },

  --*** WWII CBU ***
  ["AB_250_2_SD_2"] = { explosive = 100, shaped_charge = false },
  ["AB_250_2_SD_10A"] = { explosive = 100, shaped_charge = false },
  ["AB_500_1_SD_10A"] = { explosive = 213, shaped_charge = false },

  --*** WWII ROCKETS ***
  ["3xM8_ROCKETS_IN_TUBES"] = { explosive = 4, shaped_charge = false },
  ["WGr21"] = { explosive = 4, shaped_charge = false },

  --*** UNGUIDED BOMBS (UGB) ***
  ["M_117"] = { explosive = 201, shaped_charge = false },
  ["AN_M30A1"] = { explosive = 45, shaped_charge = false },
  ["AN_M57"] = { explosive = 100, shaped_charge = false },
  ["AN_M64"] = { explosive = 121, shaped_charge = false },
  ["AN_M65"] = { explosive = 400, shaped_charge = false },
  ["AN_M66"] = { explosive = 800, shaped_charge = false },
  ["AN-M66A2"] = { explosive = 536, shaped_charge = false },
  ["AN-M81"] = { explosive = 100, shaped_charge = false },
  ["AN-M88"] = { explosive = 100, shaped_charge = false },

  ["Mk_81"] = { explosive = 60, shaped_charge = false },
  ["MK-81SE"] = { explosive = 60, shaped_charge = false },
  ["Mk_82"] = { explosive = 100, shaped_charge = false },
  ["MK_82AIR"] = { explosive = 100, shaped_charge = false },
  ["MK_82SNAKEYE"] = { explosive = 100, shaped_charge = false },
  ["Mk_83"] = { explosive = 274, shaped_charge = false },
  ["Mk_84"] = { explosive = 582, shaped_charge = false },

  ["HEBOMB"] = { explosive = 40, shaped_charge = false },
  ["HEBOMBD"] = { explosive = 40, shaped_charge = false },

  ["SAMP125LD"] = { explosive = 60, shaped_charge = false },
  ["SAMP250LD"] = { explosive = 118, shaped_charge = false },
  ["SAMP250HD"] = { explosive = 118, shaped_charge = false },
  ["SAMP400LD"] = { explosive = 274, shaped_charge = false },
  ["SAMP400HD"] = { explosive = 274, shaped_charge = false },

  ["BR_250"] = { explosive = 100, shaped_charge = false },
  ["BR_500"] = { explosive = 100, shaped_charge = false },

  ["FAB_100"] = { explosive = 45, shaped_charge = false },
  ["FAB_250"] = { explosive = 118, shaped_charge = false },
  ["FAB_250M54TU"] = { explosive = 118, shaped_charge = false },
  ["FAB-250-M62"] = { explosive = 118, shaped_charge = false },
  ["FAB_500"] = { explosive = 213, shaped_charge = false },
  ["FAB_1500"] = { explosive = 675, shaped_charge = false },

  --*** UNGUIDED BOMBS WITH PENETRATOR / ANTI-RUNWAY ***
  ["Durandal"] = { explosive = 64, shaped_charge = false },
  ["BLU107B_DURANDAL"] = { explosive = 64, shaped_charge = false },
  ["BAP_100"] = { explosive = 32, shaped_charge = false },
  ["BAP-100"] = { explosive = 32, shaped_charge = false },
  ["BAT-120"] = { explosive = 32, shaped_charge = false },
  ["TYPE-200A"] = { explosive = 107, shaped_charge = false },
  ["BetAB_500"] = { explosive = 98, shaped_charge = false },
  ["BetAB_500ShP"] = { explosive = 107, shaped_charge = false },
  
  --*** GUIDED BOMBS (GBU) ***
  ["GBU_10"] = { explosive = 582, shaped_charge = false },
  ["GBU_12"] = { explosive = 100, shaped_charge = false },
  ["GBU_16"] = { explosive = 274, shaped_charge = false },
  ["GBU_24"] = { explosive = 582, shaped_charge = false },
  ["KAB_1500Kr"] = { explosive = 675, shaped_charge = false },
  ["KAB_500Kr"] = { explosive = 213, shaped_charge = false },
  ["KAB_500"] = { explosive = 213, shaped_charge = false },

  --*** CLUSTER BOMBS (CBU) ***
--I don't have most of these so can't test them with debug on
  ["CBU_99"] = { explosive = 0, shaped_charge = false, cluster = true, submunition_count = 247, submunition_explosive = 2, submunition_name = "Mk 118" }, --Mk 20 Rockeye variant, confirmed 247 Mk 118 bomblets
  ["ROCKEYE"] = { explosive = 0, shaped_charge = false, cluster = true, submunition_count = 247, submunition_explosive = 2, submunition_name = "Mk 118" }, --Mk 20 Rockeye, confirmed 247 Mk 118 bomblets
  ["BLU_3B_GROUP"] = { explosive = 0, shaped_charge = false, cluster = true, submunition_count = 19, submunition_explosive = 0.2, submunition_name = "BLU_3B" }, --Not in datamine, possibly custom or outdated; submunition name guessed
  ["MK77mod0-WPN"] = { explosive = 0, shaped_charge = false, cluster = true, submunition_count = 132, submunition_explosive = 0.1, submunition_name = "BLU_1B" }, --Not in datamine, possibly custom; submunition name guessed
  ["MK77mod1-WPN"] = { explosive = 0, shaped_charge = false, cluster = true, submunition_count = 132, submunition_explosive = 0.1, submunition_name = "BLU_1B" }, --Not in datamine, possibly custom; submunition name guessed
  ["CBU_87"] = { explosive = 0, shaped_charge = false, cluster = true, submunition_count = 202, submunition_explosive = 0.5, submunition_name = "BLU_97B" }, --Confirmed 202 BLU-97/B bomblets
  ["CBU_103"] = { explosive = 0, shaped_charge = false, cluster = true, submunition_count = 202, submunition_explosive = 0.5, submunition_name = "BLU_97B" }, --WCMD variant of CBU-87, confirmed 202 BLU-97/B bomblets
  ["CBU_97"] = { explosive = 0, shaped_charge = false, cluster = true, submunition_count = 10, submunition_explosive = 15, submunition_name = "BLU_108" }, --Confirmed 10 BLU-108 submunitions, each with 4 skeets
  ["CBU_105"] = { explosive = 0, shaped_charge = false, cluster = true, submunition_count = 10, submunition_explosive = 15, submunition_name = "BLU_108" }, --WCMD variant of CBU-97, confirmed 10 BLU-108 submunitions
  ["BELOUGA"] = { explosive = 0, shaped_charge = false, cluster = true, submunition_count = 151, submunition_explosive = 0.3, submunition_name = "grenade_AC" }, --Confirmed 151 grenade_AC bomblets (French BLG-66)
  ["BLG66_BELOUGA"] = { explosive = 0, shaped_charge = false, cluster = true, submunition_count = 151, submunition_explosive = 0.3, submunition_name = "grenade_AC" }, --Alias for BELOUGA, confirmed 151 grenade_AC bomblets
  ["BL_755"] = { explosive = 0, shaped_charge = false, cluster = true, submunition_count = 147, submunition_explosive = 0.4, submunition_name = "BL_755_bomblet" }, --Confirmed 147 bomblets, submunition name from your table
  ["RBK_250"] = { explosive = 0, shaped_charge = false, cluster = true, submunition_count = 60, submunition_explosive = 0.5, submunition_name = "PTAB_25M" }, --Confirmed 60 PTAB-2.5M anti-tank bomblets
  ["RBK_250_275_AO_1SCH"] = { explosive = 0, shaped_charge = false, cluster = true, submunition_count = 150, submunition_explosive = 0.2, submunition_name = "AO_1SCh" }, --Confirmed 150 AO-1SCh fragmentation bomblets
  ["RBK_500"] = { explosive = 0, shaped_charge = false, cluster = true, submunition_count = 108, submunition_explosive = 0.5, submunition_name = "PTAB_10_5" }, --Confirmed 108 PTAB-10-5 anti-tank bomblets
  ["RBK_500U"] = { explosive = 0, shaped_charge = false, cluster = true, submunition_count = 352, submunition_explosive = 0.2, submunition_name = "OAB_25RT" }, --Confirmed 352 OAB-2.5RT fragmentation bomblets
  ["RBK_500AO"] = { explosive = 0, shaped_charge = false, cluster = true, submunition_count = 108, submunition_explosive = 0.5, submunition_name = "AO_25RT" }, --Confirmed 108 AO-2.5RT fragmentation bomblets
  ["RBK_500U_OAB_2_5RT"] = { explosive = 0, shaped_charge = false, cluster = true, submunition_count = 352, submunition_explosive = 0.2, submunition_name = "OAB_25RT" }, --Confirmed 352 OAB-2.5RT fragmentation bomblets
["RBK_500_255_PTO_1M"] = { explosive = 0, shaped_charge = false, cluster = true, submunition_count = 126, submunition_explosive = 0.5, submunition_name = "PTO_1M" },
["RBK_500_255_ShO"] = { explosive = 0, shaped_charge = false, cluster = true, submunition_count = 565, submunition_explosive = 0.1, submunition_name = "ShO" },  
  --*** INS/GPS BOMBS (JDAM) ***
  ["GBU_31"] = { explosive = 582, shaped_charge = false },
  ["GBU_31_V_3B"] = { explosive = 582, shaped_charge = false },
  ["GBU_31_V_2B"] = { explosive = 582, shaped_charge = false },
  ["GBU_31_V_4B"] = { explosive = 582, shaped_charge = false },
  ["GBU_32_V_2B"] = { explosive = 202, shaped_charge = false },
  ["GBU_38"] = { explosive = 100, shaped_charge = false },
  ["GBU_54_V_1B"] = { explosive = 100, shaped_charge = false },

  --*** GLIDE BOMBS (JSOW) ***
  ["AGM_154A"] = { explosive = 0, shaped_charge = false, cluster = true, submunition_count = 145, submunition_explosive = 2, submunition_name = "BLU-97/B" }, --JSOW-A, confirmed 145 BLU-97 bomblets from datamine
  ["AGM_154C"] = { explosive = 305, shaped_charge = false },
  ["AGM_154"] = { explosive = 305, shaped_charge = false },
  ["BK90_MJ1"] = { explosive = 0, shaped_charge = false },
  ["BK90_MJ1_MJ2"] = { explosive = 0, shaped_charge = false },
  ["BK90_MJ2"] = { explosive = 0, shaped_charge = false },

  ["LS-6-100"] = { explosive = 45, shaped_charge = false },
  ["LS-6-250"] = { explosive = 100, shaped_charge = false },
  ["LS-6-500"] = { explosive = 274, shaped_charge = false },
  ["GB-6"] = { explosive = 0, shaped_charge = false },
  ["GB-6-HE"] = { explosive = 0, shaped_charge = false },
  ["GB-6-SFW"] = { explosive = 0, shaped_charge = false },

  --*** AIR GROUND MISSILE (AGM) ***
  ["AGM_62"] = { explosive = 400, shaped_charge = false },
  ["AGM_65D"] = { explosive = 38, shaped_charge = true },
  ["AGM_65E"] = { explosive = 80, shaped_charge = true },
  ["AGM_65F"] = { explosive = 80, shaped_charge = true },
  ["AGM_65G"] = { explosive = 80, shaped_charge = true },
  ["AGM_65H"] = { explosive = 38, shaped_charge = true },
  ["AGM_65K"] = { explosive = 80, shaped_charge = true },
  ["AGM_65L"] = { explosive = 80, shaped_charge = true },
  ["AGM_123"] = { explosive = 274, shaped_charge = false },
  ["AGM_130"] = { explosive = 582, shaped_charge = false },
  ["AGM_119"] = { explosive = 176, shaped_charge = false },
  ["AGM_114"] = { explosive = 10, shaped_charge = true },
  ["AGM_114K"] = { explosive = 10, shaped_charge = true },

  ["Rb 05A"] = { explosive = 217, shaped_charge = false },
  ["RB75"] = { explosive = 38, shaped_charge = false },
  ["RB75A"] = { explosive = 38, shaped_charge = false },
  ["RB75B"] = { explosive = 38, shaped_charge = false },
  ["RB75T"] = { explosive = 80, shaped_charge = false },
  ["HOT3_MBDA"] = { explosive = 15, shaped_charge = false },
  ["C-701T"] = { explosive = 38, shaped_charge = false },
  ["C-701IR"] = { explosive = 38, shaped_charge = false },

  ["Vikhr_M"] = { explosive = 11, shaped_charge = false },
  ["Vikhr_9M127_1"] = { explosive = 11, shaped_charge = false },
  ["AT_6"] = { explosive = 11, shaped_charge = false },
  ["Ataka_9M120"] = { explosive = 11, shaped_charge = false },
  ["Ataka_9M120F"] = { explosive = 11, shaped_charge = false },
  ["P_9M117"] = { explosive = 0, shaped_charge = false },
  
  ["KH-66_Grom"] = { explosive = 108, shaped_charge = false },
  ["X_23"] = { explosive = 111, shaped_charge = false },
  ["X_23L"] = { explosive = 111, shaped_charge = false },
  ["X_28"] = { explosive = 160, shaped_charge = false },
  ["X_25ML"] = { explosive = 89, shaped_charge = false },
  ["X_25MR"] = { explosive = 140, shaped_charge = false },
  ["X_29L"] = { explosive = 320, shaped_charge = false },
  ["X_29T"] = { explosive = 320, shaped_charge = false },
  ["X_29TE"] = { explosive = 320, shaped_charge = false },

  --*** ANTI-RADAR MISSILE (ARM) ***
  ["AGM_88C"] = { explosive = 89, shaped_charge = false },
  ["AGM_88"] = { explosive = 89, shaped_charge = false },
  ["AGM_122"] = { explosive = 15, shaped_charge = false },
  ["LD-10"] = { explosive = 89, shaped_charge = false },
  ["AGM_45A"] = { explosive = 38, shaped_charge = false },
  ["X_58"] = { explosive = 140, shaped_charge = false },
  ["X_25MP"] = { explosive = 89, shaped_charge = false },

  --*** ANTI-SHIP MISSILE (ASh) ***
  ["AGM_84D"] = { explosive = 488, shaped_charge = false },
  ["Rb 15F"] = { explosive = 500, shaped_charge = false },
  ["C-802AK"] = { explosive = 500, shaped_charge = false },

  --*** CRUISE MISSILE ***
  ["CM-802AKG"] = { explosive = 488, shaped_charge = false },
  ["AGM_84E"] = { explosive = 488, shaped_charge = false },
  ["AGM_84H"] = { explosive = 488, shaped_charge = false },
  ["X_59M"] = { explosive = 488, shaped_charge = false },

  --*** ROCKETS ***
  ["HYDRA_70M15"] = { explosive = 5, shaped_charge = false },
  ["HYDRA_70_MK1"] = { explosive = 5, shaped_charge = false },
  ["HYDRA_70_MK5"] = { explosive = 8, shaped_charge = false },
  ["HYDRA_70_M151"] = { explosive = 5, shaped_charge = false },
  ["HYDRA_70_M151_M433"] = { explosive = 5, shaped_charge = false },
  ["HYDRA_70_M229"] = { explosive = 10, shaped_charge = false },
  ["FFAR Mk1 HE"] = { explosive = 5, shaped_charge = false },
  ["FFAR Mk5 HEAT"] = { explosive = 8, shaped_charge = false },
  ["HVAR"] = { explosive = 5, shaped_charge = false },
  ["Zuni_127"] = { explosive = 8, shaped_charge = false },
  ["ARAKM70BHE"] = { explosive = 5, shaped_charge = false },
  ["ARAKM70BAP"] = { explosive = 8, shaped_charge = false },
  ["SNEB_TYPE251_F1B"] = { explosive = 4, shaped_charge = false },
  ["SNEB_TYPE252_F1B"] = { explosive = 4, shaped_charge = false },
  ["SNEB_TYPE253_F1B"] = { explosive = 5, shaped_charge = false },
  ["SNEB_TYPE256_F1B"] = { explosive = 6, shaped_charge = false },
  ["SNEB_TYPE257_F1B"] = { explosive = 8, shaped_charge = false },
  ["SNEB_TYPE251_F4B"] = { explosive = 4, shaped_charge = false },
  ["SNEB_TYPE252_F4B"] = { explosive = 4, shaped_charge = false },
  ["SNEB_TYPE253_F4B"] = { explosive = 5, shaped_charge = false },
  ["SNEB_TYPE256_F4B"] = { explosive = 6, shaped_charge = false },
  ["SNEB_TYPE257_F4B"] = { explosive = 8, shaped_charge = false },
  ["SNEB_TYPE251_H1"] = { explosive = 4, shaped_charge = false },
  ["SNEB_TYPE252_H1"] = { explosive = 4, shaped_charge = false },
  ["SNEB_TYPE253_H1"] = { explosive = 5, shaped_charge = false },
  ["SNEB_TYPE256_H1"] = { explosive = 6, shaped_charge = false },
  ["SNEB_TYPE257_H1"] = { explosive = 8, shaped_charge = false },
  ["MATRA_F4_SNEBT251"] = { explosive = 8, shaped_charge = false },
  ["MATRA_F4_SNEBT253"] = { explosive = 8, shaped_charge = false },
  ["MATRA_F4_SNEBT256"] = { explosive = 8, shaped_charge = false },
  ["MATRA_F1_SNEBT253"] = { explosive = 8, shaped_charge = false },
  ["MATRA_F1_SNEBT256"] = { explosive = 8, shaped_charge = false },
  ["TELSON8_SNEBT251"] = { explosive = 4, shaped_charge = false },
  ["TELSON8_SNEBT253"] = { explosive = 8, shaped_charge = false },
  ["TELSON8_SNEBT256"] = { explosive = 4, shaped_charge = false },
  ["TELSON8_SNEBT257"] = { explosive = 6, shaped_charge = false },
  ["ARF8M3API"] = { explosive = 8, shaped_charge = false },
  ["UG_90MM"] = { explosive = 8, shaped_charge = false },
  ["S-24A"] = { explosive = 24, shaped_charge = false },
  ["S-25OF"] = { explosive = 194, shaped_charge = false },
  ["S-25OFM"] = { explosive = 150, shaped_charge = false },
  ["S-25O"] = { explosive = 150, shaped_charge = false },
  ["S-25-O"] = { explosive = 150, shaped_charge = false },
  ["S_25L"] = { explosive = 190, shaped_charge = false },
  ["S-5M"] = { explosive = 1, shaped_charge = false },
  ["C_5"] = { explosive = 8, shaped_charge = false },
  ["C5"] = { explosive = 5, shaped_charge = false },
  ["C_8"] = { explosive = 4, shaped_charge = false },
  ["C_8OFP2"] = { explosive = 3, shaped_charge = false },
  ["C_13"] = { explosive = 21, shaped_charge = false },
  ["C_24"] = { explosive = 123, shaped_charge = false },
  ["C_25"] = { explosive = 151, shaped_charge = false },

  --*** LASER ROCKETS ***
  ["AGR_20"] = { explosive = 8, shaped_charge = false },
  ["AGR_20A"] = { explosive = 8, shaped_charge = false },
  ["AGR_20_M282"] = { explosive = 8, shaped_charge = false },
  ["Hydra_70_M282_MPP"] = { explosive = 5, shaped_charge = true },
  ["BRM-1_90MM"] = { explosive = 8, shaped_charge = false },
}






local effectSmokeId = 1

----[[ ##### HELPER/UTILITY FUNCTIONS ##### ]]----

local function tableHasKey(table, key)
  return table[key] ~= nil
end

local function debugMsg(str)
  if splash_damage_options.debug == true then
      debugCounter = (debugCounter or 0) + 1
      local uniqueStr = str .. " [" .. timer.getTime() .. " - " .. debugCounter .. "]"
      trigger.action.outText(uniqueStr, 5)
      env.info("DEBUG: " .. uniqueStr)
  end
end

local function gameMsg(str)
  if splash_damage_options.game_messages == true then
      trigger.action.outText(str, 5)
  end
end

local function getDistance(point1, point2)
  local x1 = point1.x
  local y1 = point1.y
  local z1 = point1.z
  local x2 = point2.x
  local y2 = point2.y
  local z2 = point2.z
  local dX = math.abs(x1 - x2)
  local dZ = math.abs(z1 - z2)
  local distance = math.sqrt(dX * dX + dZ * dZ)
  return distance
end

local function getDistance3D(point1, point2)
  local x1 = point1.x
  local y1 = point1.y
  local z1 = point1.z
  local x2 = point2.x
  local y2 = point2.y
  local z2 = point2.z
  local dX = math.abs(x1 - x2)
  local dY = math.abs(y1 - y2)
  local dZ = math.abs(z1 - z2)
  local distance = math.sqrt(dX * dX + dZ * dZ + dY * dY)
  return distance
end

local function vec3Mag(speedVec)
  return math.sqrt(speedVec.x^2 + speedVec.y^2 + speedVec.z^2)
end

local function lookahead(speedVec)
  local speed = vec3Mag(speedVec)
  local dist = speed * refreshRate * 1.5 
  return dist
end

--Cluster-specific helper functions from Rockeye script
local function normalizeVector(vec)
  local mag = math.sqrt(vec.x^2 + vec.z^2)
  if mag > 0 then
      return { x = vec.x / mag, z = vec.z / mag }
  else
      return { x = 1, z = 0 }
  end
end
local function calculate_drop_angle(velocity)
  local horizontal_speed = math.sqrt((velocity.x or 0)^2 + (velocity.z or 0)^2)
  local vertical_speed = math.abs(velocity.y or 0)
  if horizontal_speed == 0 then return 90 end
  local angle_rad = math.atan(vertical_speed / horizontal_speed)
  return math.deg(angle_rad)
end
local function calculate_dispersion(velocity, burst_altitude)
  local velocity_magnitude = math.sqrt((velocity.x or 0)^2 + (velocity.z or 0)^2)
  local drop_angle = calculate_drop_angle(velocity)
  local length = splash_damage_options.cluster_base_length * (1 + velocity_magnitude / 200)
  local width = splash_damage_options.cluster_base_width * (1 + burst_altitude / 6000)
  local length_jitter = length * (0.85 + math.random() * 0.3)
  local width_jitter = width * (0.85 + math.random() * 0.3)
  return math.max(splash_damage_options.cluster_min_length, math.min(splash_damage_options.cluster_max_length, length_jitter)),
         math.max(splash_damage_options.cluster_min_width, math.min(splash_damage_options.cluster_max_width, width_jitter))
end









----[[ ##### End of HELPER/UTILITY FUNCTIONS ##### ]]----
giantExplosionTargets = {}
cargoEffectsQueue = {}
WpnHandler = {}
tracked_target_position = nil --Store the last known position of TargetUnit for giant explosion
tracked_weapons = {}
local processedUnitsGlobal = {}

function scanGiantExplosionTargets()
  giantExplosionTargets = {}
  local function findTargets(obj)
      if obj:isExist() then
          local name = obj:getName()
          if string.find(name, "GiantExplosionTarget") then
              local flagName = string.gsub(name, "Target", "")
              table.insert(giantExplosionTargets, {
                  name = name,
                  flag = flagName,
                  obj = obj,
                  pos = obj:getPoint(),
                  static = splash_damage_options.giant_explosion_target_static
              })
          end
      end
      return true
  end
  world.searchObjects({Object.Category.UNIT, Object.Category.STATIC}, {id = world.VolumeType.ALL}, findTargets)
  if not splash_damage_options.giant_explosion_target_static then
      timer.scheduleFunction(updateGiantExplosionPositions, {}, timer.getTime() + 1.0)
  end
end

function updateTargetPosition()
  for name, target in pairs(giantExplosionTargets) do
      if target.obj:isExist() then
          target.pos = target.obj:getPosition().p
      end
  end
  return timer.getTime() + 1.0
end


--Giant Explosion Function
function triggerGiantExplosion(params)
  if not splash_damage_options.giant_explosion_enabled then
      debugMsg("Giant Explosion is disabled in options.")
      return
  end

  local initialPos = params.pos or {x = 0, y = 0, z = 0}
  local explosionPower = params.power or splash_damage_options.giant_explosion_power
  local sizeScale = params.scale or splash_damage_options.giant_explosion_scale
  local totalDuration = params.duration or splash_damage_options.giant_explosion_duration
  local explosionCount = params.count or splash_damage_options.giant_explosion_count

  if not initialPos.x or not initialPos.y or not initialPos.z then
      gameMsg("Error: Invalid position for giant explosion!")
      debugMsg("No valid initial position set for giant explosion!")
      return
  end

  debugMsg("Triggering giant fireball at X: " .. initialPos.x .. ", Y: " .. initialPos.y .. ", Z: " .. initialPos.z)

  local function scheduleExplosion(pos, delay)
      if not pos or not pos.x or not pos.y or not pos.z then
          debugMsg("Error: Invalid position for explosion - pos: " .. tostring(pos))
          return
      end
      timer.scheduleFunction(function(p)
          if p and p.x and p.y and p.z then
              trigger.action.explosion(p, explosionPower)
          end
      end, pos, timer.getTime() + delay)
  end

  -- Pre-explosion scan for cargo units
  local scanRadius = 1500 * sizeScale -- 1500m base radius, scaled by sizeScale
  local preExplosionTargets = {}
  if splash_damage_options.enable_cargo_effects then
      local volS = {
          id = world.VolumeType.SPHERE,
          params = { point = initialPos, radius = scanRadius }
      }
      local ifFound = function(foundObject)
          if foundObject:isExist() then
              local category = foundObject:getCategory()
              if (category == Object.Category.UNIT and foundObject:getDesc().category == Unit.Category.GROUND_UNIT) or
                 category == Object.Category.STATIC then
                  table.insert(preExplosionTargets, {
                      name = foundObject:getTypeName(),
                      health = foundObject:getLife() or 0,
                      position = foundObject:getPoint(),
                      maxHealth = (category == Object.Category.UNIT and foundObject:getDesc().life) or foundObject:getLife() or 0,
                      unit = foundObject
                  })
              end
          end
          return true
      end
      world.searchObjects({Object.Category.UNIT, Object.Category.STATIC}, volS, ifFound)
      debugMsg("Pre-explosion scan for Giant Explosion: " .. #preExplosionTargets .. " targets found within " .. scanRadius .. "m")
  end
  -- Trigger the explosion
  local maxRadius = 200 * sizeScale
  local maxHeight = 500 * sizeScale
  local adjustedExplosionCount = math.floor(explosionCount * (sizeScale ^ 2.5))
  local stepTime = totalDuration / adjustedExplosionCount
  local variance = 0.25 --Fixed at 25%

  for i = 1, adjustedExplosionCount do
      local progress = i / adjustedExplosionCount
      local currentRadius = maxRadius * progress
      local r = currentRadius * (0.9 + math.random() * 0.1)
      local theta = math.random() * 2 * math.pi
      local phi = math.acos(math.random())

      local offsetX = r * math.sin(phi) * math.cos(theta)
      local offsetZ = r * math.sin(phi) * math.sin(theta)
      local offsetY = r * math.cos(phi)

      offsetX = offsetX * (1 + (math.random() - 0.5) * variance)
      offsetZ = offsetZ * (1 + (math.random() - 0.5) * variance)
      offsetY = offsetY * (1 + (math.random() - 0.5) * variance * 0.5)

      local blastPos = {
          x = initialPos.x + offsetX,
          y = land.getHeight({x = initialPos.x, y = initialPos.z}) + offsetY,
          z = initialPos.z + offsetZ
      }
      if blastPos.y < land.getHeight({x = blastPos.x, y = blastPos.z}) then
          blastPos.y = land.getHeight({x = blastPos.x, y = blastPos.z})
      end

      local delay = (i - 1) * stepTime + (math.random() - 0.5) * stepTime * variance
      scheduleExplosion(blastPos, delay)
  end

  gameMsg("Expanding giant fireball over " .. totalDuration .. "s (scale " .. sizeScale .. ")!")

  -- Post-explosion scan and cargo cook-off queuing
  if splash_damage_options.enable_cargo_effects then
      timer.scheduleFunction(function(args)
          local centerPos = args[1]
          local radius = args[2]
          local preTargets = args[3]

          local postExplosionTargets = {}
          local volS = {
              id = world.VolumeType.SPHERE,
              params = { point = centerPos, radius = radius }
          }
          local ifFound = function(foundObject)
              if foundObject:isExist() then
                  local category = foundObject:getCategory()
                  if (category == Object.Category.UNIT and foundObject:getDesc().category == Unit.Category.GROUND_UNIT) or
                     category == Object.Category.STATIC then
                      table.insert(postExplosionTargets, {
                          name = foundObject:getTypeName(),
                          health = foundObject:getLife() or 0,
                          position = foundObject:getPoint(),
                          maxHealth = (category == Object.Category.UNIT and foundObject:getDesc().life) or foundObject:getLife() or 0
                      })
                  end
              end
              return true
          end
          world.searchObjects({Object.Category.UNIT, Object.Category.STATIC}, volS, ifFound)
          debugMsg("Post-explosion scan for Giant Explosion: " .. #postExplosionTargets .. " targets found within " .. radius .. "m")

          -- Compare pre- and post-explosion targets
          for _, preTarget in ipairs(preTargets) do
              local found = false
              local postHealth = 0
              for _, postTarget in ipairs(postExplosionTargets) do
                  if preTarget.name == postTarget.name and getDistance(preTarget.position, postTarget.position) < 1 then
                      found = true
                      postHealth = postTarget.health
                      break
                  end
              end

              local cargoData = cargoUnits[preTarget.name]
              if cargoData and (not found or postHealth <= 0) then
                  local distance = getDistance(initialPos, preTarget.position)
                  if distance <= radius then
                      local cargoPower = cargoData.cargoExplosionPower or explosionPower
                      table.insert(cargoEffectsQueue, {
                          name = preTarget.name,
                          distance = distance,
                          coords = preTarget.position,
                          power = cargoPower,
                          explosion = cargoData.cargoExplosion,
                          cookOff = cargoData.cargoCookOff,
                          cookOffCount = cargoData.cookOffCount,
                          cookOffPower = cargoData.cookOffPower,
                          cookOffDuration = cargoData.cookOffDuration,
                          cookOffRandomTiming = cargoData.cookOffRandomTiming,
                          cookOffPowerRandom = cargoData.cookOffPowerRandom,
                          isTanker = cargoData.isTanker,
                          flameSize = cargoData.flameSize,
                          flameDuration = cargoData.flameDuration
                      })
                      debugMsg("Queued cargo effect for " .. preTarget.name .. " destroyed by Giant Explosion at " .. string.format("%.1f", distance) .. "m")
                  end
              end
          end

          -- Process queued cargo effects with prioritized flames
          if #cargoEffectsQueue > 0 then
              local flameIndex = 0 -- Separate index for flames
              local otherIndex = 0 -- Index for explosions, cook-offs, debris
              local processedCargoUnits = {}
              local flamePositions = {}
              for _, effect in ipairs(cargoEffectsQueue) do
                  local unitKey = effect.name .. "_" .. effect.coords.x .. "_" .. effect.coords.z
                  if not processedUnitsGlobal[unitKey] and not processedCargoUnits[unitKey] then
                      -- Handle tanker flames first with minimal delay
                      if effect.isTanker and effect.explosion then
                          debugMsg("Triggering cargo explosion for tanker " .. effect.name .. " at " .. string.format("%.1f", effect.distance) .. "m with power " .. effect.power .. " scheduled at " .. flameIndex .. "s")
                          timer.scheduleFunction(function(params)
                              debugMsg("Executing cargo explosion at X: " .. string.format("%.0f", params[1].x) .. ", Y: " .. string.format("%.0f", params[1].y) .. ", Z: " .. string.format("%.0f", params[1].z) .. " with power " .. params[2])
                              trigger.action.explosion(params[1], params[2])
                          end, {effect.coords, effect.power}, timer.getTime() + flameIndex + 0.1)

                              local flameSize = effect.flameSize or 3
                              local flameDuration = effect.flameDuration
                              local flameDensity = 1.0
                              local effectId = effectSmokeId
                              effectSmokeId = effectSmokeId + 1
                              local isDuplicate = false
                              for _, pos in pairs(flamePositions) do
                                  if getDistance3D(effect.coords, pos) < 3 then
                                      isDuplicate = true
                                      debugMsg("Skipping duplicate flame for " .. effect.name .. " near X: " .. string.format("%.0f", pos.x) .. ", Y: " .. string.format("%.0f", pos.y) .. ", Z: " .. string.format("%.0f", pos.z))
                                      break
                                  end
                              end
                              if not isDuplicate then
                              debugMsg("Adding flame effect for tanker " .. effect.name .. " at " .. string.format("%.1f", effect.distance) .. "m (Size: " .. flameSize .. ", Duration: " .. flameDuration .. "s, ID: " .. effectId .. ") scheduled at " .. flameIndex .. "s")
                                  timer.scheduleFunction(function(params)
                                      local terrainHeight = land.getHeight({x = params[1].x, y = params[1].z})
                                      local adjustedCoords = {x = params[1].x, y = terrainHeight + 2, z = params[1].z}
                                      debugMsg("Spawning flame effect at X: " .. string.format("%.0f", adjustedCoords.x) .. ", Y: " .. string.format("%.0f", adjustedCoords.y) .. ", Z: " .. string.format("%.0f", adjustedCoords.z))
                                      trigger.action.explosion(adjustedCoords, 10) -- Small trigger explosion
                                      trigger.action.effectSmokeBig(adjustedCoords, params[2], params[3], params[4])
                              end, {effect.coords, flameSize, flameDensity, effectId}, timer.getTime() + flameIndex + 0.2)
                                  timer.scheduleFunction(function(id)
                                      debugMsg("Stopping flame effect for " .. effect.name .. " (ID: " .. id .. ")")
                                      trigger.action.effectSmokeStop(id)
                              end, effectId, timer.getTime() + flameIndex + flameDuration + 0.2)
                                  table.insert(flamePositions, effect.coords)
                              end
                          flameIndex = flameIndex + 0.5 -- Fast spacing for flames (0.5s)
                          end
                      -- Handle non-tanker explosions, cook-offs, and debris
                      if not effect.isTanker or (effect.explosion and not effect.isTanker) then
                          if effect.explosion then
                              debugMsg("Triggering cargo explosion for " .. effect.name .. " at " .. string.format("%.1f", effect.distance) .. "m with power " .. effect.power .. " scheduled at " .. otherIndex .. "s")
                              timer.scheduleFunction(function(params)
                                  debugMsg("Executing cargo explosion at X: " .. string.format("%.0f", params[1].x) .. ", Y: " .. string.format("%.0f", params[1].y) .. ", Z: " .. string.format("%.0f", params[1].z) .. " with power " .. params[2])
                                  trigger.action.explosion(params[1], params[2])
                              end, {effect.coords, effect.power}, timer.getTime() + otherIndex + 0.1)
                      end
                      if effect.cookOff and effect.cookOffCount > 0 then
                              debugMsg("Scheduling " .. effect.cookOffCount .. " cook-off explosions for " .. effect.name .. " at " .. string.format("%.1f", effect.distance) .. "m over " .. effect.cookOffDuration .. "s starting at " .. otherIndex .. "s")
                          for i = 1, effect.cookOffCount do
                              local delay = effect.cookOffRandomTiming and math.random() * effect.cookOffDuration or (i - 1) * (effect.cookOffDuration / effect.cookOffCount)
                              local basePower = effect.cookOffPower
                              local powerVariation = effect.cookOffPowerRandom / 100
                              local cookOffPower = effect.cookOffPowerRandom == 0 and basePower or basePower * (1 + powerVariation * (math.random() * 2 - 1))
                              debugMsg("Cook-off #" .. i .. " for " .. effect.name .. " at " .. string.format("%.1f", effect.distance) .. "m scheduled at " .. string.format("%.3f", delay) .. "s with power " .. string.format("%.2f", cookOffPower))
                              timer.scheduleFunction(function(params)
                                  debugMsg("Executing cook-off at X: " .. string.format("%.0f", params[1].x) .. ", Y: " .. string.format("%.0f", params[1].y) .. ", Z: " .. string.format("%.0f", params[1].z) .. " with power " .. params[2])
                                  trigger.action.explosion(params[1], params[2])
                                  end, {effect.coords, cookOffPower}, timer.getTime() + otherIndex + delay)
                          end
                          if splash_damage_options.debris_effects then
                              local debrisCount = math.random(splash_damage_options.debris_count_min, splash_damage_options.debris_count_max)
                              for j = 1, debrisCount do
                                  local theta = math.random() * 2 * math.pi
                                  local phi = math.acos(math.random() * 2 - 1)
                                  local minDist = splash_damage_options.debris_max_distance * 0.1
                                  local maxDist = splash_damage_options.debris_max_distance
                                  local r = math.random() * (maxDist - minDist) + minDist
                                  local debrisX = effect.coords.x + r * math.sin(phi) * math.cos(theta)
                                  local debrisZ = effect.coords.z + r * math.sin(phi) * math.sin(theta)
                                  local terrainY = land.getHeight({x = debrisX, y = debrisZ})
                                  local debrisY = terrainY + math.random() * maxDist
                                  local debrisPos = {x = debrisX, y = debrisY, z = debrisZ}
                                  local debrisPower = splash_damage_options.debris_power
                                  local debrisDelay = (j - 1) * (effect.cookOffDuration / debrisCount)
                                  timer.scheduleFunction(function(debrisArgs)
                                      debugMsg("Debris explosion at X: " .. string.format("%.0f", debrisArgs[1].x) .. ", Y: " .. string.format("%.0f", debrisArgs[1].y) .. ", Z: " .. string.format("%.0f", debrisArgs[1].z) .. " with power " .. debrisArgs[2])
                                      trigger.action.explosion(debrisArgs[1], debrisArgs[2])
                                      end, {debrisPos, debrisPower}, timer.getTime() + otherIndex + debrisDelay)
                                  end
                              end
                          end
                          otherIndex = otherIndex + 1 -- Slower spacing for non-flame effects (1s)
                      end
                      processedCargoUnits[unitKey] = true
                      processedUnitsGlobal[unitKey] = true
                  end
              end
              cargoEffectsQueue = {} -- Clear the queue after processing
          end
      end, {initialPos, scanRadius, preExplosionTargets}, timer.getTime() + totalDuration + 1.0)
  end
end

--Flag Checker for mission editor
function checkGiantExplosionFlag()
  for name, target in pairs(giantExplosionTargets) do
      local flagName = name:gsub("GiantExplosionTarget", "GiantExplosionTarget")
      local flagValue = trigger.misc.getUserFlag(flagName)
      --commenting out as it spams every second 
  debugMsg("Checking flag " .. flagName .. ": " .. flagValue)
      if flagValue == 1 then
          debugMsg("Triggering explosion for " .. name .. " at X:" .. target.pos.x .. " Y:" .. target.pos.y .. " Z:" .. target.pos.z)
          triggerGiantExplosion({
              pos = target.pos,
              power = splash_damage_options.giant_explosion_power,
              scale = splash_damage_options.giant_explosion_scale,
              duration = splash_damage_options.giant_explosion_duration,
              count = splash_damage_options.giant_explosion_count
          })
          trigger.action.setUserFlag(flagName, 2)
      end
  end
  return timer.getTime() + splash_damage_options.giant_explosion_poll_rate
end

function getWeaponExplosive(name)
  local weaponData = explTable[name]
  if weaponData then
      return weaponData.explosive, weaponData.shaped_charge
  else
      return 0, false
  end
end

function track_wpns_cluster_scan(args)
  local parentPos = args[1]
  local parentDir = args[2]
  local parentName = args[3]
  local subName = args[4]
  local subCount = args[5]
  local subPower = args[6]
  local parentVel = args[7]
  local attempt = args[8] or 1
  local maxAttempts = 3
  local scanVol = {
      id = world.VolumeType.SPHERE,
      params = { point = parentPos, radius = 400 }
  }
  local bombletsFound = {}
  local allWeaponsFound = {}
  --General scan for all weapons
  world.searchObjects(Object.Category.WEAPON, scanVol, function(wpn)
      if wpn:isExist() then
          local wpnId = wpn.id_
          local wpnType = wpn:getTypeName()
          local wpnPos = wpn:getPosition().p
          table.insert(allWeaponsFound, { id = wpnId, type = wpnType, x = wpnPos.x, y = wpnPos.y, z = wpnPos.z })
          if wpnType == subName and not tracked_weapons[wpnId] then
              tracked_weapons[wpnId] = {
                  wpn = wpn,
                  pos = wpnPos,
                  speed = wpn:getVelocity(),
                  name = wpnType,
                  parent = parentName,
                  parentVelocity = parentVel
              }
              table.insert(bombletsFound, wpnId)
              debugMsg("Detected expected submunition '" .. wpnType .. "' from '" .. parentName .. "' at X: " .. string.format("%.0f", wpnPos.x) .. ", Y: " .. string.format("%.0f", wpnPos.y) .. ", Z: " .. string.format("%.0f", wpnPos.z) .. " (Attempt " .. attempt .. ")")
          end
      end
      return true
  end)
  --Log results
  debugMsg("Scanned for submunition '" .. subName .. "' bomblets from '" .. parentName .. "': " .. #bombletsFound .. " found (Attempt " .. attempt .. ")")
  if #allWeaponsFound > 0 then
      local msg = "General scan for '" .. parentName .. "': " .. #allWeaponsFound .. " bomblets released, expected " .. subCount .. " '" .. subName .. "'"
      local typeMismatch = false
      for _, wpn in ipairs(allWeaponsFound) do
          if wpn.type ~= subName then
              typeMismatch = true
              break
          end
      end
      if typeMismatch then
          msg = msg .. " - Mismatch detected! Actual bomblets: "
          for _, wpn in ipairs(allWeaponsFound) do
              msg = msg .. "'" .. wpn.type .. "' (X: " .. string.format("%.0f", wpn.x) .. ", Y: " .. string.format("%.0f", wpn.y) .. ", Z: " .. string.format("%.0f", wpn.z) .. ") "
          end
          msg = msg .. "Script may need changing."
      end
      debugMsg(msg)
  elseif #bombletsFound == 0 and #allWeaponsFound == 0 then
      debugMsg("No bomblets of any type detected for '" .. parentName .. "' (Attempt " .. attempt .. ")")
  end
  --Retry if no expected submunitions found
  if #bombletsFound == 0 and attempt < maxAttempts then
      debugMsg("No expected submunition '" .. subName .. "' found on attempt " .. attempt .. ", retrying in 0.5s")
      timer.scheduleFunction(track_wpns_cluster_scan, {parentPos, parentDir, parentName, subName, subCount, subPower, parentVel, attempt + 1}, timer.getTime() + 0.5)
  elseif #bombletsFound == 0 and attempt == maxAttempts then
      debugMsg("No submunition '" .. subName .. "' spawned by DCS for '" .. parentName .. "' after " .. maxAttempts .. " attempts - skipping additional explosions")
  end
end

----[[ ##### Updated track_wpns() Function ##### ]]----
local recentExplosions = {}
function track_wpns()
  local weaponsToRemove = {} --Delay removal to ensure all weapons are checked
  for wpn_id_, wpnData in pairs(tracked_weapons) do   
      local status, err = pcall(function()
      if wpnData.wpn:isExist() then
          --Update position, direction, speed
          wpnData.pos = wpnData.wpn:getPosition().p
          wpnData.dir = wpnData.wpn:getPosition().x
          wpnData.speed = wpnData.wpn:getVelocity()
              --[[
      
  
          --Tick-by-tick tracking from weapon's actual position
local tickVol = {
  id = world.VolumeType.SPHERE,
  params = {
                  point = wpnData.pos, --Real weapon position
                  radius = 150 --150m radius
  }
}
local tickTargets = {}
world.searchObjects({Object.Category.UNIT, Object.Category.STATIC}, tickVol, function(obj)
  if obj:isExist() then
      table.insert(tickTargets, {
          name = obj:getTypeName(),
                      distance = getDistance3D(wpnData.pos, obj:getPoint()), --3D distance
          position = obj:getPoint(),
          health = obj:getLife() or 0
      })
              end
              return true
          end)
          debugMsg("Tick Track for " .. wpnData.name .. " at X: " .. string.format("%.0f", wpnData.pos.x) .. ", Y: " .. string.format("%.0f", wpnData.pos.y) .. ", Z: " .. string.format("%.0f", wpnData.pos.z) .. " - " .. #tickTargets .. " targets")
          for i, target in ipairs(tickTargets) do
              debugMsg("Tick Target #" .. i .. ": " .. target.name .. " at X: " .. string.format("%.0f", target.position.x) .. ", Y: " .. string.format("%.0f", target.position.y) .. ", Z: " .. string.format("%.0f", target.position.z) .. ", Dist: " .. string.format("%.1f", target.distance) .. "m, Health: " .. target.health)
          end
 
      
      ]]--

          --Scan potential blast zone in the last frame before impact
          if splash_damage_options.track_pre_explosion then
              local ip = land.getIP(wpnData.pos, wpnData.dir, lookahead(wpnData.speed))
              local predictedImpact = ip or wpnData.pos

              local base_explosive, isShapedCharge = getWeaponExplosive(wpnData.name)
              base_explosive = base_explosive * splash_damage_options.overall_scaling
              if splash_damage_options.rocket_multiplier and wpnData.cat == Weapon.Category.ROCKET then
                  base_explosive = base_explosive * splash_damage_options.rocket_multiplier
              end

              local explosionPower = base_explosive
              if splash_damage_options.apply_shaped_charge_effects and isShapedCharge then
                  explosionPower = explosionPower * splash_damage_options.shaped_charge_multiplier
              end

                  local blastRadius = splash_damage_options.blast_search_radius * 2 --Wider post-scan (180m default)
              if splash_damage_options.use_dynamic_blast_radius then
                  blastRadius = math.pow(explosionPower, 1/3) * 10 * splash_damage_options.dynamic_blast_radius_modifier 
              end

              --Tight scan while weapon exists
              --local tightRadius = 50
              --if splash_damage_options.use_dynamic_blast_radius then
              --tightRadius = math.pow(explosionPower, 1/3) * 5 * splash_damage_options.dynamic_blast_radius_modifier
              --end
      local tightRadius = blastRadius --Use already calculated blastRadius
              local volS = {
                  id = world.VolumeType.SPHERE,
                  params = { 
                      point = wpnData.pos, --Use current pos
                      radius = tightRadius 
                  }
              }
              local tightTargets = {}
              local ifFound = function(foundObject, targets, center)
                  if foundObject:isExist() then
                      local category = foundObject:getCategory()
                      if (category == Object.Category.UNIT and (foundObject:getDesc().category == Unit.Category.GROUND_UNIT or foundObject:getDesc().category == Unit.Category.AIRPLANE)) or
                             category == Object.Category.STATIC then
                              table.insert(targets, {
                                  name = foundObject:getTypeName(),
                                  distance = getDistance(center, foundObject:getPoint()),
                                  health = foundObject:getLife() or 0,
                                  position = foundObject:getPoint(),
                                  maxHealth = (category == Object.Category.UNIT and foundObject:getDesc().life) or foundObject:getLife() or 0,
                                  unit = foundObject
                              })
                      end
                  end
                  return true
              end
                  if splash_damage_options.track_pre_explosion_debug then
              debugMsg("Scanning tight radius " .. tightRadius .. "m at current pos while weapon exists")
                  end
              world.searchObjects({Object.Category.UNIT, Object.Category.STATIC}, volS, function(obj) ifFound(obj, tightTargets, wpnData.pos) end)
              wpnData.tightTargets = tightTargets --Store for impact

              --Wider scan for lastKnownTargets
              volS.params.point = predictedImpact
              volS.params.radius = blastRadius
              local foundTargets = {}
              world.searchObjects({Object.Category.UNIT, Object.Category.STATIC}, volS, function(obj) ifFound(obj, foundTargets, predictedImpact) end)
              wpnData.lastKnownTargets = foundTargets
          end
              --Submunition impact handling
              local weaponData = explTable[wpnData.parent or wpnData.name] or { submunition_name = "unknown" }
              if wpnData.name == weaponData.submunition_name then
                  local groundHeight = land.getHeight({x = wpnData.pos.x, y = wpnData.pos.z})
                  if wpnData.pos.y - groundHeight < 50 then --Impact threshold like old script
                      debugMsg("Submunition '" .. wpnData.name .. "' from '" .. (wpnData.parent or "unknown") .. "' impacted at X: " .. string.format("%.0f", wpnData.pos.x) .. ", Z: " .. string.format("%.0f", wpnData.pos.z))
                      local parentWeaponData = explTable[wpnData.parent] or { submunition_count = 30, submunition_explosive = 1 }
                      local submunitionCount = parentWeaponData.submunition_count or 30
                      local submunitionPower = (parentWeaponData.submunition_explosive or 1) * splash_damage_options.cluster_bomblet_damage_modifier * splash_damage_options.overall_scaling
          if splash_damage_options.cluster_bomblet_reductionmodifier then
            if submunitionCount > 35 then
                              local reductionFactor = (60 - 35) / (247 - 35)
              submunitionCount = 35 + math.floor((submunitionCount - 35) * reductionFactor)
                              if submunitionCount > 60 then submunitionCount = 60 end
            end
          end
      --Use parent velocity if available, else submunition speed
      local parentDir = wpnData.parentVelocity or wpnData.speed
      local dispersionLength, dispersionWidth = calculate_dispersion(parentDir, 2000) --Match original 2000m
      local dirMag = math.sqrt(parentDir.x^2 + parentDir.z^2)
      local dir = dirMag > 0 and {x = parentDir.x / dirMag, z = parentDir.z / dirMag} or {x = 1, z = 0}
                      debugMsg("Simulating " .. submunitionCount .. " bomblets for submunition '" .. wpnData.name .. "' from '" .. (wpnData.parent or "unknown") .. "' over " .. string.format("%.0f", dispersionLength) .. "m x " .. string.format("%.0f", dispersionWidth) .. "m")
                      for i = 1, submunitionCount do
                          local theta = math.random() * 2 * math.pi
                          local r = math.sqrt(math.random())
                          local xOffset = r * dispersionLength * 0.5 * math.cos(theta)
                          local zOffset = r * dispersionWidth * 0.5 * math.sin(theta)
                          local subPos = {
                              x = wpnData.pos.x + (xOffset * dir.x - zOffset * dir.z),
                              z = wpnData.pos.z + (xOffset * dir.z + zOffset * dir.x)
                          }
                          subPos.y = land.getHeight({x = subPos.x, y = subPos.z})
                          debugMsg("Triggering bomblet #" .. i .. " for submunition '" .. wpnData.name .. "' at X: " .. string.format("%.0f", subPos.x) .. ", Z: " .. string.format("%.0f", subPos.z) .. " with power " .. submunitionPower)
                          trigger.action.explosion(subPos, submunitionPower)
                      end
                      table.insert(weaponsToRemove, wpn_id_)
                  end
              end
          else
          --Weapon has impacted
          debugMsg("Weapon " .. wpnData.name .. " no longer exists at " .. timer.getTime() .. "s")
          local ip = land.getIP(wpnData.pos, wpnData.dir, lookahead(wpnData.speed))  --terrain intersection point with weapon's nose.  Only search out 20 meters though.
          local explosionPoint
          if not ip then --use last calculated IP
              explosionPoint = wpnData.pos
          else --use intersection point
              explosionPoint = ip
          end
    local chosenTargets = wpnData.tightTargets or {}
          local safeToBlast = true
    if splash_damage_options.ordnance_protection then
              local checkVol = { id = world.VolumeType.SPHERE, params = { point = explosionPoint, radius = splash_damage_options.ordnance_protection_radius } }
                  debugMsg("Checking ordnance protection for '" .. wpnData.name .. "' at X: " .. explosionPoint.x .. ", Y: " .. explosionPoint.y .. ", Z: " .. explosionPoint.z .. " with radius " .. splash_damage_options.ordnance_protection_radius .. "m")
      world.searchObjects(Object.Category.WEAPON, checkVol, function(obj)
        if obj:isExist() and tracked_weapons[obj.id_] then
          safeToBlast = false
                          debugMsg("Skipping explosion for '" .. wpnData.name .. "' - nearby bomb '" .. tracked_weapons[obj.id_].name .. "' within " .. splash_damage_options.ordnance_protection_radius .. "m")
          return false
        end
        return true
              end)
          end
          if safeToBlast then
                  debugMsg("FinalPos Check for '" .. wpnData.name .. "': X: " .. string.format("%.0f", explosionPoint.x) .. ", Y: " .. string.format("%.0f", explosionPoint.y) .. ", Z: " .. string.format("%.0f", explosionPoint.z) .. ")")
          local base_explosive, isShapedCharge = getWeaponExplosive(wpnData.name)
          base_explosive = base_explosive * splash_damage_options.overall_scaling
          if splash_damage_options.rocket_multiplier and wpnData.cat == Weapon.Category.ROCKET then
              base_explosive = base_explosive * splash_damage_options.rocket_multiplier
          end

          local explosionPower = base_explosive
          if splash_damage_options.apply_shaped_charge_effects and isShapedCharge then
              explosionPower = explosionPower * splash_damage_options.shaped_charge_multiplier
          end

          local blastRadius = splash_damage_options.blast_search_radius * 2 --Wider post-scan (180m default)
          if splash_damage_options.use_dynamic_blast_radius then
                  blastRadius = math.pow(explosionPower, 1/3) * 10 * splash_damage_options.dynamic_blast_radius_modifier
          end


              --Store pre-explosion state of all tracked weapons for detection
              local preExplosionWeapons = {}
              if splash_damage_options.ordnance_protection and splash_damage_options.detect_ordnance_destruction and splash_damage_options.larger_explosions then
                  for id, data in pairs(tracked_weapons) do
                      if data.wpn:isExist() then
                          preExplosionWeapons[id] = {
                              name = data.name,
                              pos = data.wpn:getPosition().p,
                                  distance = getDistance3D(explosionPoint, data.wpn:getPosition().p),
                                  explosive = getWeaponExplosive(data.name) --Store the explosive power
                          }
                      end
                  end
              end
    --Cluster Bomb Handling
    local weaponData = explTable[wpnData.name] or { explosive = 0, shaped_charge = false }
    local isCluster = weaponData.cluster or false
    if splash_damage_options.cluster_enabled and isCluster then
      local submunitionCount = weaponData.submunition_count or 30
      local submunitionPower = (weaponData.submunition_explosive or 1) * splash_damage_options.cluster_bomblet_damage_modifier * splash_damage_options.overall_scaling
                      local submunitionName = weaponData.submunition_name or "unknown"
      --Apply bomblet reduction logic if enabled
      if splash_damage_options.cluster_bomblet_reductionmodifier then
                          if submunitionCount > 35 then
                              local reductionFactor = (60 - 35) / (247 - 35)
            submunitionCount = 35 + math.floor((submunitionCount - 35) * reductionFactor)
            if submunitionCount > 60 then submunitionCount = 60 end --Cap at 60
          end
        end
                      --Extended scan with general bomblet detection
                  timer.scheduleFunction(track_wpns_cluster_scan, {explosionPoint, wpnData.dir, wpnData.name, submunitionName, submunitionCount, submunitionPower, wpnData.speed}, timer.getTime() + 0.3)
    else
      --Standard explosion handling
      if splash_damage_options.larger_explosions then
                          debugMsg("Triggering initial explosion for '" .. wpnData.name .. "' at power " .. explosionPower)
        trigger.action.explosion(explosionPoint, explosionPower)
                          table.insert(recentExplosions, { pos = explosionPoint, time = timer.getTime(), radius = blastRadius })
                          debugMsg("Added to recentExplosions for '" .. wpnData.name .. "': X: " .. explosionPoint.x .. ", Y: " .. explosionPoint.y .. ", Z: " .. explosionPoint.z .. ", Time: " .. timer.getTime())
      end
      blastWave(explosionPoint, splash_damage_options.blast_search_radius, wpnData.ordnance, explosionPower, isShapedCharge)
    end
          --detect_ordnance_destruction comes before recent_large_explosion_snap in original
          if splash_damage_options.ordnance_protection and splash_damage_options.detect_ordnance_destruction and splash_damage_options.larger_explosions then
              timer.scheduleFunction(function(args)
                  local explosionPoint = args[1]
                  local blastRadius = args[2]
                  local triggeringWeapon = args[3]
                  local preExplosionWeapons = args[4]
                  for id, preData in pairs(preExplosionWeapons) do
                      if tracked_weapons[id] and not tracked_weapons[id].wpn:isExist() then
                          if preData.distance <= blastRadius then
                              local msg = "WARNING: " .. preData.name .. " destroyed by large explosion from " .. triggeringWeapon .. " at " .. string.format("X: %.0f, Y: %.0f, Z: %.0f", explosionPoint.x, explosionPoint.y, explosionPoint.z)
                              gameMsg(msg)
                              debugMsg(msg)
                              env.info(msg)
                              if splash_damage_options.snap_to_ground_if_destroyed_by_large_explosion then
                                  local groundPos = {
                                      x = preData.pos.x,
                                      y = land.getHeight({x = preData.pos.x, y = preData.pos.z}),
                                      z = preData.pos.z
                                  }
                                  local destroyedWeaponPower, isShapedCharge = preData.explosive
                                  destroyedWeaponPower = destroyedWeaponPower * splash_damage_options.overall_scaling
                                  if splash_damage_options.rocket_multiplier and tracked_weapons[id].cat == Weapon.Category.ROCKET then
                                      destroyedWeaponPower = destroyedWeaponPower * splash_damage_options.rocket_multiplier
                                  end
                                  if splash_damage_options.apply_shaped_charge_effects and isShapedCharge then
                                      destroyedWeaponPower = destroyedWeaponPower * splash_damage_options.shaped_charge_multiplier
                                  end
                                  debugMsg("Triggering ground explosion for destroyed " .. preData.name .. " (detect_ordnance_destruction) at X: " .. string.format("%.0f", groundPos.x) .. ", Y: " .. string.format("%.0f", groundPos.y) .. ", Z: " .. string.format("%.0f", groundPos.z) .. " with power " .. destroyedWeaponPower)
                                  trigger.action.explosion(groundPos, destroyedWeaponPower)
                              end
                          end
                      end
                  end
              end, {explosionPoint, blastRadius, wpnData.name, preExplosionWeapons}, timer.getTime() + 0.2)
          end
          --recent_large_explosion_snap comes after main explosion and detect_ordnance_destruction
                  if splash_damage_options.ordnance_protection and splash_damage_options.larger_explosions and splash_damage_options.recent_large_explosion_snap and splash_damage_options.snap_to_ground_if_destroyed_by_large_explosion then
                      local currentTime = timer.getTime()
                      for id, data in pairs(tracked_weapons) do
                          if id ~= wpn_id_ and not data.wpn:isExist() then
                              local terrainHeight = land.getHeight({x = data.pos.x, y = data.pos.z})
            local weaponHeight = data.pos.y - terrainHeight --Calculate height above ground
            local isMidAir = weaponHeight > 5 --Still checks if above ground
                                  local snapTriggered = false
                                  for _, explosion in ipairs(recentExplosions) do
                                      local timeDiff = currentTime - explosion.time
                                      local distance = getDistance3D(data.pos, explosion.pos)
                                      debugMsg("Checking " .. data.name .. " at X: " .. data.pos.x .. ", Y: " .. data.pos.y .. ", Z: " .. data.pos.z .. " against explosion at X: " .. explosion.pos.x .. ", Y: " .. explosion.pos.y .. ", Z: " .. explosion.pos.z .. " - Distance: " .. distance .. "m, TimeDiff: " .. timeDiff .. "s")
                                      if timeDiff <= splash_damage_options.recent_large_explosion_time and distance <= splash_damage_options.recent_large_explosion_range then
                          if isMidAir and weaponHeight <= splash_damage_options.max_snapped_height then --New height check
                                              local groundPos = { x = data.pos.x, y = terrainHeight, z = data.pos.z }
                                              local destroyedWeaponPower, isShapedCharge = getWeaponExplosive(data.name)
                                              destroyedWeaponPower = destroyedWeaponPower * splash_damage_options.overall_scaling
                                              if splash_damage_options.rocket_multiplier and data.cat == Weapon.Category.ROCKET then
                                                  destroyedWeaponPower = destroyedWeaponPower * splash_damage_options.rocket_multiplier
                                              end
                                              if splash_damage_options.apply_shaped_charge_effects and isShapedCharge then
                                                  destroyedWeaponPower = destroyedWeaponPower * splash_damage_options.shaped_charge_multiplier
                                              end
                      debugMsg("Weapon " .. data.name .. " detected recent large explosion within " .. splash_damage_options.recent_large_explosion_range .. "m and " .. splash_damage_options.recent_large_explosion_time .. "s, snapping to ground at X: " .. string.format("%.0f", groundPos.x) .. ", Y: " .. string.format("%.0f", groundPos.y) .. ", Z: " .. string.format("%.0f", groundPos.z) .. " with power " .. destroyedWeaponPower .. " (Height: " .. string.format("%.0f", weaponHeight) .. "m)")
                                              trigger.action.explosion(groundPos, destroyedWeaponPower)
                                              snapTriggered = true
                                              table.insert(weaponsToRemove, id)
                                              break
                  elseif isMidAir then
                      debugMsg("Weapon " .. data.name .. " destroyed above max_snapped_height (" .. splash_damage_options.max_snapped_height .. "m) at " .. string.format("%.0f", weaponHeight) .. "m, skipping snap")
                                          else
                                              debugMsg("Weapon " .. data.name .. " impacted ground within recent_large_explosion_range (" .. splash_damage_options.recent_large_explosion_range .. "m) and time (" .. splash_damage_options.recent_large_explosion_time .. "s), no snap needed")
                                  snapTriggered = true
                                              break
                                          end
                                      end
                                  end
                                  if not snapTriggered then
                                      if isMidAir then
                              debugMsg("Weapon " .. data.name .. " destroyed in air, but no recent large explosion within " .. splash_damage_options.recent_large_explosion_range .. "m or " .. splash_damage_options.recent_large_explosion_time .. "s")
                                      else
                                          debugMsg("Weapon " .. data.name .. " impacted ground, not processed by recent large explosion settings")
                                      end
                                  end
                          end
                      end
                      local newExplosions = {}
                      for _, explosion in ipairs(recentExplosions) do
                          if currentTime - explosion.time <= splash_damage_options.recent_large_explosion_time then
                              table.insert(newExplosions, explosion)
                          end
                      end
                      recentExplosions = newExplosions
                  end
              --Mark units as destroyed to avoid MiST accessing them
              local destroyedUnits = {}
              for _, target in ipairs(chosenTargets) do
                  if target.unit:isExist() and target.health > 0 and target.unit:getLife() <= 0 then
                      destroyedUnits[target.name] = true
                      debugMsg("Marked " .. target.name .. " as destroyed pre-impact")
                  end
              end
              --Schedule explosion handling with original 0.1-second delay, enhanced error handling
          timer.scheduleFunction(function(args)
              local finalPos = args[1]
              local explosionPoint = args[2]
              local explosionPower = args[3]
              local isShapedCharge = args[4]
              local blastRadius = args[5]
              local chosenTargets = args[6]
              local weaponName = args[7]
              local wpnData = args[8]
      if splash_damage_options.debug then
                  debugMsg("Starting impact handling for " .. weaponName .. " at " .. timer.getTime() .. "s")
      end
                  local status, err = pcall(function()
              --Log pre-explosion targets
              if splash_damage_options.track_pre_explosion then
                  if #chosenTargets > 0 then
                      local msg = "Targets in blast zone for " .. weaponName .. " BEFORE explosion (last frame, using finalPos):\n"
                      for i, target in ipairs(chosenTargets) do
                          msg = msg .. "- " .. target.name .. " (Dist: " .. string.format("%.1f", target.distance) .. "m, Health: " .. target.health .. ")\n"
                      end
                      debugMsg(msg)
                      env.info("SplashDamage Pre-Explosion (Last Frame): " .. msg)
                  else
                      debugMsg("No targets in blast zone for " .. weaponName .. " BEFORE explosion (last frame)")
                      env.info("SplashDamage Pre-Explosion (Last Frame): No targets in blast zone for " .. weaponName)
                  end
              end

              blastWave(explosionPoint, splash_damage_options.blast_search_radius, wpnData.ordnance, explosionPower, isShapedCharge)

              --Post-explosion analysis and queue cargo effects
              if splash_damage_options.track_pre_explosion then
                  timer.scheduleFunction(function(innerArgs)
                      local impactPoint = innerArgs[1]
                      local blastRadius = innerArgs[2]
                      local preExplosionTargets = innerArgs[3] or {}
                      local weaponName = innerArgs[4]
                      local weaponPower = innerArgs[5]
              if splash_damage_options.debug == true then
                              debugMsg("Starting post-explosion analysis for " .. weaponName .. " at " .. timer.getTime() .. "s")
            end

                      --Scan all units in wider radius
                      local postExplosionTargets = {}
                      local volS = {
                          id = world.VolumeType.SPHERE,
                          params = {
                              point = impactPoint,
                              radius = blastRadius
                          }
                      }

                      local ifFound = function(foundObject)
                          if foundObject:isExist() then
                              local category = foundObject:getCategory()
                              if (category == Object.Category.UNIT and (foundObject:getDesc().category == Unit.Category.GROUND_UNIT or foundObject:getDesc().category == Unit.Category.AIRPLANE)) or
                                     category == Object.Category.STATIC then
                                      table.insert(postExplosionTargets, {
                                          name = foundObject:getTypeName(),
                                          health = foundObject:getLife() or 0,
                                          position = foundObject:getPoint(),
                                          maxHealth = (category == Object.Category.UNIT and foundObject:getDesc().life) or foundObject:getLife() or 0
                                      })
                                          end
                              end
                          return true
                      end

                      world.searchObjects({Object.Category.UNIT, Object.Category.STATIC}, volS, ifFound)

                      local msg = "Post-explosion analysis for " .. weaponName .. ":\n"

                      --Match pre-detected units
                      for _, preTarget in ipairs(preExplosionTargets) do
                          local found = false
                          local postHealth = 0
                          local postPosition = nil
                          for _, postTarget in ipairs(postExplosionTargets) do
                              if preTarget.name == postTarget.name and getDistance(preTarget.position, postTarget.position) < 1 then
                                  found = true
                                  postHealth = postTarget.health
                                  postPosition = postTarget.position
                                  break
                              end
                          end

                          local healthPercent = preTarget.maxHealth > 0 and (postHealth / preTarget.maxHealth * 100) or 0
                          local status = ""

                          if not found or postHealth <= 0 then
                              status = "WAS FULLY DESTROYED"
                          elseif healthPercent < splash_damage_options.cargo_damage_threshold then
                              status = "WAS DAMAGED BELOW THRESHOLD"
                          else
                              status = "SURVIVED (Health: " .. postHealth .. ")"
                          end

                          --Always include coords in status message
                          local coords = found and postPosition or preTarget.position
                          local statusMsg = status .. " AT " .. string.format("X: %.0f, Y: %.0f, Z: %.0f", coords.x, coords.y, coords.z) .. " (Pre: " .. preTarget.health .. ", Post: " .. postHealth .. ")"
                          --Check if target is in cargoUnits and within blast radius
                          local cargoData = cargoUnits[preTarget.name]
                          if cargoData and preTarget.distance <= blastRadius and 
                             (not found or postHealth <= 0 or healthPercent < splash_damage_options.cargo_damage_threshold) then

                              if splash_damage_options.enable_cargo_effects then
                                              local cargoPower = cargoData.cargoExplosionPower or weaponPower --Use fixed power or fallback
                                  table.insert(cargoEffectsQueue, {
                                      name = preTarget.name,
                                      distance = preTarget.distance,
                                      coords = coords,
                                      power = cargoPower,
                                      explosion = cargoData.cargoExplosion,
                                      cookOff = cargoData.cargoCookOff,
                                      cookOffCount = cargoData.cookOffCount,
                                      cookOffPower = cargoData.cookOffPower,
                                      cookOffDuration = cargoData.cookOffDuration,
                                      cookOffRandomTiming = cargoData.cookOffRandomTiming,
                                      cookOffPowerRandom = cargoData.cookOffPowerRandom,
                                      isTanker = cargoData.isTanker,
                                      flameSize = cargoData.flameSize,
                                      flameDuration = cargoData.flameDuration
                                  })
                                  statusMsg = statusMsg .. " WITH CARGO EXPLOSION (Power: " .. cargoPower .. ")"
                                  if cargoData.cargoCookOff and cargoData.cookOffCount > 0 then
                                      statusMsg = statusMsg .. " WITH COOK-OFF (" .. cargoData.cookOffCount .. " blasts over " .. cargoData.cookOffDuration .. "s)"
                                  end
                              end
                          end

                          msg = msg .. "- " .. preTarget.name .. " " .. statusMsg .. "\n"
                      end
                      --Check for additional units
                      for _, postTarget in ipairs(postExplosionTargets) do
                          local isPreDetected = false
                          for _, preTarget in ipairs(preExplosionTargets) do
                              if preTarget.name == postTarget.name and getDistance(preTarget.position, postTarget.position) < 1 then
                                  isPreDetected = true
                                  break
                              end
                          end
                          if not isPreDetected then
                              local coords = postTarget.position
                              local healthPercent = postTarget.maxHealth > 0 and (postTarget.health / postTarget.maxHealth * 100) or 0
                              local status = postTarget.health <= 0 and "WAS FULLY DESTROYED" or 
                                            (healthPercent < splash_damage_options.cargo_damage_threshold and "WAS DAMAGED BELOW THRESHOLD" or 
                                            "SURVIVED (Health: " .. postTarget.health .. ")")
                              local statusMsg = status .. " AT " .. string.format("X: %.0f, Y: %.0f, Z: %.0f", coords.x, coords.y, coords.z) .. " (Pre: Unknown, Post: " .. postTarget.health .. ")"
                              local cargoData = cargoUnits[postTarget.name]
                              if cargoData and (postTarget.health <= 0 or healthPercent < splash_damage_options.cargo_damage_threshold) then
                                  if splash_damage_options.enable_cargo_effects then
                                                  local cargoPower = cargoData.cargoExplosionPower or weaponPower --Use fixed power or fallback
                                  local distance = getDistance(impactPoint, coords)
                                  table.insert(cargoEffectsQueue, {
                                      name = postTarget.name,
                                      distance = distance,
                                      coords = coords,
                                      power = cargoPower,
                                      explosion = cargoData.cargoExplosion,
                                      cookOff = cargoData.cargoCookOff,
                                      cookOffCount = cargoData.cookOffCount,
                                      cookOffPower = cargoData.cookOffPower,
                                      cookOffDuration = cargoData.cookOffDuration,
                                      cookOffRandomTiming = cargoData.cookOffRandomTiming,
                                      cookOffPowerRandom = cargoData.cookOffPowerRandom,
                                      isTanker = cargoData.isTanker,
                                      flameSize = cargoData.flameSize,
                                      flameDuration = cargoData.flameDuration
                                  })
                                  statusMsg = statusMsg .. " WITH CARGO EXPLOSION (Power: " .. cargoPower .. ")"
                                              if cargoData.cargoCookOff and cargoData.cookOffCount > 0 then
                                      statusMsg = statusMsg .. " WITH COOK-OFF (" .. cargoData.cookOffCount .. " blasts over " .. cargoData.cookOffDuration .. "s)"
                                      end
                                  end
                              end
                              msg = msg .. "- " .. postTarget.name .. " " .. statusMsg .. "\n"
                          end
                      end

                      --Schedule all queued cargo effects
                      if #cargoEffectsQueue > 0 then
                          local effectIndex = 0
                              local processedCargoUnits = {} --Track processed units
  local flamePositions = {} --Track flame coords with 3m radius
                          for _, effect in ipairs(cargoEffectsQueue) do
                                  local unitKey = effect.name .. "_" .. effect.coords.x .. "_" .. effect.coords.z
                                  if not processedUnitsGlobal[unitKey] and not processedCargoUnits[unitKey] then
                              if effect.explosion then
                                  debugMsg("Triggering cargo explosion for " .. effect.name .. " at " .. string.format("%.1f", effect.distance) .. "m with power " .. effect.power .. " scheduled at " .. effectIndex .. "s")
                                  timer.scheduleFunction(function(params)
                  debugMsg("Executing cargo explosion at X: " .. string.format("%.0f", params[1].x) .. ", Y: " .. string.format("%.0f", params[1].y) .. ", Z: " .. string.format("%.0f", params[1].z) .. " with power " .. params[2])
                                      trigger.action.explosion(params[1], params[2])
              end, {effect.coords, effect.power}, timer.getTime() + effectIndex + 0.1) --Slight delay for visibility
                                  if effect.isTanker then
                                      local flameSize = effect.flameSize or 3
                        local flameDuration = effect.flameDuration --Use cargoUnits value directly, no default
                                      local flameDensity = 1.0 --Max density for visibility
                                      local effectId = effectSmokeId
                                      effectSmokeId = effectSmokeId + 1
                  --Check for nearby flames within 3m
                  local isDuplicate = false
                  for _, pos in pairs(flamePositions) do
                      if getDistance3D(effect.coords, pos) < 3 then
                          isDuplicate = true
                          debugMsg("Skipping duplicate flame for " .. effect.name .. " near X: " .. string.format("%.0f", pos.x) .. ", Y: " .. string.format("%.0f", pos.y) .. ", Z: " .. string.format("%.0f", pos.z))
                          break
                      end
                  end
                  if not isDuplicate then
                                      debugMsg("Adding flame effect for tanker " .. effect.name .. " at " .. string.format("%.1f", effect.distance) .. "m (Size: " .. flameSize .. ", Duration: " .. flameDuration .. "s, ID: " .. effectId .. ") scheduled at " .. effectIndex .. "s")
                                      timer.scheduleFunction(function(params)
                                                  --Adjust Y-coordinate to terrain height + offset
                                                  local terrainHeight = land.getHeight({x = params[1].x, y = params[1].z})
                                                  local adjustedCoords = {x = params[1].x, y = terrainHeight + 2, z = params[1].z}
                                                          debugMsg("Spawning flame effect at X: " .. string.format("%.0f", adjustedCoords.x) .. ", Y: " .. string.format("%.0f", adjustedCoords.y) .. ", Z: " .. string.format("%.0f", adjustedCoords.z))
          trigger.action.explosion(adjustedCoords, 10) --Small explosion to force visibility
                                                  trigger.action.effectSmokeBig(adjustedCoords, params[2], params[3], params[4])
                      end, {effect.coords, flameSize, flameDensity, effectId}, timer.getTime() + effectIndex + 0.2) --Slight delay
                                      timer.scheduleFunction(function(id)
                                          debugMsg("Stopping flame effect for " .. effect.name .. " (ID: " .. id .. ")")
                                          trigger.action.effectSmokeStop(id)
                      end, effectId, timer.getTime() + effectIndex + flameDuration + 0.2)
                      table.insert(flamePositions, effect.coords)
                                              end
                                  end
                              end
                                          debugMsg("Checking cook-off for " .. effect.name .. ": cookOff=" .. tostring(effect.cookOff) .. ", count=" .. tostring(effect.cookOffCount))
        if effect.cookOff and effect.cookOffCount > 0 then
          debugMsg("Scheduling " .. effect.cookOffCount .. " cook-off explosions for " .. effect.name .. " at " .. string.format("%.1f", effect.distance) .. "m over " .. effect.cookOffDuration .. "s starting at " .. effectIndex .. "s")
          for i = 1, effect.cookOffCount do
            local delay = effect.cookOffRandomTiming and math.random() * effect.cookOffDuration or (i - 1) * (effect.cookOffDuration / effect.cookOffCount)
            local basePower = effect.cookOffPower
            local powerVariation = effect.cookOffPowerRandom / 100
            local cookOffPower = effect.cookOffPowerRandom == 0 and basePower or basePower * (1 + powerVariation * (math.random() * 2 - 1))
            debugMsg("Cook-off #" .. i .. " for " .. effect.name .. " at " .. string.format("%.1f", effect.distance) .. "m scheduled at " .. string.format("%.3f", delay) .. "s with power " .. string.format("%.2f", cookOffPower))
            timer.scheduleFunction(function(params)
              local pos = params[1]
              local power = params[2]
              debugMsg("Executing cook-off at " .. string.format("X: %.0f, Y: %.0f, Z: %.0f", pos.x, pos.y, pos.z) .. " with power " .. power)
              trigger.action.explosion(pos, power)
            end, {effect.coords, cookOffPower}, timer.getTime() + effectIndex + delay)
          end
          --Debris burst only if cook-off is true and enabled
          if splash_damage_options.debris_effects then
            local debrisCount = math.random(splash_damage_options.debris_count_min, splash_damage_options.debris_count_max)
            for j = 1, debrisCount do
              --Random spherical offset
              local theta = math.random() * 2 * math.pi --Horizontal angle
              local phi = math.acos(math.random() * 2 - 1) --Vertical angle for sphere
              local minDist = splash_damage_options.debris_max_distance * 0.1 --10% of max
              local maxDist = splash_damage_options.debris_max_distance
              local r = math.random() * (maxDist - minDist) + minDist --10% to full max distance
              local debrisX = effect.coords.x + r * math.sin(phi) * math.cos(theta)
              local debrisZ = effect.coords.z + r * math.sin(phi) * math.sin(theta)
              local terrainY = land.getHeight({x = debrisX, y = debrisZ})
              local debrisY = terrainY + math.random() * maxDist --0 to max_distance above ground
              local debrisPos = {x = debrisX, y = debrisY, z = debrisZ}
              local debrisPower = splash_damage_options.debris_power
              local debrisDelay = (j - 1) * (effect.cookOffDuration / debrisCount) --Spread over cook-off duration
              timer.scheduleFunction(function(debrisArgs)
                local dPos = debrisArgs[1]
                local dPower = debrisArgs[2]
                debugMsg("Debris explosion at X: " .. string.format("%.0f", dPos.x) .. ", Y: " .. string.format("%.0f", dPos.y) .. ", Z: " .. string.format("%.0f", dPos.z) .. " with power " .. dPower)
                trigger.action.explosion(dPos, dPower)
              end, {debrisPos, debrisPower}, timer.getTime() + effectIndex + debrisDelay)
            end
          end
        end


                                      processedCargoUnits[unitKey] = true
                                      processedUnitsGlobal[unitKey] = true
            effectIndex = effectIndex + 3  --3 secs spacing if not random
                          end
                              end
                          --Clear the queue after scheduling
                          cargoEffectsQueue = {}
                      end

                      debugMsg(msg)
                      env.info("SplashDamage Post-Explosion: " .. msg)
                  end, {finalPos, blastRadius, chosenTargets, weaponName, explosionPower}, timer.getTime() + 1)
                      end
                  end)
                  if not status then
                          debugMsg("Impact handling error for '" .. weaponName .. "': " .. err)
                      end
                  end, {explosionPoint, explosionPoint, explosionPower, isShapedCharge, blastRadius, chosenTargets, wpnData.name, wpnData}, timer.getTime() + 0.1)
              else
                  debugMsg("Explosion skipped due to ordnance protection for '" .. wpnData.name .. "'")
                  if splash_damage_options.larger_explosions then
                      table.insert(recentExplosions, { pos = explosionPoint, time = timer.getTime(), radius = blastRadius })
                      debugMsg("Skipped explosion logged for snap check for '" .. wpnData.name .. "': X: " .. explosionPoint.x .. ", Y: " .. explosionPoint.y .. ", Z: " .. explosionPoint.z .. ", Time: " .. timer.getTime())
                  end
              end
              table.insert(weaponsToRemove, wpn_id_)
          end
      end)
      if not status then
          debugMsg("Error in track_wpns for '" .. (wpnData.name or "unknown weapon") .. "': " .. err)
      end
  end
  --Perform all removals after iteration
  for _, id in ipairs(weaponsToRemove) do
      tracked_weapons[id] = nil
end
  return timer.getTime() + refreshRate
end
function onWpnEvent(event)
      if event.id == world.event.S_EVENT_SHOT then
      if event.weapon then
              local ordnance = event.weapon
              local typeName = trim(ordnance:getTypeName())
          if splash_damage_options.debug then
              env.info("Weapon fired: [" .. typeName .. "]")
              debugMsg("Weapon fired: [" .. typeName .. "]")
          end
              if string.find(typeName, "weapons.shells") then 
              if splash_damage_options.debug then
                  debugMsg("Event shot, but not tracking: " .. typeName)
              env.info("SplashDamage: event shot, but not tracking: " .. typeName .. " (" .. event.initiator:getTypeName() .. ")")
  end
                  return
              end

          --Check if weapon is in explTable before tracking
              if not explTable[typeName] then
              env.info("SplashDamage: " .. typeName .. " missing from script (" .. event.initiator:getTypeName() .. ")")
                  if splash_damage_options.weapon_missing_message == true then
                      trigger.action.outText("SplashDamage: " .. typeName .. " missing from script (" .. (event.initiator and event.initiator:isExist() and event.initiator:getTypeName() or "no initiator") .. ")", 3)
--                if mist and mist.utils and mist.utils.tableShow then --Only if MiST is present
--                     local success, desc = pcall(mist.utils.tableShow, ordnance:getDesc())
--                     if success then
--                          debugMsg("desc for [" .. typeName .. "]: " .. desc)
--                     else
--                         debugMsg("Could not retrieve description for [" .. typeName .. "]. Object may no longer exist.")
--                     end
--                  end
                      env.info("Current keys in explTable:")
                      for k, v in pairs(explTable) do
                          env.info("Key: [" .. k .. "]")
                      end
                  end
              return --Skip tracking this weapon since its not in the table
              end

          if (ordnance:getDesc().category ~= 0) and event.initiator then
                  if ordnance:getDesc().category == 1 then
                      if (ordnance:getDesc().MissileCategory ~= 1 and ordnance:getDesc().MissileCategory ~= 2) then
                      tracked_weapons[event.weapon.id_] = { wpn = ordnance, init = event.initiator:getName(), pos = ordnance:getPoint(), dir = ordnance:getPosition().x, name = typeName, speed = ordnance:getVelocity(), cat = ordnance:getCategory() }
                  end
              else
                  tracked_weapons[event.weapon.id_] = { wpn = ordnance, init = event.initiator:getName(), pos = ordnance:getPoint(), dir = ordnance:getPosition().x, name = typeName, speed = ordnance:getVelocity(), cat = ordnance:getCategory() }
              end
          end
      end
  end
end

local function protectedCall(...)
  local status, retval = pcall(...)
  if not status then
      env.warning("Splash damage script error... gracefully caught! " .. retval, true)
  end
end

function WpnHandler:onEvent(event)
  protectedCall(onWpnEvent, event)
end

function explodeObject(args)
  local point = args[1]
  local distance = args[2]
  local power = args[3]
  trigger.action.explosion(point, power)
end

function blastWave(_point, _radius, weapon, power, isShapedCharge)
  if isShapedCharge then
      _radius = _radius * splash_damage_options.shaped_charge_multiplier
  end
  if splash_damage_options.use_dynamic_blast_radius then
      local dynamicRadius = math.pow(power, 1/3) * 5 * splash_damage_options.dynamic_blast_radius_modifier
      if isShapedCharge then
          _radius = dynamicRadius * splash_damage_options.shaped_charge_multiplier
      else
          _radius = dynamicRadius
      end
  end
  
  local foundUnits = {}
  local volS = {
      id = world.VolumeType.SPHERE,
      params = {
          point = _point,
          radius = _radius
      }
  }

  local ifFound = function(foundObject, val)
      if foundObject:getDesc().category == Unit.Category.GROUND_UNIT and foundObject:getCategory() == Object.Category.UNIT then
          foundUnits[#foundUnits + 1] = foundObject
      end
      if foundObject:getDesc().category == Unit.Category.GROUND_UNIT then
          if splash_damage_options.blast_stun == true then
              --suppressUnit(foundObject, 2, weapon)
          end
      end
      if splash_damage_options.wave_explosions then
          local obj = foundObject
          local obj_location = obj:getPoint()
          local dist = getDistance(_point, obj_location)
          local timing = dist / 500
          if obj:isExist() and tableHasKey(obj:getDesc(), "box") then
              local length = (obj:getDesc().box.max.x + math.abs(obj:getDesc().box.min.x))
              local height = (obj:getDesc().box.max.y + math.abs(obj:getDesc().box.min.y))
              local depth = (obj:getDesc().box.max.z + math.abs(obj:getDesc().box.min.z))
              local _length = length
              local _depth = depth
              if depth > length then 
                  _length = depth 
                  _depth = length
              end
              local surface_distance = dist - _depth / 2
              local scaled_power_factor = 0.006 * power + 1
              local intensity = (power * scaled_power_factor) / (4 * math.pi * surface_distance^2)
              local surface_area = _length * height
              local damage_for_surface = intensity * surface_area
              if damage_for_surface > splash_damage_options.cascade_damage_threshold then
                  local explosion_size = damage_for_surface
                  if obj:getDesc().category == Unit.Category.STRUCTURE then
                      explosion_size = intensity * splash_damage_options.static_damage_boost
                  end
                  if explosion_size > power then explosion_size = power end
                  local triggerExplosion = false
                  if splash_damage_options.always_cascade_explode then
                      triggerExplosion = true
                  else
                      if obj:getDesc().life then
                          local healthPercent = (obj:getLife() / obj:getDesc().life) * 100
                          if healthPercent <= splash_damage_options.cascade_explode_threshold then
                              triggerExplosion = true
                          end
                          --Queue cargo effects for units below
                          local cargoData = cargoUnits[obj:getTypeName()]
                          if cargoData and healthPercent <= splash_damage_options.cargo_damage_threshold and splash_damage_options.enable_cargo_effects then
                              local cargoPower = power * cargoData.cargoExplosionMult
                              table.insert(cargoEffectsQueue, {
                                  name = obj:getTypeName(),
                                  distance = dist,
                                  coords = obj_location,
                                  power = cargoPower,
                                  explosion = cargoData.cargoExplosion,
                                  cookOff = cargoData.cargoCookOff,
                                  cookOffCount = cargoData.cookOffCount,
                                  cookOffPower = cargoData.cookOffPower,
                                  cookOffDuration = cargoData.cookOffDuration,
                                  cookOffRandomTiming = cargoData.cookOffRandomTiming,
                                  cookOffPowerRandom = cargoData.cookOffPowerRandom,
                                  isTanker = cargoData.isTanker,
                                  flameSize = cargoData.flameSize,
                                  flameDuration = cargoData.flameDuration
                              })
                          end
                      else
                          triggerExplosion = true
                      end
                      if not triggerExplosion and obj:getDesc().category == Unit.Category.GROUND_UNIT then
                          local health = obj:getLife() or 0
                          if health <= 0 then
                              triggerExplosion = true
                          end
                      end
                  end
                  if triggerExplosion then
                      timer.scheduleFunction(explodeObject, {obj_location, dist, explosion_size * splash_damage_options.cascade_scaling}, timer.getTime() + timing)
                  end
              end
          end
      end
      return true
  end

  world.searchObjects(Object.Category.UNIT, volS, ifFound)
  world.searchObjects(Object.Category.STATIC, volS, ifFound)
  world.searchObjects(Object.Category.SCENERY, volS, ifFound)
  world.searchObjects(Object.Category.CARGO, volS, ifFound)
  if splash_damage_options.damage_model then
      timer.scheduleFunction(modelUnitDamage, foundUnits, timer.getTime() + 1.5)
  end
end

function modelUnitDamage(units)
  for i, unit in ipairs(units) do
      if unit:isExist() then
          local health = (unit:getLife() / unit:getDesc().life) * 100
          if unit:hasAttribute("Infantry") and health > 0 then
              if health <= splash_damage_options.infantry_cant_fire_health then
                  unit:getController():setOption(AI.Option.Ground.id.ROE, AI.Option.Ground.val.ROE.WEAPON_HOLD)
              end
          end
          if unit:getDesc().category == Unit.Category.GROUND_UNIT and (not unit:hasAttribute("Infantry")) and health > 0 then
              if health <= splash_damage_options.unit_cant_fire_health then
                  unit:getController():setOption(AI.Option.Ground.id.ROE, AI.Option.Ground.val.ROE.WEAPON_HOLD)
                  gameMsg(unit:getTypeName() .. " weapons disabled")
              end
              if health <= splash_damage_options.unit_disabled_health and health > 0 then
                  unit:getController():setTask({id = 'Hold', params = {}})
                  unit:getController():setOnOff(false)
                  gameMsg(unit:getTypeName() .. " disabled")
              end
          end
      end
  end
end

function updateSplashDamageSetting(setting, increment)
  if not splash_damage_options[setting] then
      env.info("Error: Setting " .. setting .. " does not exist.")
      return
  end

  local newValue = math.max(0, splash_damage_options[setting] + increment)
  env.info("Updating " .. setting .. " from " .. tostring(splash_damage_options[setting]) .. " to " .. tostring(newValue))
  splash_damage_options[setting] = newValue
  trigger.action.outText("Updated " .. setting .. " to: " .. tostring(splash_damage_options[setting]), 5)
end

function toggleSplashDamageSetting(setting)
  splash_damage_options[setting] = not splash_damage_options[setting]
  trigger.action.outText("Toggled " .. setting .. " to: " .. tostring(splash_damage_options[setting]), 5)

  if setting == "enable_radio_menu" then
      if splash_damage_options.enable_radio_menu then
          addSplashDamageMenu()
      else
          missionCommands.removeItem(splash_damage_menu)
          splash_damage_menu = nil
      end
  end
end

function addValueAdjustmentCommands(menu, setting)
  missionCommands.addCommand("+0.1", menu, updateSplashDamageSetting, setting, 0.1)
  missionCommands.addCommand("+1", menu, updateSplashDamageSetting, setting, 1)
  missionCommands.addCommand("+10", menu, updateSplashDamageSetting, setting, 10)
  missionCommands.addCommand("+100", menu, updateSplashDamageSetting, setting, 100)

  missionCommands.addCommand("-0.1", menu, updateSplashDamageSetting, setting, -0.1)
  missionCommands.addCommand("-1", menu, updateSplashDamageSetting, setting, -1)
  missionCommands.addCommand("-10", menu, updateSplashDamageSetting, setting, -10)
  missionCommands.addCommand("-100", menu, updateSplashDamageSetting, setting, -100)
end

function exitSplashDamageMenu()
  if splash_damage_menu then
      missionCommands.removeItem(splash_damage_menu)
      splash_damage_menu = nil
  end
end

function addSplashDamageMenu()
  if not splash_damage_options.enable_radio_menu then return end

  if splash_damage_menu then
      missionCommands.removeItem(splash_damage_menu)
  end

  splash_damage_menu = missionCommands.addSubMenu("Splash Damage Settings")

  --Page 1: Debug & General Settings
  local debugGeneralMenu = missionCommands.addSubMenu("Debug & General Settings", splash_damage_menu)
  missionCommands.addCommand("Toggle Game Messages", debugGeneralMenu, toggleSplashDamageSetting, "game_messages")
  missionCommands.addCommand("Toggle Debug Messages", debugGeneralMenu, toggleSplashDamageSetting, "debug")
  missionCommands.addCommand("Toggle Weapon Missing Messages", debugGeneralMenu, toggleSplashDamageSetting, "weapon_missing_message")
  missionCommands.addCommand("Toggle Pre-Explosion Debug", debugGeneralMenu, toggleSplashDamageSetting, "track_pre_explosion_debug")
  missionCommands.addCommand("Toggle Damage Model", debugGeneralMenu, toggleSplashDamageSetting, "damage_model")
  missionCommands.addCommand("Toggle Blast Stun", debugGeneralMenu, toggleSplashDamageSetting, "blast_stun")
  local unitDisabledMenu = missionCommands.addSubMenu("Unit Disabled Health", debugGeneralMenu)
  addValueAdjustmentCommands(unitDisabledMenu, "unit_disabled_health")
  local unitCantFireMenu = missionCommands.addSubMenu("Unit Cant Fire Health", debugGeneralMenu)
  addValueAdjustmentCommands(unitCantFireMenu, "unit_cant_fire_health")
  local infantryCantFireMenu = missionCommands.addSubMenu("Infantry Cant Fire Health", debugGeneralMenu)
  addValueAdjustmentCommands(infantryCantFireMenu, "infantry_cant_fire_health")
  local rocketMultiplierMenu = missionCommands.addSubMenu("Rocket Multiplier", debugGeneralMenu)
  addValueAdjustmentCommands(rocketMultiplierMenu, "rocket_multiplier")
  --Page 2/3: Explosions
  local explosionCargoMenu = missionCommands.addSubMenu("Explosion Settings", splash_damage_menu)
  local staticDamageMenu = missionCommands.addSubMenu("Static Damage Boost", explosionCargoMenu)
  addValueAdjustmentCommands(staticDamageMenu, "static_damage_boost")
  missionCommands.addCommand("Toggle Wave Explosions", explosionCargoMenu, toggleSplashDamageSetting, "wave_explosions")
  missionCommands.addCommand("Toggle Larger Explosions", explosionCargoMenu, toggleSplashDamageSetting, "larger_explosions")
  local blastRadiusMenu = missionCommands.addSubMenu("Blast Search Radius", explosionCargoMenu)
  addValueAdjustmentCommands(blastRadiusMenu, "blast_search_radius")

  local overallScalingMenu = missionCommands.addSubMenu("Overall Scaling", explosionCargoMenu)
  addValueAdjustmentCommands(overallScalingMenu, "overall_scaling")
  missionCommands.addCommand("Toggle Shaped Charge Effects", explosionCargoMenu, toggleSplashDamageSetting, "apply_shaped_charge_effects")
  local shapedChargeMenu = missionCommands.addSubMenu("Shaped Charge Multiplier", explosionCargoMenu)
  addValueAdjustmentCommands(shapedChargeMenu, "shaped_charge_multiplier")
  missionCommands.addCommand("Toggle Dynamic Blast Radius", explosionCargoMenu, toggleSplashDamageSetting, "use_dynamic_blast_radius")
  local dynamicBlastMenu = missionCommands.addSubMenu("Dynamic Blast Radius Modifier", explosionCargoMenu)
  addValueAdjustmentCommands(dynamicBlastMenu, "dynamic_blast_radius_modifier")

  local explosionCargoMenu = missionCommands.addSubMenu("Cascade Settings", splash_damage_menu)
  local cascadeScalingMenu = missionCommands.addSubMenu("Cascade Scaling", explosionCargoMenu)
  addValueAdjustmentCommands(cascadeScalingMenu, "cascade_scaling")
  local cascadeExplodeThresholdMenu = missionCommands.addSubMenu("Cascade Explode Threshold", explosionCargoMenu)
  addValueAdjustmentCommands(cascadeExplodeThresholdMenu, "cascade_explode_threshold")
  local cascadeThresholdMenu = missionCommands.addSubMenu("Cascade Damage Threshold", explosionCargoMenu)
  addValueAdjustmentCommands(cascadeThresholdMenu, "cascade_damage_threshold")

  --Page 4: Cargo and Ordnance Protection 
  local explosionCargoMenu = missionCommands.addSubMenu("Cargo and Ordnance", splash_damage_menu)
  missionCommands.addCommand("Toggle Always Cascade Explode", explosionCargoMenu, toggleSplashDamageSetting, "always_cascade_explode")
  missionCommands.addCommand("Toggle Tracking & Cargo Effects", explosionCargoMenu, toggleSplashDamageSetting, "track_pre_explosion")
  local cargoThresholdMenu = missionCommands.addSubMenu("Cargo Damage Threshold", explosionCargoMenu)
  addValueAdjustmentCommands(cargoThresholdMenu, "cargo_damage_threshold")
  missionCommands.addCommand("Toggle Ordnance Protection", explosionCargoMenu, toggleSplashDamageSetting, "ordnance_protection")
  local ordnanceRadiusMenu = missionCommands.addSubMenu("Ordnance Protection Radius", explosionCargoMenu)
  addValueAdjustmentCommands(ordnanceRadiusMenu, "ordnance_protection_radius")
  missionCommands.addCommand("Toggle Snap To Ground If Destroyed By LE", explosionCargoMenu, toggleSplashDamageSetting, "snap_to_ground_if_destroyed_by_large_explosion")
  local ordnanceRadiusMenu = missionCommands.addSubMenu("Ordnance Protection Radius", explosionCargoMenu)
local cargoThresholdMenu = missionCommands.addSubMenu("Max Snap Height", explosionCargoMenu)
  addValueAdjustmentCommands(cargoThresholdMenu, "max_snapped_height")
  missionCommands.addCommand("Toggle Recent Expl Track Snap", explosionCargoMenu, toggleSplashDamageSetting, "recent_large_explosion_snap")	

 
--Page 5: Debris Settings 
  local debrisMenu = missionCommands.addSubMenu("Debris Settings", splash_damage_menu)
  missionCommands.addCommand("Toggle Debris Effects", debrisMenu, toggleSplashDamageSetting, "debris_effects")
  local debrisCountMinMenu = missionCommands.addSubMenu("Min Debris Count", debrisMenu)
  addValueAdjustmentCommands(debrisCountMinMenu, "debris_count_min")
  local debrisCountMaxMenu = missionCommands.addSubMenu("Max Debris Count", debrisMenu)
  addValueAdjustmentCommands(debrisCountMaxMenu, "debris_count_max")
  local debrisDistanceMenu = missionCommands.addSubMenu("Max Debris Distance", debrisMenu)
  addValueAdjustmentCommands(debrisDistanceMenu, "debris_max_distance")
  local debrisPowerMenu = missionCommands.addSubMenu("Debris Power", debrisMenu)
  addValueAdjustmentCommands(debrisPowerMenu, "debris_power")

  --Page 6: Cluster Settings
  local clusterMenu = missionCommands.addSubMenu("Cluster Settings", splash_damage_menu)
  missionCommands.addCommand("Toggle Cluster Enabled", clusterMenu, toggleSplashDamageSetting, "cluster_enabled")
  local clusterBaseLengthMenu = missionCommands.addSubMenu("Cluster Base Length", clusterMenu)
  addValueAdjustmentCommands(clusterBaseLengthMenu, "cluster_base_length")
  local clusterBaseWidthMenu = missionCommands.addSubMenu("Cluster Base Width", clusterMenu)
  addValueAdjustmentCommands(clusterBaseWidthMenu, "cluster_base_width")
  local clusterMaxLengthMenu = missionCommands.addSubMenu("Cluster Max Length", clusterMenu)
  addValueAdjustmentCommands(clusterMaxLengthMenu, "cluster_max_length")
  local clusterMaxWidthMenu = missionCommands.addSubMenu("Cluster Max Width", clusterMenu)
  addValueAdjustmentCommands(clusterMaxWidthMenu, "cluster_max_width")
  local clusterMinLengthMenu = missionCommands.addSubMenu("Cluster Min Length", clusterMenu)
  addValueAdjustmentCommands(clusterMinLengthMenu, "cluster_min_length")
  local clusterMinWidthMenu = missionCommands.addSubMenu("Cluster Min Width", clusterMenu)
  addValueAdjustmentCommands(clusterMinWidthMenu, "cluster_min_width")
  missionCommands.addCommand("Toggle Bomblet Reduction Modifier", clusterMenu, toggleSplashDamageSetting, "cluster_bomblet_reductionmodifier")
  local clusterBombletDamageMenu = missionCommands.addSubMenu("Bomblet Damage Modifier", clusterMenu)
  addValueAdjustmentCommands(clusterBombletDamageMenu, "cluster_bomblet_damage_modifier")

--Page 7: Giant Explosion Settings
  local giantExplosionMenu = missionCommands.addSubMenu("Giant Explosion Settings", splash_damage_menu)
  missionCommands.addCommand("Toggle Giant Explosion", giantExplosionMenu, toggleSplashDamageSetting, "giant_explosion_enabled")
  missionCommands.addCommand("Toggle Static Target", giantExplosionMenu, toggleSplashDamageSetting, "giant_explosion_target_static")
  for name, target in pairs(giantExplosionTargets) do
      local displayName = name:gsub("GiantExplosionTarget", "GiantExplosionTarget")
      missionCommands.addCommand("Detonate " .. displayName, giantExplosionMenu, function()
          trigger.action.setUserFlag(displayName, 1)
      end)
          end
  missionCommands.addCommand("Detonate All Giant Targets", giantExplosionMenu, function()
      for name, target in pairs(giantExplosionTargets) do
          local flagName = name:gsub("GiantExplosionTarget", "GiantExplosionTarget")
          trigger.action.setUserFlag(flagName, 1)
      end
  end)
  local powerMenu = missionCommands.addSubMenu("Explosion Power", giantExplosionMenu)
  addValueAdjustmentCommands(powerMenu, "giant_explosion_power")
  local scaleMenu = missionCommands.addSubMenu("Size Scale", giantExplosionMenu)
  missionCommands.addCommand("+0.1", scaleMenu, updateSplashDamageSetting, "giant_explosion_scale", 0.1)
  missionCommands.addCommand("+0.5", scaleMenu, updateSplashDamageSetting, "giant_explosion_scale", 0.5)
  missionCommands.addCommand("-0.1", scaleMenu, updateSplashDamageSetting, "giant_explosion_scale", -0.1)
  missionCommands.addCommand("-0.5", scaleMenu, updateSplashDamageSetting, "giant_explosion_scale", -0.5)
  local durationMenu = missionCommands.addSubMenu("Duration", giantExplosionMenu)
  missionCommands.addCommand("+0.25s", durationMenu, updateSplashDamageSetting, "giant_explosion_duration", 0.25)
  missionCommands.addCommand("-0.25s", durationMenu, updateSplashDamageSetting, "giant_explosion_duration", -0.25)
  local countMenu = missionCommands.addSubMenu("Explosion Count", giantExplosionMenu)
  addValueAdjustmentCommands(countMenu, "giant_explosion_count")
  
end

if (script_enable == 1) then
  gameMsg("SPLASH DAMAGE 3.1 SCRIPT RUNNING")
  env.info("SPLASH DAMAGE 3.1 SCRIPT RUNNING")

  timer.scheduleFunction(function()
      protectedCall(track_wpns)
      return timer.getTime() + refreshRate
  end, {}, timer.getTime() + refreshRate)

  if splash_damage_options.giant_explosion_enabled then
      giantExplosionTargets = {} -- Ensure it’s fresh
      local targetCount = 0
      for coa = 0, 2 do
          local groups = coalition.getGroups(coa)
          if groups then
              for _, group in pairs(groups) do
                  local units = group:getUnits()
                  if units then
                      for _, unit in pairs(units) do
                          local name = unit:getName()
                          if name:find("GiantExplosionTarget") then
                              local pos = unit:getPosition().p
                              giantExplosionTargets[name] = {obj = unit, pos = pos}
                              if splash_damage_options.giant_explosion_target_static then
                                  giantExplosionTargets[name].pos = pos
                          end
                              debugMsg("Found GiantExplosionTarget: " .. name .. " at X:" .. pos.x .. " Y:" .. pos.y .. " Z:" .. pos.z)
                              targetCount = targetCount + 1
              end
          end
      end
              end
          end
      end
      debugMsg("Total GiantExplosionTargets found: " .. targetCount)
      timer.scheduleFunction(checkGiantExplosionFlag, {}, timer.getTime() + splash_damage_options.giant_explosion_poll_rate)
      if not splash_damage_options.giant_explosion_target_static then
          timer.scheduleFunction(updateTargetPosition, {}, timer.getTime() + 1.0)
      end
  end

  world.addEventHandler(WpnHandler)
  addSplashDamageMenu()
end