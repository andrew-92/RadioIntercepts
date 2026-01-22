using RadioIntercepts.Analysis.Services.PeriodComparison;
using RadioIntercepts.Core.Models.PeriodComparison;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Analysis.Interfaces.Services
{
    public interface IPeriodComparisonService
    {
        Task<PeriodComparisonResult> ComparePeriodsAsync(PeriodComparisonRequest request);
        Task<List<PeriodComparisonResult>> CompareMultiplePeriodsAsync(List<PeriodComparisonRequest> requests);
        Task<Dictionary<DateTime, PeriodStats>> GetPeriodTimeSeriesAsync(DateTime startDate, DateTime endDate, string interval = "day");
        Task<List<CallsignActivityShift>> GetCallsignActivityShiftsAsync(DateTime startDate1, DateTime endDate1, DateTime startDate2, DateTime endDate2);
    }
}
