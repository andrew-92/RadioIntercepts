using RadioIntercepts.Core.Models.DialogPatterns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.PeriodComparison
{
    public class MessageTypeComparison
    {
        public MessageType MessageType { get; set; }
        public int CountPeriod1 { get; set; }
        public int CountPeriod2 { get; set; }
        public double ChangePercent { get; set; }
        public double ContributionPeriod1 { get; set; }
        public double ContributionPeriod2 { get; set; }
    }
}
