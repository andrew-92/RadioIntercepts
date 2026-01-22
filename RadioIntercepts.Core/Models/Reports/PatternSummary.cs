using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Reports
{
    public class PatternSummary
    {
        public string PatternType { get; set; } = null!;
        public int Occurrences { get; set; }
        public double Confidence { get; set; }
        public List<string> ExampleCallsigns { get; set; } = new();
        public string Description { get; set; } = null!;
    }
}
