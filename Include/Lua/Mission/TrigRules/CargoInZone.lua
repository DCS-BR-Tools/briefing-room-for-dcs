[$INDEX$] = 
{
	["rules"] = 
	{
		[1] = 
		{
			["predicate"] = "c_cargo_unhooked_in_zone",
			["cargo"] = $CARGOUNITID$,
			["zone"] = $ZONEID$,
		}, 
	}, 
	["comment"] = "BR -  Check for Cargo $CARGOUNITNAME$ in zone $ZONEID$",
	["eventlist"] = "",
	["actions"] = 
	{
		[1] = 
				{
					["text"] = "briefingRoom.mission.objectivesTriggersCommon.fireCargoNearTrigger($OBJECTIVEINDEX$, \"$CARGOUNITNAME$\")",
					["predicate"] = "a_do_script",
				}, 
	},
	["predicate"] = "triggerOnce",
},