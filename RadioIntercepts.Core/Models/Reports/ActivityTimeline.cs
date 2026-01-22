using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Reports
{
    public class ActivityTimeline
    {
        public List<HourlyActivity> HourlyData { get; set; } = new();
        public List<DailyActivity> DailyData { get; set; } = new();
        public DateTime PeakTime { get; set; }
        public double AverageActivity { get; set; }
    }
}
