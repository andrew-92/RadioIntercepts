using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Reports
{
    public class BehavioralChange
    {
        public DateTime ChangeDate { get; set; }
        public string ChangeType { get; set; } = null!; // "activity_increase", "area_change", "pattern_change"
        public string Description { get; set; } = null!;
        public double Magnitude { get; set; }
        public List<string> PossibleReasons { get; set; } = new();
    }
}
