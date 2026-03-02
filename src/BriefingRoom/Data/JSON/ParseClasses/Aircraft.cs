using System.Collections.Generic;
using System.Linq;
using BriefingRoom4DCS.Template;


namespace BriefingRoom4DCS.Data.JSON
{
    public class ExtraProp
    {
        public object defValue { get; set; }
        public string id { get; set; }
    }

    public class PanelRadio
    {
        public object range { get; set; }
        public string name { get; set; }
        public List<Channel> channels { get; set; }
        public string displayUnits { get; set; }
    }

    public class Channel
    {
        public double @default { get; set; }
        public string modulation { get; set; }
        public string name { get; set; }
        public bool connect { get; set; }
    }

    public class Radio
    {
        public double frequency { get; set; }
        public int modulation { get; set; }
    }

    public class Aircraft : Unit
    {
        public Dictionary<string, List<List<string>>> paintSchemes { get; set; }
        public List<Payload> payloadPresets { get; set; }
        public List<Task> tasks { get; set; }
        public double fuel { get; set; }
        public int flares { get; set; }
        public int chaff { get; set; }
        public Radio radio { get; set; }
        public double maxAlt { get; set; }
        public double cruiseSpeed { get; set; }
        public List<PanelRadio> panelRadio { get; set; }
        public List<ExtraProp> extraProps { get; set; }
        public bool? EPLRS { get; set; }
        public int? ammoType { get; set; }
        public double height { get; set; }
        public double width { get; set; }
        public double length { get; set; }
        public Dictionary<string, List<CallSign>> callsigns { get; set; }
    }

    public class AircraftLegacy : Aircraft
    {
        new public Dictionary<string, List<string>> paintSchemes { get; set; }

        public Aircraft getAircraft()
        {
            return new Aircraft
            {
                type = this.type,
                displayName = this.displayName,
                module = this.module,
                shape = this.shape,
                maxAlt = this.maxAlt,
                cruiseSpeed = this.cruiseSpeed,
                fuel = this.fuel,
                flares = this.flares,
                chaff = this.chaff,
                radio = this.radio,
                tasks = this.tasks,
                panelRadio = this.panelRadio,
                extraProps = this.extraProps,
                EPLRS = this.EPLRS,
                ammoType = this.ammoType,
                height = this.height,
                width = this.width,
                length = this.length,
                callsigns = this.callsigns,
                paintSchemes = this.paintSchemes.ToDictionary(pair => pair.Key, pair => pair.Value.Select(x => new List<string> { x, x }).ToList()),
                payloadPresets = this.payloadPresets,
                Operators = this.Operators
            };
        }
    }

    public class Task
    {
        public string OldID { get; set; }
        public string Name { get; set; }
        public int WorldID { get; set; }
    }

    public class Payload
    {
        public List<Pylon> pylons { get; set; }
        public List<int?> tasks { get; set; }
        public string name { get; set; }
        public string displayName { get; set; }
        public Decade decade { get; set; }
    }

    public class CallSign
    {
        public string Name { get; set; }
        public int WorldID { get; set; }
    }

    public class Pylon
    {
        public string CLSID { get; set; }
        public int num { get; set; }

#nullable enable
        public Dictionary<string, object>? settings { get; set; }
    }
}
