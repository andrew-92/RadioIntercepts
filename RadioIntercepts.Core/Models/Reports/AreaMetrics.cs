using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Reports
{
    public class AreaMetrics
    {
        public double ActivityDensity { get; set; }
        public double CallsignTurnover { get; set; }
        public double PatternRichness { get; set; }
        public double AlertFrequency { get; set; }
        public Dictionary<string, double> AdditionalMetrics { get; set; } = new();
    }
}
