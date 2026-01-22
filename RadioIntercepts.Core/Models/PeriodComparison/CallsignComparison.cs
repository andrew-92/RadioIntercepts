using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.PeriodComparison
{
    public class CallsignComparison
    {
        public string Callsign { get; set; } = null!;
        public int CountPeriod1 { get; set; }
        public int CountPeriod2 { get; set; }
        public double ChangePercent { get; set; }
        public double ContributionPeriod1 { get; set; } // вклад в общее количество сообщений в периоде 1
        public double ContributionPeriod2 { get; set; }
    }
}
