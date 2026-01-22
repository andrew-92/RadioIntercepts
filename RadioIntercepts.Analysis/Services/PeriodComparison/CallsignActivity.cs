using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Analysis.Services.PeriodComparison
{
    public class CallsignActivity
    {
        public string Callsign { get; set; } = null!;
        public int TotalMessages { get; set; }
        public Dictionary<int, double> HourlyDistribution { get; set; } = new Dictionary<int, double>(); // час -> процент сообщений
    }
}
