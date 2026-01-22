using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Analysis.Services.Graphs
{
    public class GraphEdge
    {
        public string SourceCallsign { get; set; } = null!;
        public string TargetCallsign { get; set; } = null!;
        public int Weight { get; set; }
        public DateTime FirstInteraction { get; set; }
        public DateTime LastInteraction { get; set; }
        public double Strength => Weight / (1 + (LastInteraction - FirstInteraction).TotalDays);
    }
}
