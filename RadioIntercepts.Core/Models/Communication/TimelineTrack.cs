using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Communication
{
    public class TimelineTrack
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Type { get; set; } = null!; // Callsign, Area, Frequency, Group
        public List<TimelineEvent> Events { get; set; } = new();
        public string Color { get; set; } = "#2196F3";
        public double ActivityLevel { get; set; } // Уровень активности (0-1)
    }
}
