// Core/Models/PeriodComparison.cs
using RadioIntercepts.Core.Models.DialogPatterns;
using System;
using System.Collections.Generic;

namespace RadioIntercepts.Core.Models.PeriodComparison
{
    public class PeriodComparisonRequest
    {
        public DateTime StartDate1 { get; set; }
        public DateTime EndDate1 { get; set; }
        public DateTime StartDate2 { get; set; }
        public DateTime EndDate2 { get; set; }
        public string? Area { get; set; }
        public string? Frequency { get; set; }
        public List<string>? Callsigns { get; set; }
    }
}