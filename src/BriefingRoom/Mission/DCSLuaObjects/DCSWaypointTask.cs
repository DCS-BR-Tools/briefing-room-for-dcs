using System.Collections.Generic;
using LuaTableSerializer;

namespace BriefingRoom4DCS.Mission.DCSLuaObjects
{
    public class DCSWaypointTask
    {

        public bool Enabled { get; init; } = true;
        public bool Auto { get; init; } = true;
        public string Id { get; init; }
        public string Key { get; set; }
        public int Priority { get; set; } = 9;
        private Dictionary<string, object> _parameters = new();
        public Dictionary<string, object> Parameters
        {
            get { return _parameters; }
            set { _parameters = DeterminTypes(value); }
        }

        public DCSWaypointTask() { }

        public DCSWaypointTask(string _id, Dictionary<string, object> _parameters, bool _enabled = true, bool _auto = true, int _priority = 9)
        {
            Id = _id;
            Parameters = _parameters;
            Enabled = _enabled;
            Auto = _auto;
            Priority = _priority;
        }

        public string ToLuaString(int number)
        {
            var obj = new Dictionary<string, object> {
                {"id", Id},
                {"number", number},
                {"enabled", Enabled},
                {"auto", Auto},
                {"params", Parameters},
            };
            if (!string.IsNullOrEmpty(Key))
                obj.Add("key", Key);
            return LuaSerializer.Serialize(obj);
        }

        private static Dictionary<string, object> DeterminTypes(Dictionary<string, object> values)
        {
            foreach (var param in values)
            {
                values[param.Key] = Toolbox.DeterminType(param.Value);
            }
            return values;
        }

    }
}