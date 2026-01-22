using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Reports
{
    public class CommunicationStyle
    {
        public double AverageMessageLength { get; set; }
        public double QuestionRatio { get; set; }
        public double CommandRatio { get; set; }
        public double ReportRatio { get; set; }
        public Dictionary<string, double> StyleMetrics { get; set; } = new();
        public List<string> CharacteristicPhrases { get; set; } = new();
    }
}
