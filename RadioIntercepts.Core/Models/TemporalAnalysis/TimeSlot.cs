using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.TemporalAnalysis
{
    public class TimeSlot
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int MessageCount { get; set; }
        public int ActiveCallsigns { get; set; }
        public double ActivityLevel => MessageCount / (EndTime - StartTime).TotalHours;
        public string Description => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
    }
}
