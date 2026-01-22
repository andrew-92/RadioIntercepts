using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.DialogPatterns
{
    public class RoleAnalysisResult
    {
        public string Callsign { get; set; } = null!;
        public ParticipantRole Role { get; set; }
        public double RoleConfidence { get; set; }
        public Dictionary<MessageType, int> MessageTypeDistribution { get; set; } = new();
        public Dictionary<string, double> RoleFeatures { get; set; } = new();
    }
}
