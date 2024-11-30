using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LuaTableSerializer;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace BriefingRoom4DCS.Mission.DCSLuaObjects
{
    public partial class DCSGroup
    {
        public bool Static { get; set; }
        public bool LateActivation { get; set; }
        public float Modulation { get; set; }
        public bool Uncontrolled { get; set; }
        public bool TaskSelected { get; set; }
        public string Task { get; set; }
        public List<DCSWaypoint> Waypoints { get; set; }
        public int GroupId { get; set; }
        public bool Hidden { get; set; }
        public List<DCSUnit> Units { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public string Name { get; set; }
        public float Frequency { get; set; }
        public bool RouteRelativeTOT { get; set; } = false;
        public bool? Visible { get; set; }

        public bool? HiddenOnMFD { get; set; }

        public bool Dead { get; set; }

        public bool RadioSet { get; set; } = false;

        public string ToLuaString()
        {
            if (Static) return ToLuaStringStatic();
            var obj = new Dictionary<string, object> {
                {"lateActivation", LateActivation},
                {"modulation", Modulation},
                {"tasks", System.Array.Empty<string>()},
                {"uncontrolled", Uncontrolled},
                {"taskSelected", TaskSelected},
                {"task", Task},
                {"route", new Dictionary<string, object> {
                    {"routeRelativeTOT", RouteRelativeTOT},
                    {"points", Waypoints}
                }},
                {"groupId", GroupId},
                {"hidden", Hidden},
                {"units", Units},
                {"x", X},
                {"y", Y},
                {"name", Name},
                {"communication", true},
                {"start_time", 0},
                {"frequency", Frequency},
                {"visible", Visible},
                {"hiddenOnMFD", HiddenOnMFD ?? Visible},
            };
            if (RadioSet)
                obj.Add("radioSet", true);
            return LuaSerializer.Serialize(obj.Where(x => x.Value != null).ToDictionary(x => x.Key, x => x.Value));
        }

        private string ToLuaStringStatic()
        {
            var obj = new Dictionary<string, object> {
                {"heading", 0},
                {"route", new Dictionary<string, object> {
                    {"points", Waypoints}
                }},
                {"groupId", GroupId},
                {"units", Units},
                {"x", X},
                {"y", Y},
                {"name", Name},
                {"dead", Dead},
            };
            return LuaSerializer.Serialize(obj);
        }

        public static DCSGroup YamlToGroup(string yaml)
        {
            yaml = YamlRegex().Replace(yaml, "0");
            var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
            try
            {
                return deserializer.Deserialize<DCSGroup>(yaml);
            }
            catch (System.Exception e)
            {
                throw new BriefingRoomException("en", "FailedYAMLDeserializing", e.InnerException.Message, yaml, e);
            }
        }

        [GeneratedRegex("\\$.*?\\$")]
        private static partial Regex YamlRegex();

    }
}