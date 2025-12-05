[$INDEX$] = 
{
	["rules"] = 
	{
		[1] = 
		{
			["predicate"] = "c_unit_in_zone",
			["unit"] = $CARGOUNITID$,
			["zone"] = $ZONEID$,
		}, 
	}, 
	["comment"] = "BR -  Check for Troops $CARGOUNITNAME$ in zone $ZONEID$",
	["eventlist"] = "",
	["actions"] = 
	{
		[1] = 
				{
					["text"] = "briefingRoom.mission.objectivesTriggersCommon.fireTroopsNearTrigger($OBJECTIVEINDEX$, \"$CARGOUNITNAME$\")",
					["predicate"] = "a_do_script",
				}, 
	},
	["predicate"] = "triggerOnce",
},