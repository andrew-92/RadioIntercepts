using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Reports
{
    public class Recommendation
    {
        public string Type { get; set; } = null!; // "monitoring", "investigation", "analysis"
        public string Description { get; set; } = null!;
        public string Priority { get; set; } = null!; // "low", "medium", "high"
        public List<string> Actions { get; set; } = new();
    }
}
