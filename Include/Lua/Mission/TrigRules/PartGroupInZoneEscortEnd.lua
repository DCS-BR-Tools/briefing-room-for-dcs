[$INDEX$] = 
{
	["rules"] = 
	{
		[1] = 
		{
			["group"] = $TRIGGROUP$,
			["predicate"] = "c_part_of_group_in_zone",
			["zone"] = $ZONEID$,
		},
	}, 
	["comment"] = "BR -  Check for Escorted units in zone $ZONEID$",
	["eventlist"] = "",
	["actions"] = 
	{
		[1] = 
				{
					["text"] = "briefingRoom.mission.objectivesTriggersCommon.fireEscortNearTrigger($OBJECTIVEINDEX$)",
					["predicate"] = "a_do_script",
				}, 
	},
	["predicate"] = "triggerOnce",
},