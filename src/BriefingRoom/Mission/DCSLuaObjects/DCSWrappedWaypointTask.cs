using System.Collections.Generic;
using LuaTableSerializer;

namespace BriefingRoom4DCS.Mission.DCSLuaObjects
{
    internal class DCSWrappedWaypointTask : DCSWaypointTask
    {
        internal DCSWrappedWaypointTask(string _id, Dictionary<string, object> _parameters, bool _enabled = true, bool _auto = true, int _priority = 9) : base(_id, _parameters, _enabled, _auto, _priority)
        {
        }

        internal DCSWrappedWaypointTask(DCSWaypointTask task) : base(task.Id, task.Parameters, task.Enabled, task.Auto, task.Priority)
        {
            Parameters.Remove("wrapped");
        }

        public new string ToLuaString(int number)
        {
            return LuaSerializer.Serialize(new Dictionary<string, object> {
                {"id", "WrappedAction"},
                {"number", number},
                {"enabled", Enabled},
                {"auto", Enabled},
                {"params", new Dictionary<string, object>{
                    {"action", new Dictionary<string, object>{
                        {"id", Id},
                        {"params", Parameters}
                    }}
                }},
            });
        }

    }
}