using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.PeriodComparison
{
    public class FrequencyComparison
    {
        public string Frequency { get; set; } = null!;
        public int CountPeriod1 { get; set; }
        public int CountPeriod2 { get; set; }
        public double ChangePercent { get; set; }
    }
}
