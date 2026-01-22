using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Analysis.Services.TemporalPatterns
{
    public class TimeSlotAnalysis
    {
        public List<int> PeakHours { get; set; }
        public List<int> QuietHours { get; set; }
        public int[] HourlyActivity { get; set; }
    }
}
