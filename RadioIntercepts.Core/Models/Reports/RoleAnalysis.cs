using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Reports
{
    public class RoleAnalysis
    {
        public string PrimaryRole { get; set; } = null!;
        public double RoleConfidence { get; set; }
        public Dictionary<string, double> RoleProbabilities { get; set; } = new();
        public List<string> RoleIndicators { get; set; } = new();
    }
}
