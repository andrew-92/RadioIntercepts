using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Reports
{
    public class PatternParticipation
    {
        public string PatternType { get; set; } = null!;
        public int ParticipationCount { get; set; }
        public string RoleInPattern { get; set; } = null!;
        public double Frequency { get; set; }
    }
}
