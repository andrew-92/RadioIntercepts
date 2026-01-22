using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Reports
{
    public class HourlyActivity
    {
        public int Hour { get; set; }
        public int MessageCount { get; set; }
        public int CallsignCount { get; set; }
        public double ActivityLevel { get; set; }
    }
}
