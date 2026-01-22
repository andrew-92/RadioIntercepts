using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Communication
{
    public class TimelineStatistics
    {
        public int EventCount { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public double Density { get; set; } // Плотность событий
        public TimeSpan AverageEventDuration { get; set; }
        public List<DateTime> PeakTimes { get; set; } = new();
    }
}
