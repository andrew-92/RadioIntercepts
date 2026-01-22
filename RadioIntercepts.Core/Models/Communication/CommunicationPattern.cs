using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Communication
{
    public class CommunicationPattern
    {
        public string PatternType { get; set; } = null!;
        public string Description { get; set; } = null!;
        public List<string> ExampleFlows { get; set; } = new();
        public double Frequency { get; set; }
        public double Confidence { get; set; }
        public List<string> CharacteristicCallsigns { get; set; } = new();
        public TimeSpan TypicalDuration { get; set; }
    }
}
