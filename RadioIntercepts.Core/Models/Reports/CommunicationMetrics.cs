using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Reports
{
    public class CommunicationMetrics
    {
        public double NetworkDensity { get; set; }
        public double ResponseRate { get; set; }
        public TimeSpan AverageReactionTime { get; set; }
        public double FlowEfficiency { get; set; }
        public double Centralization { get; set; }
        public Dictionary<string, double> AdditionalMetrics { get; set; } = new();
    }
}
