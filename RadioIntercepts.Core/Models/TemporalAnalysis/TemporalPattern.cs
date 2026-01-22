using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.TemporalAnalysis
{
    public class TemporalPattern
    {
        public string PatternType { get; set; } = null!; // "Утренний пик", "Ночной спад" и т.д.
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public double Confidence { get; set; }
        public List<string> TypicalCallsigns { get; set; } = new();
        public List<string> TypicalAreas { get; set; } = new();
    }
}
