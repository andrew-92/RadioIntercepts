// Application/Services/TemporalAnalysisService.cs
using Microsoft.EntityFrameworkCore;
using RadioIntercepts.Core.Models;
using RadioIntercepts.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadioIntercepts.WpfApp.Services
{
    public interface ITemporalAnalysisService
    {
        Task<TimeSlotAnalysis> AnalyzeActivitySlotsAsync(DateTime? startDate = null, DateTime? endDate = null,
            string? area = null, string? frequency = null, int slotDurationHours = 1);
        Task<List<TemporalPattern>> DetectTemporalPatternsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<AnomalyDetectionResult>> DetectAnomaliesAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<PredictionResult>> PredictActivityAsync(string callsign, int hoursAhead = 24);
        Task<Dictionary<DayOfWeek, int>> AnalyzeDayOfWeekPatternsAsync(string callsign = null);
        Task<Dictionary<int, int>> AnalyzeHourlyPatternsAsync(string callsign = null); // час -> количество сообщений
        Task<List<DateTime>> FindSilentPeriodsAsync(TimeSpan minDuration, string callsign = null);
        Task<List<DateTime>> FindPeakActivityTimesAsync(int topN = 10, string callsign = null);
    }

    public class TemporalAnalysisService : ITemporalAnalysisService
    {
        private readonly AppDbContext _context;

        public TemporalAnalysisService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TimeSlotAnalysis> AnalyzeActivitySlotsAsync(
            DateTime? startDate = null, DateTime? endDate = null,
            string? area = null, string? frequency = null, int slotDurationHours = 1)
        {
            var query = BuildFilteredQuery(startDate, endDate, area, frequency);
            var messages = await query.ToListAsync();

            if (!messages.Any())
                return new TimeSlotAnalysis();

            var periodStart = startDate ?? messages.Min(m => m.DateTime);
            var periodEnd = endDate ?? messages.Max(m => m.DateTime);
            var totalHours = (periodEnd - periodStart).TotalHours;
            var slotCount = (int)Math.Ceiling(totalHours / slotDurationHours);

            var slots = new List<TimeSlot>();
            for (int i = 0; i < slotCount; i++)
            {
                var slotStart = periodStart.AddHours(i * slotDurationHours);
                var slotEnd = slotStart.AddHours(slotDurationHours);

                var slotMessages = messages.Where(m => m.DateTime >= slotStart && m.DateTime < slotEnd).ToList();

                slots.Add(new TimeSlot
                {
                    StartTime = slotStart.TimeOfDay,
                    EndTime = slotEnd.TimeOfDay,
                    MessageCount = slotMessages.Count,
                    ActiveCallsigns = slotMessages
                        .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                        .Distinct()
                        .Count()
                });
            }

            var peakSlot = slots.OrderByDescending(s => s.MessageCount).FirstOrDefault();
            var quietSlot = slots.Where(s => s.MessageCount > 0).OrderBy(s => s.MessageCount).FirstOrDefault();

            // Рассчитываем коэффициент вариации
            var mean = slots.Average(s => s.MessageCount);
            var variance = slots.Sum(s => Math.Pow(s.MessageCount - mean, 2)) / slots.Count;
            var stdDev = Math.Sqrt(variance);
            var variation = mean > 0 ? stdDev / mean : 0;

            return new TimeSlotAnalysis
            {
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                Slots = slots,
                PeakSlot = peakSlot,
                QuietSlot = quietSlot,
                ActivityVariation = variation
            };
        }

        public async Task<List<TemporalPattern>> DetectTemporalPatternsAsync(
            DateTime? startDate = null, DateTime? endDate = null)
        {
            var patterns = new List<TemporalPattern>();

            // Анализируем почасовую активность
            var hourlyData = await AnalyzeHourlyPatternsAsync();

            // Определяем утренний пик (6:00 - 10:00)
            var morningHours = Enumerable.Range(6, 4);
            var morningActivity = morningHours.Sum(h => hourlyData.ContainsKey(h) ? hourlyData[h] : 0);
            var avgMorningActivity = morningActivity / 4.0;

            // Определяем дневную активность (10:00 - 18:00)
            var dayHours = Enumerable.Range(10, 8);
            var dayActivity = dayHours.Sum(h => hourlyData.ContainsKey(h) ? hourlyData[h] : 0);
            var avgDayActivity = dayActivity / 8.0;

            // Определяем вечернюю активность (18:00 - 22:00)
            var eveningHours = Enumerable.Range(18, 4);
            var eveningActivity = eveningHours.Sum(h => hourlyData.ContainsKey(h) ? hourlyData[h] : 0);
            var avgEveningActivity = eveningActivity / 4.0;

            // Определяем ночную активность (22:00 - 6:00)
            var nightHours = Enumerable.Range(22, 8).Select(h => h % 24);
            var nightActivity = nightHours.Sum(h => hourlyData.ContainsKey(h) ? hourlyData[h] : 0);
            var avgNightActivity = nightActivity / 8.0;

            // Создаем паттерны
            if (avgMorningActivity > avgDayActivity * 1.2)
            {
                patterns.Add(new TemporalPattern
                {
                    PatternType = "Утренний пик",
                    StartTime = TimeSpan.FromHours(6),
                    EndTime = TimeSpan.FromHours(10),
                    Confidence = CalculatePatternConfidence(avgMorningActivity, avgDayActivity),
                    TypicalCallsigns = await GetTypicalCallsignsForPeriodAsync(TimeSpan.FromHours(6), TimeSpan.FromHours(10)),
                    TypicalAreas = await GetTypicalAreasForPeriodAsync(TimeSpan.FromHours(6), TimeSpan.FromHours(10))
                });
            }

            if (avgDayActivity > avgMorningActivity * 1.2 && avgDayActivity > avgEveningActivity * 1.2)
            {
                patterns.Add(new TemporalPattern
                {
                    PatternType = "Дневная активность",
                    StartTime = TimeSpan.FromHours(10),
                    EndTime = TimeSpan.FromHours(18),
                    Confidence = CalculatePatternConfidence(avgDayActivity, (avgMorningActivity + avgEveningActivity) / 2),
                    TypicalCallsigns = await GetTypicalCallsignsForPeriodAsync(TimeSpan.FromHours(10), TimeSpan.FromHours(18)),
                    TypicalAreas = await GetTypicalAreasForPeriodAsync(TimeSpan.FromHours(10), TimeSpan.FromHours(18))
                });
            }

            if (avgNightActivity < avgDayActivity * 0.3)
            {
                patterns.Add(new TemporalPattern
                {
                    PatternType = "Ночной спад",
                    StartTime = TimeSpan.FromHours(22),
                    EndTime = TimeSpan.FromHours(6),
                    Confidence = 1.0 - (avgNightActivity / avgDayActivity),
                    TypicalCallsigns = await GetTypicalCallsignsForPeriodAsync(TimeSpan.FromHours(22), TimeSpan.FromHours(6)),
                    TypicalAreas = await GetTypicalAreasForPeriodAsync(TimeSpan.FromHours(22), TimeSpan.FromHours(6))
                });
            }

            // Анализ по дням недели
            var dayOfWeekPatterns = await AnalyzeDayOfWeekPatternsAsync();
            var maxDay = dayOfWeekPatterns.OrderByDescending(kv => kv.Value).First();
            var minDay = dayOfWeekPatterns.OrderBy(kv => kv.Value).First();

            if (maxDay.Value > minDay.Value * 1.5)
            {
                patterns.Add(new TemporalPattern
                {
                    PatternType = $"Пик в {TranslateDayOfWeek(maxDay.Key)}",
                    StartTime = TimeSpan.Zero,
                    EndTime = TimeSpan.FromHours(24),
                    Confidence = (double)(maxDay.Value - minDay.Value) / maxDay.Value,
                    TypicalCallsigns = await GetTypicalCallsignsForDayAsync(maxDay.Key),
                    TypicalAreas = await GetTypicalAreasForDayAsync(maxDay.Key)
                });
            }

            return patterns.OrderByDescending(p => p.Confidence).ToList();
        }

        public async Task<List<AnomalyDetectionResult>> DetectAnomaliesAsync(
            DateTime? startDate = null, DateTime? endDate = null)
        {
            var anomalies = new List<AnomalyDetectionResult>();
            var query = BuildFilteredQuery(startDate, endDate, null, null);
            var messages = await query
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .Include(m => m.Area)
                .OrderBy(m => m.DateTime)
                .ToListAsync();

            if (!messages.Any())
                return anomalies;

            // 1. Поиск необычно долгих периодов молчания
            var silentPeriods = await FindSilentPeriodsAsync(TimeSpan.FromHours(4));
            foreach (var silentTime in silentPeriods)
            {
                anomalies.Add(new AnomalyDetectionResult
                {
                    Timestamp = silentTime,
                    Type = "Долгое молчание",
                    Description = $"Период молчания продолжительностью более 4 часов",
                    Severity = 0.7,
                    RelatedCallsigns = new List<string>(),
                    RelatedAreas = new List<string>()
                });
            }

            // 2. Поиск всплесков активности
            var hourlyData = messages
                .GroupBy(m => new DateTime(m.DateTime.Year, m.DateTime.Month, m.DateTime.Day, m.DateTime.Hour, 0, 0))
                .Select(g => new { Hour = g.Key, Count = g.Count() })
                .OrderBy(x => x.Hour)
                .ToList();

            if (hourlyData.Count > 1)
            {
                var avg = hourlyData.Average(x => x.Count);
                var stdDev = Math.Sqrt(hourlyData.Sum(x => Math.Pow(x.Count - avg, 2)) / hourlyData.Count);

                foreach (var hour in hourlyData)
                {
                    if (hour.Count > avg + 2 * stdDev)
                    {
                        var hourMessages = messages.Where(m =>
                            m.DateTime >= hour.Hour && m.DateTime < hour.Hour.AddHours(1)).ToList();

                        anomalies.Add(new AnomalyDetectionResult
                        {
                            Timestamp = hour.Hour,
                            Type = "Всплеск активности",
                            Description = $"Необычно высокая активность: {hour.Count} сообщений в час (среднее: {avg:F1})",
                            Severity = Math.Min(1.0, (hour.Count - avg) / (avg * 3)),
                            RelatedCallsigns = hourMessages
                                .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                                .Distinct()
                                .ToList(),
                            RelatedAreas = hourMessages
                                .Select(m => m.Area.Name)
                                .Distinct()
                                .ToList()
                        });
                    }
                }
            }

            // 3. Обнаружение новых позывных
            var allCallsigns = messages
                .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                .Distinct()
                .ToList();

            var firstAppearance = new Dictionary<string, DateTime>();
            foreach (var message in messages.OrderBy(m => m.DateTime))
            {
                foreach (var cs in message.MessageCallsigns.Select(mc => mc.Callsign.Name))
                {
                    if (!firstAppearance.ContainsKey(cs))
                    {
                        firstAppearance[cs] = message.DateTime;

                        // Если это первый день в данных, считаем это новым позывным
                        if (message.DateTime.Date <= (startDate?.Date ?? messages.Min(m => m.DateTime).Date).AddDays(1))
                        {
                            anomalies.Add(new AnomalyDetectionResult
                            {
                                Timestamp = message.DateTime,
                                Type = "Новый позывной",
                                Description = $"Первое появление позывного '{cs}'",
                                Severity = 0.5,
                                RelatedCallsigns = new List<string> { cs },
                                RelatedAreas = new List<string> { message.Area.Name }
                            });
                        }
                    }
                }
            }

            // 4. Обнаружение необычных времен (активность ночью, если обычно днем)
            var typicalHours = await AnalyzeHourlyPatternsAsync();
            var typicalDayHours = typicalHours.Where(kv => kv.Key >= 6 && kv.Key <= 22).Sum(kv => kv.Value);
            var typicalNightHours = typicalHours.Where(kv => kv.Key < 6 || kv.Key > 22).Sum(kv => kv.Value);

            if (typicalDayHours > typicalNightHours * 3)
            {
                // Обычно активны днем, ищем ночную активность
                var nightMessages = messages.Where(m => m.DateTime.Hour < 6 || m.DateTime.Hour > 22);
                if (nightMessages.Any())
                {
                    anomalies.Add(new AnomalyDetectionResult
                    {
                        Timestamp = nightMessages.First().DateTime,
                        Type = "Ночная активность",
                        Description = "Активность в нехарактерное ночное время",
                        Severity = 0.6,
                        RelatedCallsigns = nightMessages
                            .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                            .Distinct()
                            .ToList(),
                        RelatedAreas = nightMessages
                            .Select(m => m.Area.Name)
                            .Distinct()
                            .ToList()
                    });
                }
            }

            return anomalies.OrderBy(a => a.Timestamp).ToList();
        }

        public async Task<List<PredictionResult>> PredictActivityAsync(string callsign, int hoursAhead = 24)
        {
            var predictions = new List<PredictionResult>();

            if (string.IsNullOrEmpty(callsign))
            {
                // Прогноз общей активности
                var hourlyPatterns = await AnalyzeHourlyPatternsAsync();
                var dayOfWeekPatterns = await AnalyzeDayOfWeekPatternsAsync();

                var now = DateTime.Now;
                var targetTime = now.AddHours(hoursAhead);
                var targetHour = targetTime.Hour;
                var targetDayOfWeek = targetTime.DayOfWeek;

                // Простой прогноз на основе исторических данных
                var hourProbability = hourlyPatterns.ContainsKey(targetHour)
                    ? (double)hourlyPatterns[targetHour] / hourlyPatterns.Values.Max()
                    : 0.1;

                var dayProbability = dayOfWeekPatterns.ContainsKey(targetDayOfWeek)
                    ? (double)dayOfWeekPatterns[targetDayOfWeek] / dayOfWeekPatterns.Values.Max()
                    : 0.1;

                var overallProbability = (hourProbability + dayProbability) / 2;

                predictions.Add(new PredictionResult
                {
                    PredictedTime = targetTime,
                    Probability = overallProbability,
                    PredictedEvent = $"Общая активность в {targetTime:HH:00}",
                    Confidence = 0.7
                });
            }
            else
            {
                // Прогноз активности конкретного позывного
                var callsignMessages = await _context.MessageCallsigns
                    .Include(mc => mc.Message)
                    .Where(mc => mc.Callsign.Name == callsign)
                    .Select(mc => mc.Message)
                    .OrderBy(m => m.DateTime)
                    .ToListAsync();

                if (!callsignMessages.Any())
                    return predictions;

                // Анализируем исторические паттерны позывного
                var lastActivity = callsignMessages.Max(m => m.DateTime);
                var avgInterval = await CalculateAverageIntervalAsync(callsign);

                // Прогноз следующего сообщения
                var nextPredictedTime = lastActivity.Add(avgInterval);
                if (nextPredictedTime <= DateTime.Now.AddHours(hoursAhead))
                {
                    predictions.Add(new PredictionResult
                    {
                        PredictedTime = nextPredictedTime,
                        Probability = 0.8,
                        PredictedEvent = $"Сообщение от {callsign}",
                        Confidence = 0.6
                    });
                }

                // Прогноз по времени суток
                var callsignHourlyPatterns = await AnalyzeHourlyPatternsAsync(callsign);
                var targetTime = DateTime.Now.AddHours(hoursAhead);
                var targetHour = targetTime.Hour;

                if (callsignHourlyPatterns.ContainsKey(targetHour))
                {
                    var hourActivity = callsignHourlyPatterns[targetHour];
                    var maxActivity = callsignHourlyPatterns.Values.Any() ? callsignHourlyPatterns.Values.Max() : 1;
                    var probability = (double)hourActivity / maxActivity;

                    predictions.Add(new PredictionResult
                    {
                        PredictedTime = targetTime,
                        Probability = probability,
                        PredictedEvent = $"Активность {callsign} в {targetHour:00}:00",
                        Confidence = 0.5
                    });
                }
            }

            return predictions.OrderByDescending(p => p.Probability).ToList();
        }

        public async Task<Dictionary<DayOfWeek, int>> AnalyzeDayOfWeekPatternsAsync(string callsign = null)
        {
            var query = _context.Messages.AsQueryable();

            if (!string.IsNullOrEmpty(callsign))
            {
                query = query.Where(m => m.MessageCallsigns.Any(mc => mc.Callsign.Name == callsign));
            }

            var patterns = await query
                .GroupBy(m => m.DateTime.DayOfWeek)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Day, x => x.Count);

            // Заполняем все дни недели
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                if (!patterns.ContainsKey(day))
                {
                    patterns[day] = 0;
                }
            }

            return patterns;
        }

        public async Task<Dictionary<int, int>> AnalyzeHourlyPatternsAsync(string callsign = null)
        {
            var query = _context.Messages.AsQueryable();

            if (!string.IsNullOrEmpty(callsign))
            {
                query = query.Where(m => m.MessageCallsigns.Any(mc => mc.Callsign.Name == callsign));
            }

            var patterns = await query
                .GroupBy(m => m.DateTime.Hour)
                .Select(g => new { Hour = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Hour, x => x.Count);

            // Заполняем все часы
            for (int hour = 0; hour < 24; hour++)
            {
                if (!patterns.ContainsKey(hour))
                {
                    patterns[hour] = 0;
                }
            }

            return patterns.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public async Task<List<DateTime>> FindSilentPeriodsAsync(TimeSpan minDuration, string callsign = null)
        {
            var query = _context.Messages.AsQueryable();

            if (!string.IsNullOrEmpty(callsign))
            {
                query = query.Where(m => m.MessageCallsigns.Any(mc => mc.Callsign.Name == callsign));
            }

            var messages = await query
                .OrderBy(m => m.DateTime)
                .Select(m => m.DateTime)
                .ToListAsync();

            var silentPeriods = new List<DateTime>();

            for (int i = 1; i < messages.Count; i++)
            {
                var gap = messages[i] - messages[i - 1];
                if (gap >= minDuration)
                {
                    silentPeriods.Add(messages[i - 1].Add(minDuration / 2));
                }
            }

            return silentPeriods;
        }

        public async Task<List<DateTime>> FindPeakActivityTimesAsync(int topN = 10, string callsign = null)
        {
            var query = _context.Messages.AsQueryable();

            if (!string.IsNullOrEmpty(callsign))
            {
                query = query.Where(m => m.MessageCallsigns.Any(mc => mc.Callsign.Name == callsign));
            }

            var peaks = await query
                .GroupBy(m => new { m.DateTime.Date, m.DateTime.Hour })
                .Select(g => new
                {
                    Time = g.Key.Date.AddHours(g.Key.Hour),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(topN)
                .Select(x => x.Time)
                .ToListAsync();

            return peaks;
        }

        // Вспомогательные методы

        private IQueryable<Message> BuildFilteredQuery(
            DateTime? startDate, DateTime? endDate, string? area, string? frequency)
        {
            IQueryable<Message> query = _context.Messages
                .Include(m => m.MessageCallsigns)
                    .ThenInclude(mc => mc.Callsign)
                .Include(m => m.Area)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(m => m.DateTime >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(m => m.DateTime <= endDate.Value);
            if (!string.IsNullOrEmpty(area))
                query = query.Where(m => m.Area.Name == area);
            if (!string.IsNullOrEmpty(frequency))
                query = query.Where(m => m.Frequency.Value == frequency);

            return query;
        }

        private double CalculatePatternConfidence(double patternValue, double baselineValue)
        {
            if (baselineValue == 0)
                return 1.0;
            return Math.Min(1.0, (patternValue - baselineValue) / baselineValue);
        }

        private async Task<List<string>> GetTypicalCallsignsForPeriodAsync(TimeSpan start, TimeSpan end)
        {
            var query = _context.Messages
                .Where(m => m.DateTime.TimeOfDay >= start && m.DateTime.TimeOfDay < end)
                .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                .GroupBy(name => name)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key);

            return await query.ToListAsync();
        }

        private async Task<List<string>> GetTypicalAreasForPeriodAsync(TimeSpan start, TimeSpan end)
        {
            var query = _context.Messages
                .Where(m => m.DateTime.TimeOfDay >= start && m.DateTime.TimeOfDay < end)
                .Select(m => m.Area.Name)
                .GroupBy(name => name)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key);

            return await query.ToListAsync();
        }

        private async Task<List<string>> GetTypicalCallsignsForDayAsync(DayOfWeek day)
        {
            var query = _context.Messages
                .Where(m => m.DateTime.DayOfWeek == day)
                .SelectMany(m => m.MessageCallsigns.Select(mc => mc.Callsign.Name))
                .GroupBy(name => name)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key);

            return await query.ToListAsync();
        }

        private async Task<List<string>> GetTypicalAreasForDayAsync(DayOfWeek day)
        {
            var query = _context.Messages
                .Where(m => m.DateTime.DayOfWeek == day)
                .Select(m => m.Area.Name)
                .GroupBy(name => name)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key);

            return await query.ToListAsync();
        }

        private async Task<TimeSpan> CalculateAverageIntervalAsync(string callsign)
        {
            var messages = await _context.MessageCallsigns
                .Include(mc => mc.Message)
                .Where(mc => mc.Callsign.Name == callsign)
                .Select(mc => mc.Message.DateTime)
                .OrderBy(dt => dt)
                .ToListAsync();

            if (messages.Count < 2)
                return TimeSpan.FromHours(24); // Дефолтный интервал

            var intervals = new List<TimeSpan>();
            for (int i = 1; i < messages.Count; i++)
            {
                intervals.Add(messages[i] - messages[i - 1]);
            }

            return TimeSpan.FromMinutes(intervals.Average(i => i.TotalMinutes));
        }

        private string TranslateDayOfWeek(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Monday => "понедельник",
                DayOfWeek.Tuesday => "вторник",
                DayOfWeek.Wednesday => "среду",
                DayOfWeek.Thursday => "четверг",
                DayOfWeek.Friday => "пятницу",
                DayOfWeek.Saturday => "субботу",
                DayOfWeek.Sunday => "воскресенье",
                _ => day.ToString()
            };
        }
    }
}