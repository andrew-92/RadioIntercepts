using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Reports
{
    public class AreaActivity
    {
        public string Area { get; set; } = null!;
        public int MessageCount { get; set; }
        public int ActiveCallsigns { get; set; }
        public DateTime PeakActivityTime { get; set; }
        public double ActivityLevel { get; set; }
    }
}
