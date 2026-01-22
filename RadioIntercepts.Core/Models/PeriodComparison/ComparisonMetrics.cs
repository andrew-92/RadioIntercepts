using RadioIntercepts.Core.Models.DialogPatterns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.PeriodComparison
{
    public class ComparisonMetrics
    {
        public double TotalMessagesChange { get; set; } // в процентах
        public double UniqueCallsignsChange { get; set; }
        public double MessagesPerDayChange { get; set; }
        public Dictionary<MessageType, double> MessageTypeChange { get; set; } = new Dictionary<MessageType, double>();
        public List<string> NewCallsigns { get; set; } = new List<string>();
        public List<string> DisappearedCallsigns { get; set; } = new List<string>();
        public List<string> NewAreas { get; set; } = new List<string>();
        public List<string> DisappearedAreas { get; set; } = new List<string>();
        public double ActivityIntensityChange { get; set; } // изменение интенсивности (сообщений в час)
    }
}
