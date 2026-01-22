using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Communication
{
    public class TimelineEvent
    {
        public DateTime Time { get; set; }
        public string Label { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Type { get; set; } = null!;
        public List<string> Callsigns { get; set; } = new();
        public string Color { get; set; } = "#2196F3";
        public double Duration { get; set; } // Длительность события в минутах
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
