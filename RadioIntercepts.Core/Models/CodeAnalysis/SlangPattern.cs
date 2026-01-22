using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.CodeAnalysis
{
    public class SlangPattern
    {
        public string Pattern { get; set; } = null!;
        public string Meaning { get; set; } = null!;
        public List<string> Examples { get; set; } = new();
        public int ExampleCount { get; set; }
        public double Confidence { get; set; }
        public List<string> AssociatedCallsigns { get; set; } = new();
        public DateTime FirstObserved { get; set; }
        public DateTime LastObserved { get; set; }
    }
}
