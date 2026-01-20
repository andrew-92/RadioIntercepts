// Application/Services/PeriodComparisonService.cs
using Microsoft.EntityFrameworkCore;
using RadioIntercepts.Core.Models;
using RadioIntercepts.Infrastructure.Data;
using RadioIntercepts.WpfApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadioIntercepts.WpfApp.Services
{
    public interface IPeriodComparisonService
    {
        Task<PeriodComparisonResult> ComparePeriodsAsync(PeriodComparisonRequest request);
        Task<List<PeriodComparisonResult>> CompareMultiplePeriodsAsync(List<PeriodComparisonRequest> requests);
        Task<Dictionary<DateTime, PeriodStats>> GetPeriodTimeSeriesAsync(DateTime startDate, DateTime endDate, string interval = "day");
        Task<List<CallsignActivityShift>> GetCallsignActivityShiftsAsync(DateTime startDate1, DateTime endDate1, DateTime startDate2, DateTime endDate2);
    }

    public class PeriodComparisonService : IPeriodComparisonService
    {
        private readonly AppDbContext _context;
        private readonly IDialogPatternAnalyzer _dialogAnalyzer;

        public PeriodComparisonService(AppDbContext context, IDialogPatternAnalyzer dialogAnalyzer)
        {
            _context = context;
            _dialogAnalyzer = dialogAnalyzer;
        }

        public async Task<PeriodComparisonResult> ComparePeriodsAsync(PeriodComparisonRequest request)
        {
            var result = new PeriodComparisonResult();

            // Получаем статистику для каждого периода
            var period1Task = GetPeriodStatsAsync(request.StartDate1, request.EndDate1, request.Area, request.Frequency, request.Callsigns);
            var period2Task = GetPeriodStatsAsync(request.StartDate2, request.EndDate2, request.Area, request.Frequency, request.Callsigns);

            await Task.WhenAll(period1Task, period2Task);

            result.Period1 = await period1Task;
            result.Period2 = await period2Task;

            // Рассчитываем метрики сравнения
            result.Metrics = CalculateComparisonMetrics(result.Period1, result.Period2);

            // Сравнение по позывным
            result.CallsignComparisons = await CompareCallsignsAsync(request);

            // Сравнение по зонам
            result.AreaComparisons = await CompareAreasAsync(request);

            // Сравнение по частотам
            result.FrequencyComparisons = await CompareFrequenciesAsync(request);

            // Сравнение по типам сообщений
            result.MessageTypeComparisons = await CompareMessageTypesAsync(request);

            return result;
        }

        public async Task<List<PeriodComparisonResult>> CompareMultiplePeriodsAsync(List<PeriodComparisonRequest> requests)
        {
            var results = new List<PeriodComparisonResult>();

            foreach (var request in requests)
            {
                var result = await ComparePeriodsAsync(request);
                results.Add(result);
            }

            return results;
        }

        public async Task<Dictionary<DateTime, PeriodStats>> GetPeriodTimeSeriesAsync(DateTime startDate, DateTime endDate, string interval = "day")
        {
            var timeSeries = new Dictionary<DateTime, PeriodStats>();

            // Определяем шаг интервала
            TimeSpan step = interval switch
            {
                "hour" => TimeSpan.FromHours(1),
                "day" => TimeSpan.FromDays(1),
                "week" => TimeSpan.FromDays(7),
                "month" => TimeSpan.FromDays(30),
                _ => TimeSpan.FromDays(1)
            };

            DateTime currentStart = startDate;
            while (currentStart < endDate)
            {
                DateTime currentEnd = currentStart + step;
                if (currentEnd > endDate)
                    currentEnd = endDate;

                var stats = await GetPeriodStatsAsync(currentStart, currentEnd, null, null, null);
                timeSeries[currentStart] = stats;

                currentStart = currentEnd;
            }

            return timeSeries;
        }

        public async Task<List<CallsignActivityShift>> GetCallsignActivityShiftsAsync(DateTime startDate1, DateTime endDate1, DateTime startDate2, DateTime endDate2)
        {
            var shifts = new List<CallsignActivityShift>();

            // Получаем активность позывных по периодам
            var period1Activity = await GetCallsignActivityAsync(startDate1, endDate1);
            var period2Activity = await GetCallsignActivityAsync(startDate2, endDate2);

            // Находим общие позывные
            var commonCallsigns = period1Activity.Keys.Intersect(period2Activity.Keys);

            foreach (var callsign in commonCallsigns)
            {
                var activity1 = period1Activity[callsign];
                var activity2 = period2Activity[callsign];

                // Рассчитываем сдвиг активности по часам
                var hourShifts = new List<double>();
                for (int hour = 0; hour < 24; hour++)
                {
                    double percent1 = activity1.HourlyDistribution.GetValueOrDefault(hour, 0);
                    double percent2 = activity2.HourlyDistribution.GetValueOrDefault(hour, 0);
                    hourShifts.Add(percent2 - percent1);
                }

                // Определяем основной сдвиг (час с максимальным изменением)
                int maxShiftHour = hourShifts.Select((val, idx) => new { Val = val, Idx = idx })
                    .OrderByDescending(x => Math.Abs(x.Val))
                    .First().Idx;

                shifts.Add(new CallsignActivityShift
                {
                    Callsign = callsign,
                    ActivityPeriod1 = activity1.TotalMessages,
                    ActivityPeriod2 = activity2.TotalMessages,
                    ActivityChangePercent = (activity2.TotalMessages - activity1.TotalMessages) / (double)activity1.TotalMessages * 100,
                    HourlyShifts = hourShifts,
                    PeakShiftHour = maxShiftHour,
                    PeakShiftValue = hourShifts[maxShiftHour]
                });
            }

            return shifts.OrderByDescending(s => Math.Abs(s.PeakShiftValue)).ToList();
        }

        private async Task<PeriodStats> GetPeriodStatsAsync(DateTime startDate, DateTime endDate, string? area, string? frequency, List<string>? callsigns)
        {
            var query = BuildFilteredQuery(startDate, endDate, area, frequency, callsigns);

            var messages = await query
                .Include(m => m.Area)
                .Include(m => m.Frequency)
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .ToListAsync();

            var stats = new PeriodStats
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalMessages = messages.Count
            };

            if (!messages.Any())
                return stats;

            // Уникальные сущности
            stats.UniqueCallsigns = messages
                .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                .Distinct()
                .Count();

            stats.UniqueAreas = messages
                .Select(m => m.Area.Name)
                .Distinct()
                .Count();

            stats.UniqueFrequencies = messages
                .Select(m => m.Frequency.Value)
                .Distinct()
                .Count();

            // Сообщений в день
            double days = (endDate - startDate).TotalDays;
            stats.MessagesPerDay = days > 0 ? stats.TotalMessages / days : 0;

            // Среднее время между сообщениями
            if (messages.Count > 1)
            {
                var orderedMessages = messages.OrderBy(m => m.DateTime).ToList();
                var timeSpans = new List<TimeSpan>();
                for (int i = 1; i < orderedMessages.Count; i++)
                {
                    timeSpans.Add(orderedMessages[i].DateTime - orderedMessages[i - 1].DateTime);
                }
                stats.AverageTimeBetweenMessages = TimeSpan.FromMilliseconds(timeSpans.Average(ts => ts.TotalMilliseconds));
            }

            // Распределение по типам сообщений
            foreach (var message in messages)
            {
                var type = _dialogAnalyzer.ClassifySingleMessage(message.Dialog);
                if (!stats.MessageTypeDistribution.ContainsKey(type))
                    stats.MessageTypeDistribution[type] = 0;
                stats.MessageTypeDistribution[type]++;
            }

            // Топ позывных
            stats.TopCallsigns = messages
                .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                .GroupBy(c => c)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Count());

            // Топ зон
            stats.TopAreas = messages
                .GroupBy(m => m.Area.Name)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Count());

            // Топ частот
            stats.TopFrequencies = messages
                .GroupBy(m => m.Frequency.Value)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Count());

            return stats;
        }

        private ComparisonMetrics CalculateComparisonMetrics(PeriodStats period1, PeriodStats period2)
        {
            var metrics = new ComparisonMetrics();

            // Изменения в абсолютных показателях
            metrics.TotalMessagesChange = CalculateChangePercent(period1.TotalMessages, period2.TotalMessages);
            metrics.UniqueCallsignsChange = CalculateChangePercent(period1.UniqueCallsigns, period2.UniqueCallsigns);
            metrics.MessagesPerDayChange = CalculateChangePercent(period1.MessagesPerDay, period2.MessagesPerDay);

            // Изменения в распределении по типам сообщений
            foreach (var type in Enum.GetValues(typeof(MessageType)).Cast<MessageType>())
            {
                int count1 = period1.MessageTypeDistribution.GetValueOrDefault(type, 0);
                int count2 = period2.MessageTypeDistribution.GetValueOrDefault(type, 0);
                metrics.MessageTypeChange[type] = CalculateChangePercent(count1, count2);
            }

            // Новые и исчезнувшие позывные
            var callsigns1 = period1.TopCallsigns.Keys.ToHashSet();
            var callsigns2 = period2.TopCallsigns.Keys.ToHashSet();

            metrics.NewCallsigns = callsigns2.Except(callsigns1).ToList();
            metrics.DisappearedCallsigns = callsigns1.Except(callsigns2).ToList();

            // Новые и исчезнувшие зоны
            var areas1 = period1.TopAreas.Keys.ToHashSet();
            var areas2 = period2.TopAreas.Keys.ToHashSet();

            metrics.NewAreas = areas2.Except(areas1).ToList();
            metrics.DisappearedAreas = areas1.Except(areas2).ToList();

            // Изменение интенсивности активности (сообщений в час)
            double hours1 = (period1.EndDate - period1.StartDate).TotalHours;
            double hours2 = (period2.EndDate - period2.StartDate).TotalHours;
            double intensity1 = hours1 > 0 ? period1.TotalMessages / hours1 : 0;
            double intensity2 = hours2 > 0 ? period2.TotalMessages / hours2 : 0;
            metrics.ActivityIntensityChange = CalculateChangePercent(intensity1, intensity2);

            return metrics;
        }

        private async Task<List<CallsignComparison>> CompareCallsignsAsync(PeriodComparisonRequest request)
        {
            var comparisons = new List<CallsignComparison>();

            // Получаем данные по позывным для каждого периода
            var callsignsPeriod1 = await GetCallsignStatsAsync(request.StartDate1, request.EndDate1, request.Area, request.Frequency, request.Callsigns);
            var callsignsPeriod2 = await GetCallsignStatsAsync(request.StartDate2, request.EndDate2, request.Area, request.Frequency, request.Callsigns);

            // Объединяем все позывные из обоих периодов
            var allCallsigns = callsignsPeriod1.Keys.Union(callsignsPeriod2.Keys);

            foreach (var callsign in allCallsigns)
            {
                int count1 = callsignsPeriod1.GetValueOrDefault(callsign, 0);
                int count2 = callsignsPeriod2.GetValueOrDefault(callsign, 0);

                double changePercent = CalculateChangePercent(count1, count2);

                // Вклад в общее количество сообщений в каждом периоде
                double contribution1 = await GetCallsignContributionAsync(request.StartDate1, request.EndDate1, callsign, request.Area, request.Frequency);
                double contribution2 = await GetCallsignContributionAsync(request.StartDate2, request.EndDate2, callsign, request.Area, request.Frequency);

                comparisons.Add(new CallsignComparison
                {
                    Callsign = callsign,
                    CountPeriod1 = count1,
                    CountPeriod2 = count2,
                    ChangePercent = changePercent,
                    ContributionPeriod1 = contribution1,
                    ContributionPeriod2 = contribution2
                });
            }

            return comparisons
                .OrderByDescending(c => Math.Abs(c.ChangePercent))
                .Take(50)
                .ToList();
        }

        private async Task<List<AreaComparison>> CompareAreasAsync(PeriodComparisonRequest request)
        {
            var comparisons = new List<AreaComparison>();

            var areasPeriod1 = await GetAreaStatsAsync(request.StartDate1, request.EndDate1, request.Area, request.Frequency, request.Callsigns);
            var areasPeriod2 = await GetAreaStatsAsync(request.StartDate2, request.EndDate2, request.Area, request.Frequency, request.Callsigns);

            var allAreas = areasPeriod1.Keys.Union(areasPeriod2.Keys);

            foreach (var area in allAreas)
            {
                int count1 = areasPeriod1.GetValueOrDefault(area, 0);
                int count2 = areasPeriod2.GetValueOrDefault(area, 0);

                comparisons.Add(new AreaComparison
                {
                    Area = area,
                    CountPeriod1 = count1,
                    CountPeriod2 = count2,
                    ChangePercent = CalculateChangePercent(count1, count2)
                });
            }

            return comparisons
                .OrderByDescending(a => Math.Abs(a.ChangePercent))
                .Take(20)
                .ToList();
        }

        private async Task<List<FrequencyComparison>> CompareFrequenciesAsync(PeriodComparisonRequest request)
        {
            var comparisons = new List<FrequencyComparison>();

            var frequenciesPeriod1 = await GetFrequencyStatsAsync(request.StartDate1, request.EndDate1, request.Area, request.Frequency, request.Callsigns);
            var frequenciesPeriod2 = await GetFrequencyStatsAsync(request.StartDate2, request.EndDate2, request.Area, request.Frequency, request.Callsigns);

            var allFrequencies = frequenciesPeriod1.Keys.Union(frequenciesPeriod2.Keys);

            foreach (var freq in allFrequencies)
            {
                int count1 = frequenciesPeriod1.GetValueOrDefault(freq, 0);
                int count2 = frequenciesPeriod2.GetValueOrDefault(freq, 0);

                comparisons.Add(new FrequencyComparison
                {
                    Frequency = freq,
                    CountPeriod1 = count1,
                    CountPeriod2 = count2,
                    ChangePercent = CalculateChangePercent(count1, count2)
                });
            }

            return comparisons
                .OrderByDescending(f => Math.Abs(f.ChangePercent))
                .Take(20)
                .ToList();
        }

        private async Task<List<MessageTypeComparison>> CompareMessageTypesAsync(PeriodComparisonRequest request)
        {
            var comparisons = new List<MessageTypeComparison>();

            var typesPeriod1 = await GetMessageTypeStatsAsync(request.StartDate1, request.EndDate1, request.Area, request.Frequency, request.Callsigns);
            var typesPeriod2 = await GetMessageTypeStatsAsync(request.StartDate2, request.EndDate2, request.Area, request.Frequency, request.Callsigns);

            foreach (MessageType type in Enum.GetValues(typeof(MessageType)))
            {
                int count1 = typesPeriod1.GetValueOrDefault(type, 0);
                int count2 = typesPeriod2.GetValueOrDefault(type, 0);

                double contribution1 = typesPeriod1.Any() ? (double)count1 / typesPeriod1.Values.Sum() * 100 : 0;
                double contribution2 = typesPeriod2.Any() ? (double)count2 / typesPeriod2.Values.Sum() * 100 : 0;

                comparisons.Add(new MessageTypeComparison
                {
                    MessageType = type,
                    CountPeriod1 = count1,
                    CountPeriod2 = count2,
                    ChangePercent = CalculateChangePercent(count1, count2),
                    ContributionPeriod1 = contribution1,
                    ContributionPeriod2 = contribution2
                });
            }

            return comparisons
                .OrderByDescending(t => Math.Abs(t.ChangePercent))
                .ToList();
        }

        private async Task<Dictionary<string, int>> GetCallsignStatsAsync(DateTime startDate, DateTime endDate, string? area, string? frequency, List<string>? callsigns)
        {
            var query = BuildFilteredQuery(startDate, endDate, area, frequency, callsigns);

            return await query
                .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                .GroupBy(c => c)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        private async Task<Dictionary<string, int>> GetAreaStatsAsync(DateTime startDate, DateTime endDate, string? area, string? frequency, List<string>? callsigns)
        {
            var query = BuildFilteredQuery(startDate, endDate, area, frequency, callsigns);

            return await query
                .GroupBy(m => m.Area.Name)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        private async Task<Dictionary<string, int>> GetFrequencyStatsAsync(DateTime startDate, DateTime endDate, string? area, string? frequency, List<string>? callsigns)
        {
            var query = BuildFilteredQuery(startDate, endDate, area, frequency, callsigns);

            return await query
                .GroupBy(m => m.Frequency.Value)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        private async Task<Dictionary<MessageType, int>> GetMessageTypeStatsAsync(DateTime startDate, DateTime endDate, string? area, string? frequency, List<string>? callsigns)
        {
            var query = BuildFilteredQuery(startDate, endDate, area, frequency, callsigns);

            var messages = await query
                .Select(m => m.Dialog)
                .ToListAsync();

            var stats = new Dictionary<MessageType, int>();
            foreach (var message in messages)
            {
                var type = _dialogAnalyzer.ClassifySingleMessage(message);
                if (!stats.ContainsKey(type))
                    stats[type] = 0;
                stats[type]++;
            }

            return stats;
        }

        private async Task<double> GetCallsignContributionAsync(DateTime startDate, DateTime endDate, string callsign, string? area, string? frequency)
        {
            var query = BuildFilteredQuery(startDate, endDate, area, frequency, null);

            int totalMessages = await query.CountAsync();
            if (totalMessages == 0)
                return 0;

            int callsignMessages = await query
                .Where(m => m.MessageCallsigns.Any(mc => mc.Callsign.Name == callsign))
                .CountAsync();

            return (double)callsignMessages / totalMessages * 100;
        }

        private async Task<Dictionary<string, CallsignActivity>> GetCallsignActivityAsync(DateTime startDate, DateTime endDate)
        {
            var query = _context.Messages
                .Where(m => m.DateTime >= startDate && m.DateTime <= endDate)
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign);

            var messages = await query.ToListAsync();

            var activity = new Dictionary<string, CallsignActivity>();

            foreach (var message in messages)
            {
                var hour = message.DateTime.Hour;

                foreach (var callsign in message.MessageCallsigns.Select(mc => mc.Callsign.Name))
                {
                    if (!activity.ContainsKey(callsign))
                    {
                        activity[callsign] = new CallsignActivity
                        {
                            Callsign = callsign,
                            TotalMessages = 0,
                            HourlyDistribution = new Dictionary<int, double>()
                        };
                    }

                    activity[callsign].TotalMessages++;

                    if (!activity[callsign].HourlyDistribution.ContainsKey(hour))
                        activity[callsign].HourlyDistribution[hour] = 0;
                    activity[callsign].HourlyDistribution[hour]++;
                }
            }

            // Нормализуем распределение по часам (в проценты)
            foreach (var callsign in activity.Keys)
            {
                var hourly = activity[callsign].HourlyDistribution;
                double total = hourly.Values.Sum();
                if (total > 0)
                {
                    foreach (var hour in hourly.Keys.ToList())
                    {
                        hourly[hour] = (hourly[hour] / total) * 100;
                    }
                }
            }

            return activity;
        }

        private IQueryable<Message> BuildFilteredQuery(DateTime startDate, DateTime endDate, string? area, string? frequency, List<string>? callsigns)
        {
            IQueryable<Message> query = _context.Messages
                .Where(m => m.DateTime >= startDate && m.DateTime <= endDate);

            if (!string.IsNullOrEmpty(area))
                query = query.Where(m => m.Area.Name == area);

            if (!string.IsNullOrEmpty(frequency))
                query = query.Where(m => m.Frequency.Value == frequency);

            if (callsigns != null && callsigns.Any())
                query = query.Where(m => m.MessageCallsigns.Any(mc => callsigns.Contains(mc.Callsign.Name)));

            return query;
        }

        private double CalculateChangePercent(double value1, double value2)
        {
            if (value1 == 0)
                return value2 == 0 ? 0 : 100;

            return ((value2 - value1) / Math.Abs(value1)) * 100;
        }
    }

    public class CallsignActivity
    {
        public string Callsign { get; set; } = null!;
        public int TotalMessages { get; set; }
        public Dictionary<int, double> HourlyDistribution { get; set; } = new Dictionary<int, double>(); // час -> процент сообщений
    }

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