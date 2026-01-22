using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Reports
{
    public class KeyInteraction
    {
        public string WithCallsign { get; set; } = null!;
        public int InteractionCount { get; set; }
        public DateTime FirstInteraction { get; set; }
        public DateTime LastInteraction { get; set; }
        public string Pattern { get; set; } = null!; // "frequent", "recent", "intense"
        public double Strength { get; set; }
    }
}
