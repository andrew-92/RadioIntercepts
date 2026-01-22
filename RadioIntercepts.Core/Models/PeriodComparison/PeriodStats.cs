using RadioIntercepts.Core.Models.DialogPatterns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.PeriodComparison
{
    public class PeriodStats
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalMessages { get; set; }
        public int UniqueCallsigns { get; set; }
        public int UniqueAreas { get; set; }
        public int UniqueFrequencies { get; set; }
        public double MessagesPerDay { get; set; }
        public TimeSpan AverageTimeBetweenMessages { get; set; }
        public Dictionary<MessageType, int> MessageTypeDistribution { get; set; } = new Dictionary<MessageType, int>();
        public Dictionary<string, int> TopCallsigns { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> TopAreas { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> TopFrequencies { get; set; } = new Dictionary<string, int>();
    }
}
