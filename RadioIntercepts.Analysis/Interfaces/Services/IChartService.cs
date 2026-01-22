using RadioIntercepts.Core.Charts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RadioIntercepts.Analysis.Interfaces.Services
{
    public interface IChartService
    {
        Task<ChartData> GetCallsignActivityByDayOfWeekAsync(string callsign);
        Task<ChartData> GetCallsignActivityByHourAsync(string callsign);
        Task<List<ChartPoint>> GetTopFrequenciesForCallsignAsync(string callsign, int topCount = 10);

        Task<ChartData> GetActivityByDayOfWeekAsync(
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string area = null,
            string frequency = null);

        Task<ChartData> GetActivityByHourAsync(
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string area = null,
            string frequency = null);

        Task<int> GetTotalMessagesCountAsync(
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string area = null,
            string frequency = null);
    }
}