using RadioIntercepts.Core.Models;
using RadioIntercepts.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadioIntercepts.Analysis.Services.TemporalPatterns
{
    public class TemporalPatternAnalyzer
    {
        private readonly AppDbContext _context;

        public TemporalPatternAnalyzer(AppDbContext context)
        {
            _context = context;
        }

        // Обнаружение "тихих часов" и "часов пик"
        public async Task<TimeSlotAnalysis> GetActivitySlotsAsync(DateTime? startDate = null, DateTime? endDate = null, string area = null)
        {
            var query = _context.Messages.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(m => m.DateTime >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(m => m.DateTime <= endDate.Value);
            if (!string.IsNullOrEmpty(area))
            {
                var areaEntity = await _context.Areas.FirstOrDefaultAsync(a => a.Name == area);
                if (areaEntity != null)
                    query = query.Where(m => m.Area.Key == areaEntity.Key);
            }

            var messages = await query.ToListAsync();

            var hourlyCounts = new int[24];
            foreach (var message in messages)
            {
                hourlyCounts[message.DateTime.Hour]++;
            }

            var maxCount = hourlyCounts.Max();
            var minCount = hourlyCounts.Min();

            return new TimeSlotAnalysis
            {
                PeakHours = hourlyCounts.Select((count, hour) => new { hour, count })
                    .Where(x => x.count == maxCount)
                    .Select(x => x.hour)
                    .ToList(),
                QuietHours = hourlyCounts.Select((count, hour) => new { hour, count })
                    .Where(x => x.count == minCount)
                    .Select(x => x.hour)
                    .ToList(),
                HourlyActivity = hourlyCounts
            };
        }

        // Прогнозирование активности (простое на основе среднего)
        public async Task<PredictionResult> PredictNextActivityAsync(string callsign, int hoursToPredict = 24)
        {
            var messages = await _context.MessageCallsigns
                .Include(mc => mc.Message)
                .Include(mc => mc.Callsign)
                .Where(mc => mc.Callsign.Name == callsign)
                .Select(mc => mc.Message)
                .ToListAsync();

            if (!messages.Any())
                return new PredictionResult { Callsign = callsign, Predictions = new List<HourPrediction>() };

            var hourlyAverages = new double[24];
            for (int hour = 0; hour < 24; hour++)
            {
                var hourMessages = messages.Where(m => m.DateTime.Hour == hour).ToList();
                hourlyAverages[hour] = hourMessages.Count > 0 ? (double)hourMessages.Count / messages.Count * 100 : 0;
            }

            var predictions = new List<HourPrediction>();
            for (int hour = 0; hour < hoursToPredict; hour++)
            {
                predictions.Add(new HourPrediction
                {
                    Hour = hour % 24,
                    PredictedActivity = hourlyAverages[hour % 24]
                });
            }

            return new PredictionResult
            {
                Callsign = callsign,
                Predictions = predictions
            };
        }

        // Обнаружение аномалий во времени
        public async Task<List<DateTime>> DetectAnomaliesAsync(string callsign, double threshold = 2.0)
        {
            var messages = await _context.MessageCallsigns
                .Include(mc => mc.Message)
                .Include(mc => mc.Callsign)
                .Where(mc => mc.Callsign.Name == callsign)
                .Select(mc => mc.Message.DateTime)
                .OrderBy(dt => dt)
                .ToListAsync();

            if (messages.Count < 10)
                return new List<DateTime>();

            var intervals = new List<TimeSpan>();
            for (int i = 1; i < messages.Count; i++)
            {
                intervals.Add(messages[i] - messages[i - 1]);
            }

            var averageInterval = TimeSpan.FromSeconds(intervals.Average(ts => ts.TotalSeconds));
            var stdDev = Math.Sqrt(intervals.Average(ts => Math.Pow((ts - averageInterval).TotalSeconds, 2)));

            var anomalies = new List<DateTime>();
            for (int i = 1; i < messages.Count; i++)
            {
                var interval = messages[i] - messages[i - 1];
                if (Math.Abs((interval - averageInterval).TotalSeconds) > threshold * stdDev)
                {
                    anomalies.Add(messages[i]);
                }
            }

            return anomalies;
        }
    }
}