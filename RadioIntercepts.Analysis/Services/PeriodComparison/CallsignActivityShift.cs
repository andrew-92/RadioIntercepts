using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Analysis.Services.PeriodComparison
{
    public class CallsignActivityShift
    {
        public string Callsign { get; set; } = null!;
        public int ActivityPeriod1 { get; set; }
        public int ActivityPeriod2 { get; set; }
        public double ActivityChangePercent { get; set; }
        public List<double> HourlyShifts { get; set; } = new List<double>(); // разница в процентах по каждому часу
        public int PeakShiftHour { get; set; } // час с максимальным изменением
        public double PeakShiftValue { get; set; } // величина изменения в этот час
    }
}
